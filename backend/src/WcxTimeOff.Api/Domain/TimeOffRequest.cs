using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WcxTimeOff.Api.Domain;

public sealed class TimeOffRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string AgentId { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartDate { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndDate { get; set; }

    [BsonRepresentation(BsonType.String)]
    public DurationType DurationType { get; set; }

    public decimal RequestedHours { get; set; }

    public string? Reason { get; set; }

    [BsonRepresentation(BsonType.String)]
    public TimeOffStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
