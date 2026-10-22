using MongoDB.Driver;
using WcxTimeOff.Api.Domain;

namespace WcxTimeOff.Api.Data;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(string id, CancellationToken cancellationToken);
    Task<List<User>> ListAgentsAsync(CancellationToken cancellationToken);
    Task InsertAsync(User user, CancellationToken cancellationToken);
}

public sealed class UserRepository(MongoContext context) : IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.Email == email.Trim().ToLowerInvariant()).FirstOrDefaultAsync(cancellationToken);

    public async Task<User?> FindByIdAsync(string id, CancellationToken cancellationToken) =>
        await context.Users.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task<List<User>> ListAgentsAsync(CancellationToken cancellationToken) =>
        context.Users.Find(x => x.Role == UserRole.Agent).SortBy(x => x.Name).ToListAsync(cancellationToken);

    public Task InsertAsync(User user, CancellationToken cancellationToken) =>
        context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
}

public interface ICreditBalanceRepository
{
    Task<CreditBalance?> FindByAgentIdAsync(string agentId, CancellationToken cancellationToken);
    Task<List<CreditBalance>> ListByAgentIdsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken);
    Task InsertAsync(CreditBalance balance, CancellationToken cancellationToken);
    Task<CreditBalance?> ReplaceEarnedIfAllowedAsync(string agentId, decimal earnedCredits, CancellationToken cancellationToken);
    Task<CreditBalance?> TryAddTakenCreditsAsync(string agentId, decimal hours, CancellationToken cancellationToken);
}

public sealed class CreditBalanceRepository(MongoContext context) : ICreditBalanceRepository
{
    public async Task<CreditBalance?> FindByAgentIdAsync(string agentId, CancellationToken cancellationToken) =>
        await context.CreditBalances.Find(x => x.AgentId == agentId).FirstOrDefaultAsync(cancellationToken);

    public async Task<List<CreditBalance>> ListByAgentIdsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken)
    {
        var ids = agentIds.ToArray();
        return await context.CreditBalances.Find(x => ids.Contains(x.AgentId)).ToListAsync(cancellationToken);
    }

    public Task InsertAsync(CreditBalance balance, CancellationToken cancellationToken) =>
        context.CreditBalances.InsertOneAsync(balance, cancellationToken: cancellationToken);

    public async Task<CreditBalance?> ReplaceEarnedIfAllowedAsync(string agentId, decimal earnedCredits, CancellationToken cancellationToken)
    {
        var filter = Builders<CreditBalance>.Filter.Where(x => x.AgentId == agentId && x.TakenCredits <= earnedCredits);
        var update = Builders<CreditBalance>.Update
            .Set(x => x.EarnedCredits, earnedCredits)
            .Inc(x => x.Version, 1);
        return await context.CreditBalances.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<CreditBalance> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public async Task<CreditBalance?> TryAddTakenCreditsAsync(string agentId, decimal hours, CancellationToken cancellationToken)
    {
        var filter = Builders<CreditBalance>.Filter.Where(x =>
            x.AgentId == agentId && x.EarnedCredits >= x.TakenCredits + hours);
        var update = Builders<CreditBalance>.Update
            .Inc(x => x.TakenCredits, hours)
            .Inc(x => x.Version, 1);
        return await context.CreditBalances.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<CreditBalance> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }
}

public interface ITimeOffRequestRepository
{
    Task InsertAsync(TimeOffRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
    Task<TimeOffRequest?> FindByIdAsync(string id, CancellationToken cancellationToken);
    Task<List<TimeOffRequest>> ListByAgentAsync(string agentId, CancellationToken cancellationToken);
    Task<List<TimeOffRequest>> ListByStatusAsync(TimeOffStatus status, CancellationToken cancellationToken);
    Task<List<TimeOffRequest>> ListActiveByAgentAsync(string agentId, CancellationToken cancellationToken);
    Task<decimal> SumPendingHoursAsync(string agentId, string? excludeRequestId, CancellationToken cancellationToken);
    Task<TimeOffRequest?> ClaimPendingAsync(string id, TimeOffStatus nextStatus, string reviewerId, CancellationToken cancellationToken);
    Task RevertToPendingAsync(string id, CancellationToken cancellationToken);
}

public sealed class TimeOffRequestRepository(MongoContext context) : ITimeOffRequestRepository
{
    public Task InsertAsync(TimeOffRequest request, CancellationToken cancellationToken) =>
        context.TimeOffRequests.InsertOneAsync(request, cancellationToken: cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken) =>
        context.TimeOffRequests.DeleteOneAsync(x => x.Id == id, cancellationToken);

    public async Task<TimeOffRequest?> FindByIdAsync(string id, CancellationToken cancellationToken) =>
        await context.TimeOffRequests.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task<List<TimeOffRequest>> ListByAgentAsync(string agentId, CancellationToken cancellationToken) =>
        context.TimeOffRequests.Find(x => x.AgentId == agentId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<TimeOffRequest>> ListByStatusAsync(TimeOffStatus status, CancellationToken cancellationToken) =>
        context.TimeOffRequests.Find(x => x.Status == status)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<TimeOffRequest>> ListActiveByAgentAsync(string agentId, CancellationToken cancellationToken) =>
        context.TimeOffRequests
            .Find(x => x.AgentId == agentId && (x.Status == TimeOffStatus.Pending || x.Status == TimeOffStatus.Approved))
            .ToListAsync(cancellationToken);

    public async Task<decimal> SumPendingHoursAsync(string agentId, string? excludeRequestId, CancellationToken cancellationToken)
    {
        var filter = Builders<TimeOffRequest>.Filter.Where(x => x.AgentId == agentId && x.Status == TimeOffStatus.Pending);
        if (!string.IsNullOrEmpty(excludeRequestId))
        {
            filter &= Builders<TimeOffRequest>.Filter.Ne(x => x.Id, excludeRequestId);
        }

        var pending = await context.TimeOffRequests.Find(filter).ToListAsync(cancellationToken);
        return pending.Sum(x => x.RequestedHours);
    }

    public async Task<TimeOffRequest?> ClaimPendingAsync(string id, TimeOffStatus nextStatus, string reviewerId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<TimeOffRequest>.Filter.Where(x => x.Id == id && x.Status == TimeOffStatus.Pending);
        var update = Builders<TimeOffRequest>.Update
            .Set(x => x.Status, nextStatus)
            .Set(x => x.ReviewedBy, reviewerId)
            .Set(x => x.ReviewedAt, now)
            .Set(x => x.UpdatedAt, now);
        return await context.TimeOffRequests.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<TimeOffRequest> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public Task RevertToPendingAsync(string id, CancellationToken cancellationToken)
    {
        var update = Builders<TimeOffRequest>.Update
            .Set(x => x.Status, TimeOffStatus.Pending)
            .Set(x => x.ReviewedBy, null)
            .Set(x => x.ReviewedAt, null)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        return context.TimeOffRequests.FindOneAndUpdateAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }
}
