using WcxTimeOff.Api.Data;
using WcxTimeOff.Api.Domain;

namespace WcxTimeOff.Api.Tests;

internal sealed class InMemoryUsers : IUserRepository
{
    private readonly List<User> _users = [];

    public void Add(User user) => _users.Add(user);

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(x => x.Email == email));

    public Task<User?> FindByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(x => x.Id == id));

    public Task<List<User>> ListAgentsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_users.Where(x => x.Role == UserRole.Agent).OrderBy(x => x.Name).ToList());

    public Task InsertAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryCredits : ICreditBalanceRepository
{
    private readonly List<CreditBalance> _items = [];

    public void Add(CreditBalance balance) => _items.Add(balance);

    public Task<CreditBalance?> FindByAgentIdAsync(string agentId, CancellationToken cancellationToken) =>
        Task.FromResult(_items.FirstOrDefault(x => x.AgentId == agentId));

    public Task<List<CreditBalance>> ListByAgentIdsAsync(IEnumerable<string> agentIds, CancellationToken cancellationToken)
    {
        var ids = agentIds.ToHashSet();
        return Task.FromResult(_items.Where(x => ids.Contains(x.AgentId)).ToList());
    }

    public Task InsertAsync(CreditBalance balance, CancellationToken cancellationToken)
    {
        _items.Add(balance);
        return Task.CompletedTask;
    }

    public Task<CreditBalance?> ReplaceEarnedIfAllowedAsync(string agentId, decimal earnedCredits, CancellationToken cancellationToken)
    {
        var item = _items.FirstOrDefault(x => x.AgentId == agentId && x.TakenCredits <= earnedCredits);
        if (item is null)
        {
            return Task.FromResult<CreditBalance?>(null);
        }

        item.EarnedCredits = earnedCredits;
        item.Version++;
        return Task.FromResult<CreditBalance?>(item);
    }

    public Task<CreditBalance?> TryAddTakenCreditsAsync(string agentId, decimal hours, CancellationToken cancellationToken)
    {
        var item = _items.FirstOrDefault(x => x.AgentId == agentId && x.EarnedCredits >= x.TakenCredits + hours);
        if (item is null)
        {
            return Task.FromResult<CreditBalance?>(null);
        }

        item.TakenCredits += hours;
        item.Version++;
        return Task.FromResult<CreditBalance?>(item);
    }
}

internal sealed class InMemoryRequests : ITimeOffRequestRepository
{
    private readonly List<TimeOffRequest> _items = [];
    private int _id;

    public IReadOnlyList<TimeOffRequest> Items => _items;

    public Task InsertAsync(TimeOffRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            request.Id = $"req-{++_id}";
        }

        _items.Add(Clone(request));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        _items.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task<TimeOffRequest?> FindByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id) is { } found ? Clone(found) : null);

    public Task<List<TimeOffRequest>> ListByAgentAsync(string agentId, CancellationToken cancellationToken) =>
        Task.FromResult(_items.Where(x => x.AgentId == agentId).Select(Clone).ToList());

    public Task<List<TimeOffRequest>> ListByStatusAsync(TimeOffStatus status, CancellationToken cancellationToken) =>
        Task.FromResult(_items.Where(x => x.Status == status).Select(Clone).ToList());

    public Task<List<TimeOffRequest>> ListActiveByAgentAsync(string agentId, CancellationToken cancellationToken) =>
        Task.FromResult(_items
            .Where(x => x.AgentId == agentId && (x.Status == TimeOffStatus.Pending || x.Status == TimeOffStatus.Approved))
            .Select(Clone)
            .ToList());

    public Task<decimal> SumPendingHoursAsync(string agentId, string? excludeRequestId, CancellationToken cancellationToken) =>
        Task.FromResult(_items
            .Where(x => x.AgentId == agentId && x.Status == TimeOffStatus.Pending && x.Id != excludeRequestId)
            .Sum(x => x.RequestedHours));

    public Task<TimeOffRequest?> ClaimPendingAsync(string id, TimeOffStatus nextStatus, string reviewerId, CancellationToken cancellationToken)
    {
        var item = _items.FirstOrDefault(x => x.Id == id && x.Status == TimeOffStatus.Pending);
        if (item is null)
        {
            return Task.FromResult<TimeOffRequest?>(null);
        }

        item.Status = nextStatus;
        item.ReviewedBy = reviewerId;
        item.ReviewedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult<TimeOffRequest?>(Clone(item));
    }

    public Task RevertToPendingAsync(string id, CancellationToken cancellationToken)
    {
        var item = _items.First(x => x.Id == id);
        item.Status = TimeOffStatus.Pending;
        item.ReviewedBy = null;
        item.ReviewedAt = null;
        item.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    private static TimeOffRequest Clone(TimeOffRequest request) => new()
    {
        Id = request.Id,
        AgentId = request.AgentId,
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        DurationType = request.DurationType,
        RequestedHours = request.RequestedHours,
        Reason = request.Reason,
        Status = request.Status,
        CreatedAt = request.CreatedAt,
        UpdatedAt = request.UpdatedAt,
        ReviewedBy = request.ReviewedBy,
        ReviewedAt = request.ReviewedAt
    };
}
