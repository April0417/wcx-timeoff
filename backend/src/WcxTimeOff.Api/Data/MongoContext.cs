using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using WcxTimeOff.Api.Configuration;
using WcxTimeOff.Api.Domain;

namespace WcxTimeOff.Api.Data;

public sealed class MongoContext
{
    static MongoContext()
    {
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<UserRole>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<DurationType>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<TimeOffStatus>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
    }

    public MongoContext(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.DatabaseName);
        Users = Database.GetCollection<User>("users");
        CreditBalances = Database.GetCollection<CreditBalance>("creditBalances");
        TimeOffRequests = Database.GetCollection<TimeOffRequest>("timeOffRequests");
    }

    public IMongoDatabase Database { get; }
    public IMongoCollection<User> Users { get; }
    public IMongoCollection<CreditBalance> CreditBalances { get; }
    public IMongoCollection<TimeOffRequest> TimeOffRequests { get; }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" }),
            cancellationToken: cancellationToken);

        await CreditBalances.Indexes.CreateOneAsync(
            new CreateIndexModel<CreditBalance>(
                Builders<CreditBalance>.IndexKeys.Ascending(x => x.AgentId),
                new CreateIndexOptions { Unique = true, Name = "ux_creditBalances_agentId" }),
            cancellationToken: cancellationToken);

        await TimeOffRequests.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<TimeOffRequest>(
                    Builders<TimeOffRequest>.IndexKeys
                        .Ascending(x => x.AgentId)
                        .Ascending(x => x.StartDate)
                        .Ascending(x => x.EndDate),
                    new CreateIndexOptions { Name = "ix_requests_agent_dates" }),
                new CreateIndexModel<TimeOffRequest>(
                    Builders<TimeOffRequest>.IndexKeys.Ascending(x => x.Status),
                    new CreateIndexOptions { Name = "ix_requests_status" })
            ],
            cancellationToken: cancellationToken);
    }
}
