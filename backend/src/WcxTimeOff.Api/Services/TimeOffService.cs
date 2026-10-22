using System.Net;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Data;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Errors;

namespace WcxTimeOff.Api.Services;

public interface ITimeOffService
{
    Task<TimeOffRequestDto> CreateAsync(string agentId, CreateTimeOffRequest request, CancellationToken cancellationToken);
    Task<List<TimeOffRequestDto>> ListMineAsync(string agentId, CancellationToken cancellationToken);
    Task<TimeOffRequestDto> GetByIdAsync(string requestId, string userId, bool isAdmin, CancellationToken cancellationToken);
    Task<List<TimeOffRequestDto>> ListPendingAsync(CancellationToken cancellationToken);
    Task<TimeOffRequestDto> ApproveAsync(string requestId, string reviewerId, CancellationToken cancellationToken);
    Task<TimeOffRequestDto> DenyAsync(string requestId, string reviewerId, CancellationToken cancellationToken);
}

public sealed class TimeOffService(
    IUserRepository users,
    ICreditBalanceRepository credits,
    ITimeOffRequestRepository requests) : ITimeOffService
{
    public async Task<TimeOffRequestDto> CreateAsync(string agentId, CreateTimeOffRequest request, CancellationToken cancellationToken)
    {
        var (start, end, durationType, hours) = ValidateCreate(request);

        var entity = new TimeOffRequest
        {
            AgentId = agentId,
            StartDate = start,
            EndDate = end,
            DurationType = durationType,
            RequestedHours = hours,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = TimeOffStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await requests.InsertAsync(entity, cancellationToken);

        try
        {
            await EnsureNoOverlap(entity, cancellationToken);
            await EnsureRequestableBalance(entity, cancellationToken);
        }
        catch
        {
            await requests.DeleteAsync(entity.Id, cancellationToken);
            throw;
        }

        return await MapAsync(entity, cancellationToken);
    }

    public async Task<List<TimeOffRequestDto>> ListMineAsync(string agentId, CancellationToken cancellationToken)
    {
        var items = await requests.ListByAgentAsync(agentId, cancellationToken);
        return await MapManyAsync(items, cancellationToken);
    }

    public async Task<TimeOffRequestDto> GetByIdAsync(string requestId, string userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var entity = await requests.FindByIdAsync(requestId, cancellationToken)
            ?? throw new AppException(HttpStatusCode.NotFound, "request_not_found", "Time-off request was not found.");

        if (!isAdmin && entity.AgentId != userId)
        {
            throw new AppException(HttpStatusCode.Forbidden, "forbidden", "You cannot view another agent's request.");
        }

        return await MapAsync(entity, cancellationToken);
    }

    public async Task<List<TimeOffRequestDto>> ListPendingAsync(CancellationToken cancellationToken)
    {
        var items = await requests.ListByStatusAsync(TimeOffStatus.Pending, cancellationToken);
        return await MapManyAsync(items, cancellationToken);
    }

    public async Task<TimeOffRequestDto> ApproveAsync(string requestId, string reviewerId, CancellationToken cancellationToken)
    {
        var claimed = await requests.ClaimPendingAsync(requestId, TimeOffStatus.Approved, reviewerId, cancellationToken);
        if (claimed is null)
        {
            var existing = await requests.FindByIdAsync(requestId, cancellationToken);
            if (existing is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "request_not_found", "Time-off request was not found.");
            }

            throw new AppException(
                HttpStatusCode.Conflict,
                "already_reviewed",
                $"This request is already {existing.Status} and cannot be approved.");
        }

        var updatedBalance = await credits.TryAddTakenCreditsAsync(claimed.AgentId, claimed.RequestedHours, cancellationToken);
        if (updatedBalance is null)
        {
            await requests.RevertToPendingAsync(claimed.Id, cancellationToken);
            throw new AppException(
                HttpStatusCode.Conflict,
                "insufficient_balance",
                "The request can no longer be approved because the agent does not have enough available credits.");
        }

        return await MapAsync(claimed, cancellationToken);
    }

    public async Task<TimeOffRequestDto> DenyAsync(string requestId, string reviewerId, CancellationToken cancellationToken)
    {
        var claimed = await requests.ClaimPendingAsync(requestId, TimeOffStatus.Denied, reviewerId, cancellationToken);
        if (claimed is null)
        {
            var existing = await requests.FindByIdAsync(requestId, cancellationToken);
            if (existing is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "request_not_found", "Time-off request was not found.");
            }

            throw new AppException(
                HttpStatusCode.Conflict,
                "already_reviewed",
                $"This request is already {existing.Status} and cannot be denied.");
        }

        return await MapAsync(claimed, cancellationToken);
    }

    public static (DateTime start, DateTime end, DurationType durationType, decimal hours) ValidateCreate(CreateTimeOffRequest request)
    {
        if (request.StartDate is null || request.EndDate is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "invalid_dates", "Start date and end date are required.");
        }

        if (request.DurationType is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "invalid_duration_type", "Duration type is required.");
        }

        var start = TimeOffCalculator.ToUtcDate(request.StartDate.Value);
        var end = TimeOffCalculator.ToUtcDate(request.EndDate.Value);
        if (end < start)
        {
            throw new AppException(HttpStatusCode.BadRequest, "invalid_dates", "End date cannot be earlier than start date.");
        }

        var durationType = request.DurationType.Value;
        if (durationType == DurationType.PartialDay)
        {
            if (start != end)
            {
                throw new AppException(
                    HttpStatusCode.BadRequest,
                    "invalid_partial_day",
                    "Partial-day requests are limited to a single date.");
            }

            if (request.RequestedHours is null || request.RequestedHours <= 0)
            {
                throw new AppException(
                    HttpStatusCode.BadRequest,
                    "invalid_hours",
                    "Partial-day requests require a positive number of hours.");
            }

            if (request.RequestedHours > TimeOffCalculator.HoursPerFullDay)
            {
                throw new AppException(
                    HttpStatusCode.BadRequest,
                    "invalid_hours",
                    $"Partial-day requests cannot exceed {TimeOffCalculator.HoursPerFullDay} hours.");
            }
        }

        var hours = TimeOffCalculator.ResolveRequestedHours(durationType, start, end, request.RequestedHours);
        if (hours <= 0)
        {
            throw new AppException(HttpStatusCode.BadRequest, "invalid_hours", "Requested hours must be greater than zero.");
        }

        return (start, end, durationType, hours);
    }

    private async Task EnsureNoOverlap(TimeOffRequest entity, CancellationToken cancellationToken)
    {
        var existing = await requests.ListActiveByAgentAsync(entity.AgentId, cancellationToken);
        var overlap = existing.FirstOrDefault(x =>
            x.Id != entity.Id &&
            TimeOffCalculator.DatesOverlap(x.StartDate, x.EndDate, entity.StartDate, entity.EndDate));

        if (overlap is not null)
        {
            throw new AppException(
                HttpStatusCode.Conflict,
                "overlapping_request",
                "This request overlaps an existing pending or approved request.");
        }
    }

    private async Task EnsureRequestableBalance(TimeOffRequest entity, CancellationToken cancellationToken)
    {
        var balance = await credits.FindByAgentIdAsync(entity.AgentId, cancellationToken)
            ?? throw new AppException(HttpStatusCode.NotFound, "credits_not_found", "Credit balance was not found.");

        var otherPending = await requests.SumPendingHoursAsync(entity.AgentId, entity.Id, cancellationToken);
        var requestable = TimeOffCalculator.RequestableBalance(balance.EarnedCredits, balance.TakenCredits, otherPending);
        if (entity.RequestedHours > requestable)
        {
            throw new AppException(
                HttpStatusCode.BadRequest,
                "insufficient_balance",
                $"Requested hours exceed the remaining requestable balance ({requestable:0.##}), which reserves pending requests.");
        }
    }

    private async Task<TimeOffRequestDto> MapAsync(TimeOffRequest entity, CancellationToken cancellationToken)
    {
        var agent = await users.FindByIdAsync(entity.AgentId, cancellationToken);
        return new TimeOffRequestDto
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            AgentName = agent?.Name ?? "Unknown agent",
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DurationType = entity.DurationType.ToString(),
            RequestedHours = entity.RequestedHours,
            Reason = entity.Reason,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ReviewedBy = entity.ReviewedBy,
            ReviewedAt = entity.ReviewedAt
        };
    }

    private async Task<List<TimeOffRequestDto>> MapManyAsync(IEnumerable<TimeOffRequest> items, CancellationToken cancellationToken)
    {
        var result = new List<TimeOffRequestDto>();
        foreach (var item in items)
        {
            result.Add(await MapAsync(item, cancellationToken));
        }

        return result;
    }
}
