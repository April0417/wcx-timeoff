using FluentAssertions;
using WcxTimeOff.Api.Domain;

namespace WcxTimeOff.Api.Tests;

public class TimeOffCalculatorTests
{
    [Fact]
    public void Available_balance_is_earned_minus_taken()
    {
        TimeOffCalculator.AvailableBalance(80, 24).Should().Be(56);
    }

    [Fact]
    public void Full_day_hours_use_inclusive_calendar_days()
    {
        var start = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);
        TimeOffCalculator.CalculateFullDayHours(start, end).Should().Be(24);
    }

    [Fact]
    public void Inclusive_date_ranges_overlap_on_boundary_days()
    {
        var aStart = new DateTime(2026, 10, 21);
        var aEnd = new DateTime(2026, 10, 22);
        var bStart = new DateTime(2026, 10, 22);
        var bEnd = new DateTime(2026, 10, 24);
        TimeOffCalculator.DatesOverlap(aStart, aEnd, bStart, bEnd).Should().BeTrue();
    }

    [Fact]
    public void Requestable_balance_reserves_pending_hours()
    {
        TimeOffCalculator.RequestableBalance(40, 8, 16).Should().Be(16);
    }
}
