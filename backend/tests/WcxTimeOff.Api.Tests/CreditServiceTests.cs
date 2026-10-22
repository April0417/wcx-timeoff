using FluentAssertions;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Errors;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Tests;

public class CreditServiceTests
{
    [Fact]
    public async Task Earned_credits_cannot_fall_below_taken_credits()
    {
        var users = new InMemoryUsers();
        var credits = new InMemoryCredits();
        var requests = new InMemoryRequests();
        users.Add(new User { Id = "agent-1", Name = "Jordan", Email = "jordan@wcx.local", Role = UserRole.Agent });
        credits.Add(new CreditBalance { AgentId = "agent-1", EarnedCredits = 40, TakenCredits = 16 });
        var service = new CreditService(users, credits, requests);

        var act = () => service.UpdateEarnedCreditsAsync("agent-1", 8, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(x => x.Error == "earned_below_taken");
    }

    [Fact]
    public async Task Dashboard_available_balance_matches_calculator()
    {
        var users = new InMemoryUsers();
        var credits = new InMemoryCredits();
        var requests = new InMemoryRequests();
        users.Add(new User { Id = "agent-1", Name = "Jordan", Email = "jordan@wcx.local", Role = UserRole.Agent });
        credits.Add(new CreditBalance { AgentId = "agent-1", EarnedCredits = 80, TakenCredits = 24 });
        var service = new CreditService(users, credits, requests);

        var overview = await service.GetAgentCreditsAsync("agent-1", CancellationToken.None);
        overview.Credits.AvailableBalance.Should().Be(56);
        overview.Credits.TakenCredits.Should().Be(24);
    }
}
