namespace WcxTimeOff.Api.Domain;

/// <summary>
/// Isolated hour-calculation rules. Full-day duration currently uses inclusive
/// calendar days (not a working-day calendar) so a weekday calendar can be
/// swapped in later without touching controllers.
/// </summary>
public static class TimeOffCalculator
{
    public const decimal HoursPerFullDay = 8m;

    public static DateTime ToUtcDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    public static int InclusiveCalendarDays(DateTime startDate, DateTime endDate)
    {
        var start = ToUtcDate(startDate);
        var end = ToUtcDate(endDate);
        return (end - start).Days + 1;
    }

    public static decimal CalculateFullDayHours(DateTime startDate, DateTime endDate) =>
        InclusiveCalendarDays(startDate, endDate) * HoursPerFullDay;

    public static decimal ResolveRequestedHours(
        DurationType durationType,
        DateTime startDate,
        DateTime endDate,
        decimal? partialHours)
    {
        if (durationType == DurationType.FullDay)
        {
            return CalculateFullDayHours(startDate, endDate);
        }

        return partialHours ?? 0m;
    }

    public static bool DatesOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
    {
        var aStart = ToUtcDate(startA);
        var aEnd = ToUtcDate(endA);
        var bStart = ToUtcDate(startB);
        var bEnd = ToUtcDate(endB);
        return aStart <= bEnd && aEnd >= bStart;
    }

    public static decimal AvailableBalance(decimal earnedCredits, decimal takenCredits) =>
        earnedCredits - takenCredits;

    public static decimal RequestableBalance(
        decimal earnedCredits,
        decimal takenCredits,
        decimal pendingReservedHours) =>
        earnedCredits - takenCredits - pendingReservedHours;
}
