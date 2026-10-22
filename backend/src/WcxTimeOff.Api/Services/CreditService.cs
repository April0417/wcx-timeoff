using System.Net;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Data;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Errors;

namespace WcxTimeOff.Api.Services;

public interface ICreditService
{
    Task<List<AgentOverviewDto>> ListAgentsAsync(CancellationToken cancellationToken);
    Task<AgentOverviewDto> GetAgentCreditsAsync(string agentId, CancellationToken cancellationToken);
    Task<AgentOverviewDto> UpdateEarnedCreditsAsync(string agentId, decimal earnedCredits, CancellationToken cancellationToken);
    Task<CreditBalanceDto> BuildBalanceDtoAsync(string agentId, CreditBalance balance, CancellationToken cancellationToken);
}

public sealed class CreditService(
    IUserRepository users,
    ICreditBalanceRepository credits,
    ITimeOffRequestRepository requests) : ICreditService
{
    public async Task<List<AgentOverviewDto>> ListAgentsAsync(CancellationToken cancellationToken)
    {
        var agents = await users.ListAgentsAsync(cancellationToken);
        var balances = await credits.ListByAgentIdsAsync(agents.Select(x => x.Id), cancellationToken);
        var balanceByAgent = balances.ToDictionary(x => x.AgentId);

        var result = new List<AgentOverviewDto>();
        foreach (var agent in agents)
        {
            balanceByAgent.TryGetValue(agent.Id, out var balance);
            balance ??= new CreditBalance { AgentId = agent.Id };
            result.Add(await ToOverview(agent, balance, cancellationToken));
        }

        return result;
    }

    public async Task<AgentOverviewDto> GetAgentCreditsAsync(string agentId, CancellationToken cancellationToken)
    {
        var agent = await RequireAgent(agentId, cancellationToken);
        var balance = await credits.FindByAgentIdAsync(agentId, cancellationToken)
            ?? throw new AppException(HttpStatusCode.NotFound, "credits_not_found", "Credit balance was not found.");
        return await ToOverview(agent, balance, cancellationToken);
    }

    public async Task<AgentOverviewDto> UpdateEarnedCreditsAsync(string agentId, decimal earnedCredits, CancellationToken cancellationToken)
    {
        var agent = await RequireAgent(agentId, cancellationToken);
        var existing = await credits.FindByAgentIdAsync(agentId, cancellationToken)
            ?? throw new AppException(HttpStatusCode.NotFound, "credits_not_found", "Credit balance was not found.");

        if (earnedCredits < existing.TakenCredits)
        {
            throw new AppException(
                HttpStatusCode.BadRequest,
                "earned_below_taken",
                $"Earned credits cannot be reduced below taken credits ({existing.TakenCredits:0.##}).");
        }

        var updated = await credits.ReplaceEarnedIfAllowedAsync(agentId, earnedCredits, cancellationToken)
            ?? throw new AppException(
                HttpStatusCode.Conflict,
                "earned_below_taken",
                "Earned credits cannot be reduced below taken credits because the balance changed.");

        return await ToOverview(agent, updated, cancellationToken);
    }

    public async Task<CreditBalanceDto> BuildBalanceDtoAsync(string agentId, CreditBalance balance, CancellationToken cancellationToken)
    {
        var pending = await requests.SumPendingHoursAsync(agentId, null, cancellationToken);
        var available = TimeOffCalculator.AvailableBalance(balance.EarnedCredits, balance.TakenCredits);
        return new CreditBalanceDto
        {
            EarnedCredits = balance.EarnedCredits,
            TakenCredits = balance.TakenCredits,
            AvailableBalance = available,
            PendingReservedHours = pending,
            RequestableBalance = TimeOffCalculator.RequestableBalance(balance.EarnedCredits, balance.TakenCredits, pending)
        };
    }

    private async Task<AgentOverviewDto> ToOverview(User agent, CreditBalance balance, CancellationToken cancellationToken) =>
        new()
        {
            Id = agent.Id,
            Name = agent.Name,
            Email = agent.Email,
            Credits = await BuildBalanceDtoAsync(agent.Id, balance, cancellationToken)
        };

    private async Task<User> RequireAgent(string agentId, CancellationToken cancellationToken)
    {
        var agent = await users.FindByIdAsync(agentId, cancellationToken);
        if (agent is null || agent.Role != UserRole.Agent)
        {
            throw new AppException(HttpStatusCode.NotFound, "agent_not_found", "Agent was not found.");
        }

        return agent;
    }
}
