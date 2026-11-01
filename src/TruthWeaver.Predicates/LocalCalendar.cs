namespace TruthWeaver.Predicates;

/// <summary>
/// Reads the local calendar fields of an instant in a fixed offset from UTC, for the calendar predicates of
/// <see cref="DateTimePredicates"/>. The fields come from the UTC ticks plus the offset ticks, so every representable
/// <see cref="DateTimeOffset"/> gets an answer in every offset.
/// </summary>
/// <remarks>
/// <see cref="DateTimeOffset.ToOffset"/> throws when the local clock time falls outside the <see cref="DateTime"/> range,
/// for example <c>default(DateTimeOffset)</c> at <c>-05:00</c> or <see cref="DateTimeOffset.MaxValue"/> at
/// <c>+05:00</c>. An offset is at most 14 hours, so such a local time is at most one day outside the range: 31 December
/// of year 0 (proleptic Gregorian) or 1 January of year 10000. These methods read the fields of that day directly.
/// </remarks>
internal static class LocalCalendar
{
    /// <summary>Gets the time of day of <paramref name="instant"/> in <paramref name="offset"/>.</summary>
    /// <param name="instant">The instant.</param>
    /// <param name="offset">The fixed offset from UTC.</param>
    /// <returns>The local time of day, from zero up to, but not including, 24 hours.</returns>
    internal static TimeSpan TimeOfDayOf(DateTimeOffset instant, TimeSpan offset)
    {
        long ticks = LocalTicks(instant, offset);
        return new TimeSpan(ticks - (DayNumber(ticks) * TimeSpan.TicksPerDay));
    }

    /// <summary>Gets the day of the week of <paramref name="instant"/> in <paramref name="offset"/>.</summary>
    /// <param name="instant">The instant.</param>
    /// <param name="offset">The fixed offset from UTC.</param>
    /// <returns>The local day of the week.</returns>
    internal static DayOfWeek DayOfWeekOf(DateTimeOffset instant, TimeSpan offset)
    {
        // Day 0 is Monday 0001-01-01, and DayOfWeek.Monday is 1. The remainder is made non-negative for day -1.
        long day = DayNumber(LocalTicks(instant, offset));
        return (DayOfWeek)((((day + 1) % 7) + 7) % 7);
    }

    /// <summary>Gets the month (1 to 12) of <paramref name="instant"/> in <paramref name="offset"/>.</summary>
    /// <param name="instant">The instant.</param>
    /// <param name="offset">The fixed offset from UTC.</param>
    /// <returns>The local month, from 1 (January) to 12 (December).</returns>
    internal static int MonthOf(DateTimeOffset instant, TimeSpan offset)
    {
        long ticks = LocalTicks(instant, offset);
        if (ticks < 0)
        {
            // The day before 0001-01-01 is 31 December.
            return 12;
        }

        // The day after 9999-12-31 is 1 January.
        return ticks > DateTime.MaxValue.Ticks ? 1 : new DateTime(ticks, DateTimeKind.Unspecified).Month;
    }

    // The local clock time as ticks since 0001-01-01 00:00. It can be below zero or above DateTime.MaxValue.Ticks by up
    // to 14 hours; it cannot overflow a long.
    private static long LocalTicks(DateTimeOffset instant, TimeSpan offset)
    {
        return instant.UtcTicks + offset.Ticks;
    }

    // The number of whole days since 0001-01-01, rounded down, so a negative tick count gives day -1.
    private static long DayNumber(long ticks)
    {
        long quotient = Math.DivRem(ticks, TimeSpan.TicksPerDay, out long remainder);
        return remainder < 0 ? quotient - 1 : quotient;
    }
}
