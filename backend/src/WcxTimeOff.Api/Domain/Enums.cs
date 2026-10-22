namespace WcxTimeOff.Api.Domain;

public enum UserRole
{
    Admin,
    Agent
}

public enum DurationType
{
    FullDay,
    PartialDay
}

public enum TimeOffStatus
{
    Pending,
    Approved,
    Denied
}
