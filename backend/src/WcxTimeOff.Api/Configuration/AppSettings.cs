namespace WcxTimeOff.Api.Configuration;

public sealed class MongoSettings
{
    public const string SectionName = "Mongo";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "wcx_timeoff";
}

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "WcxTimeOff";
    public string Audience { get; set; } = "WcxTimeOff";
    public string Key { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 12;
}

public sealed class SeedSettings
{
    public const string SectionName = "Seed";
    public bool Enabled { get; set; } = true;
}
