using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WcxTimeOff.Api.Domain;

public sealed class CreditBalance
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string AgentId { get; set; } = string.Empty;

    public decimal EarnedCredits { get; set; }

    public decimal TakenCredits { get; set; }

    public long Version { get; set; }

    [BsonIgnore]
    public decimal AvailableBalance => EarnedCredits - TakenCredits;
}
