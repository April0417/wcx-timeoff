using FluentAssertions;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Errors;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Tests;

public class TimeOffServiceTests
{
    private readonly InMemoryUsers _users = new();
    private readonly InMemoryCredits _credits = new();
    private readonly InMemoryRequests _requests = new();
    private readonly TimeOffService _service;

    public TimeOffServiceTests()
    {
        _users.Add(new User { Id = "agent-1", Name = "Jordan Lee", Email = "jordan@wcx.local", Role = UserRole.Agent });
        _credits.Add(new CreditBalance { AgentId = "agent-1", EarnedCredits = 40, TakenCredits = 8 });
        _service = new TimeOffService(_users, _credits, _requests);
    }

    [Fact]
    public void End_date_before_start_date_is_rejected()
    {
        var act = () => TimeOffService.ValidateCreate(new CreateTimeOffRequest
        {
            StartDate = new DateTime(2026, 10, 22),
            EndDate = new DateTime(2026, 10, 21),
            DurationType = DurationType.FullDay
        });

        act.Should().Throw<AppException>().Where(x => x.Error == "invalid_dates");
    }

    [Fact]
    public void Partial_day_rejects_zero_or_negative_hours()
    {
        var act = () => TimeOffService.ValidateCreate(new CreateTimeOffRequest
        {
            StartDate = new DateTime(2026, 10, 21),
            EndDate = new DateTime(2026, 10, 21),
            DurationType = DurationType.PartialDay,
            RequestedHours = 0
        });

        act.Should().Throw<AppException>().Where(x => x.Error == "invalid_hours");
    }

    [Fact]
    public void Partial_day_must_be_a_single_date_with_positive_hours()
    {
        var act = () => TimeOffService.ValidateCreate(new CreateTimeOffRequest
        {
            StartDate = new DateTime(2026, 10, 21),
            EndDate = new DateTime(2026, 10, 22),
            DurationType = DurationType.PartialDay,
            RequestedHours = 4
        });

        act.Should().Throw<AppException>().Where(x => x.Error == "invalid_partial_day");
    }

    [Fact]
    public async Task Insufficient_requestable_balance_is_rejected()
    {
        var act = () => _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-26"), CancellationToken.None);
        var error = await act.Should().ThrowAsync<AppException>();
        error.Which.Error.Should().Be("insufficient_balance");
    }

    [Fact]
    public async Task Overlapping_pending_or_approved_requests_are_rejected()
    {
        await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-21"), CancellationToken.None);

        var act = () => _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-22"), CancellationToken.None);
        var error = await act.Should().ThrowAsync<AppException>();
        error.Which.Error.Should().Be("overlapping_request");
        _requests.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Approval_increases_taken_credits_exactly_once()
    {
        var created = await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-21"), CancellationToken.None);

        var approved = await _service.ApproveAsync(created.Id, "admin-1", CancellationToken.None);
        approved.Status.Should().Be("Approved");

        var balance = await _credits.FindByAgentIdAsync("agent-1", CancellationToken.None);
        balance!.TakenCredits.Should().Be(16);

        var retry = () => _service.ApproveAsync(created.Id, "admin-1", CancellationToken.None);
        await retry.Should().ThrowAsync<AppException>().Where(x => x.Error == "already_reviewed");
        (await _credits.FindByAgentIdAsync("agent-1", CancellationToken.None))!.TakenCredits.Should().Be(16);
    }

    [Fact]
    public async Task Denial_leaves_taken_credits_unchanged()
    {
        var created = await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-21"), CancellationToken.None);
        var denied = await _service.DenyAsync(created.Id, "admin-1", CancellationToken.None);

        denied.Status.Should().Be("Denied");
        (await _credits.FindByAgentIdAsync("agent-1", CancellationToken.None))!.TakenCredits.Should().Be(8);
    }

    [Fact]
    public async Task Denied_requests_do_not_block_new_requests()
    {
        var created = await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-21"), CancellationToken.None);
        await _service.DenyAsync(created.Id, "admin-1", CancellationToken.None);

        var again = await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-21"), CancellationToken.None);
        again.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Pending_hours_are_reserved_so_two_requests_cannot_overdraw()
    {
        await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-23"), CancellationToken.None);

        var act = () => _service.CreateAsync("agent-1", FullDay("2026-10-24", "2026-10-26"), CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(x => x.Error == "insufficient_balance");
    }

    [Fact]
    public async Task Agents_cannot_view_another_agents_request()
    {
        var created = await _service.CreateAsync("agent-1", FullDay("2026-10-21", "2026-10-21"), CancellationToken.None);
        var act = () => _service.GetByIdAsync(created.Id, "agent-2", isAdmin: false, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(x => x.Error == "forbidden");
    }

    private static CreateTimeOffRequest FullDay(string start, string end) => new()
    {
        StartDate = DateTime.Parse(start),
        EndDate = DateTime.Parse(end),
        DurationType = DurationType.FullDay
    };
}
