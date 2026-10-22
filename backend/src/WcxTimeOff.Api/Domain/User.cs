using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WcxTimeOff.Api.Domain;

public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; }
}
