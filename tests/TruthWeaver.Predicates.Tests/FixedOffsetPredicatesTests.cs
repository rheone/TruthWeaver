namespace TruthWeaver.Predicates.Tests;

using System.Globalization;
using TruthWeaver.Abstractions;

/// <summary>
/// Pins the fixed-offset calendar predicates of <see cref="DateTimePredicates"/>: <c>OnDayOfWeek</c>, <c>InMonth</c> and
/// <c>InTimeWindow</c>, with their <c>NotX</c> Strong Kleene twins. Each reads the selected instant in the fixed offset
/// that the rule gives.
/// </summary>
public class FixedOffsetPredicatesTests
{
    // Friday 2026-10-09 23:30 UTC is Saturday 2026-10-10 05:00 at +05:30.
    private static readonly DateTimeOffset LateFridayUtc = new(2026, 10, 9, 23, 30, 0, TimeSpan.Zero);

    /// <summary>The offset moves the instant across midnight, so the day of the week follows the offset, and the twin complements it.</summary>
    [Theory]
    [InlineData("Z", "Friday", TruthValue.True)]
    [InlineData("+05:30", "Friday", TruthValue.False)]
    [InlineData("+05:30", "Saturday", TruthValue.True)]
    [InlineData("-02:00", "friday", TruthValue.True)]
    public async Task OnDayOfWeek_OffsetShiftsTheDate_ReadsTheDayInTheOffset_Test(
        string offset,
        string day,
        TruthValue expected
    )
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> on) =
            DateTimePredicates.OnDayOfWeek<DateContext>("on", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notOn) =
            DateTimePredicates.NotOnDayOfWeek<DateContext>("notOn", c => c.When);
        PredicateArguments args = Args(("days", Strings(day)), ("offset", LiteralValue.OfString(offset)));
        DateContext context = new(LateFridayUtc);

        Assert.Equal(expected, await RunAsync(on, context, args));
        Assert.Equal(Complement(expected), await RunAsync(notOn, context, args));
    }

    /// <summary>
    /// At a month end the offset decides the month: 2026-01-31 20:00 UTC is February at +05:00 and still January at
    /// -05:00. Year end works the same way. The twin complements each answer.
    /// </summary>
    [Theory]
    [InlineData("2026-01-31T20:00:00Z", "Z", 1, TruthValue.True)]
    [InlineData("2026-01-31T20:00:00Z", "+05:00", 1, TruthValue.False)]
    [InlineData("2026-01-31T20:00:00Z", "+05:00", 2, TruthValue.True)]
    [InlineData("2026-02-01T02:00:00Z", "-05:00", 1, TruthValue.True)]
    [InlineData("2026-12-31T23:00:00Z", "+01:00", 1, TruthValue.True)]
    [InlineData("2026-12-31T23:00:00Z", "+01:00", 12, TruthValue.False)]
    public async Task InMonth_OffsetCrossesAMonthEnd_ReadsTheMonthInTheOffset_Test(
        string instant,
        string offset,
        long month,
        TruthValue expected
    )
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inMonth) =
            DateTimePredicates.InMonth<DateContext>("inMonth", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInMonth) =
            DateTimePredicates.NotInMonth<DateContext>("notInMonth", c => c.When);
        PredicateArguments args = Args(("months", Longs(month)), ("offset", LiteralValue.OfString(offset)));
        DateContext context = new(DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture));

        Assert.Equal(expected, await RunAsync(inMonth, context, args));
        Assert.Equal(Complement(expected), await RunAsync(notInMonth, context, args));
    }

    /// <summary>
    /// With the default edges the window is [start, end): the start is inside, the end is outside. The time of day is read
    /// in the offset. The twin complements each answer.
    /// </summary>
    [Theory]
    [InlineData("08:59:59", TruthValue.False)]
    [InlineData("09:00:00", TruthValue.True)]
    [InlineData("12:00:00", TruthValue.True)]
    [InlineData("16:59:59", TruthValue.True)]
    [InlineData("17:00:00", TruthValue.False)]
    public Task InTimeWindow_DefaultEdges_IncludesStartAndExcludesEnd_Test(string localTime, TruthValue expected)
    {
        PredicateArguments args = Window(("start", "09:00"), ("end", "17:00"), ("offset", "+02:00"));

        return AssertWindowAsync(At("2026-10-09", localTime, "+02:00"), args, expected);
    }

    /// <summary>includeStart and includeEnd switch each edge between inclusive and exclusive.</summary>
    [Theory]
    [InlineData(false, false, "09:00:00", TruthValue.False)]
    [InlineData(false, false, "17:00:00", TruthValue.False)]
    [InlineData(true, true, "09:00:00", TruthValue.True)]
    [InlineData(true, true, "17:00:00", TruthValue.True)]
    [InlineData(false, true, "09:00:00", TruthValue.False)]
    [InlineData(false, true, "17:00:00", TruthValue.True)]
    public Task InTimeWindow_EdgeFlags_SetInclusiveOrExclusiveEdges_Test(
        bool includeStart,
        bool includeEnd,
        string localTime,
        TruthValue expected
    )
    {
        PredicateArguments args = Args(
            ("start", LiteralValue.OfString("09:00")),
            ("end", LiteralValue.OfString("17:00")),
            ("duration", LiteralValue.OfString(string.Empty)),
            ("includeStart", LiteralValue.OfBoolean(includeStart)),
            ("includeEnd", LiteralValue.OfBoolean(includeEnd)),
            ("offset", LiteralValue.OfString("Z"))
        );

        return AssertWindowAsync(At("2026-10-09", localTime, "Z"), args, expected);
    }

    /// <summary>A start later than the end makes a window that crosses midnight: 22:00 to 06:00 holds late evening and early morning.</summary>
    [Theory]
    [InlineData("21:59:59", TruthValue.False)]
    [InlineData("22:00:00", TruthValue.True)]
    [InlineData("23:59:59", TruthValue.True)]
    [InlineData("00:00:00", TruthValue.True)]
    [InlineData("05:59:59", TruthValue.True)]
    [InlineData("06:00:00", TruthValue.False)]
    [InlineData("12:00:00", TruthValue.False)]
    public Task InTimeWindow_StartLaterThanEnd_CrossesMidnight_Test(string localTime, TruthValue expected)
    {
        PredicateArguments args = Window(("start", "22:00"), ("end", "06:00"), ("offset", "Z"));

        return AssertWindowAsync(At("2026-10-09", localTime, "Z"), args, expected);
    }

    /// <summary>A duration replaces the end: 22:00 plus PT8H ends at 06:00 the next day, with the same edge rules.</summary>
    [Theory]
    [InlineData("21:59:59", TruthValue.False)]
    [InlineData("22:00:00", TruthValue.True)]
    [InlineData("03:00:00", TruthValue.True)]
    [InlineData("06:00:00", TruthValue.False)]
    public Task InTimeWindow_DurationForm_EndsAfterTheDuration_Test(string localTime, TruthValue expected)
    {
        PredicateArguments args = Window(("start", "22:00"), ("duration", "PT8H"), ("offset", "Z"));

        return AssertWindowAsync(At("2026-10-09", localTime, "Z"), args, expected);
    }

    /// <summary>The offset moves the time of day: 06:30 UTC is 12:00 at +05:30, inside a 09:00 to 17:00 window there and outside it in UTC.</summary>
    [Theory]
    [InlineData("Z", TruthValue.False)]
    [InlineData("+05:30", TruthValue.True)]
    public Task InTimeWindow_Offset_ReadsTheTimeOfDayInTheOffset_Test(string offset, TruthValue expected)
    {
        PredicateArguments args = Window(("start", "09:00"), ("end", "17:00"), ("offset", offset));

        return AssertWindowAsync(new DateTimeOffset(2026, 10, 9, 6, 30, 0, TimeSpan.Zero), args, expected);
    }

    /// <summary>An offset that is not 'Z' or '±hh:mm' in range is a problem the compiler reports, and a valid one is not.</summary>
    [Theory]
    [InlineData("Z", false)]
    [InlineData("+05:30", false)]
    [InlineData("-14:00", false)]
    [InlineData("+14:01", true)]
    [InlineData("+5:30", true)]
    [InlineData("+0530", true)]
    [InlineData("+05:60", true)]
    [InlineData("", true)]
    public void ArgumentValidator_Offset_ReportsOnlyAnInvalidOffset_Test(string offset, bool invalid)
    {
        PredicateSchema schema = DateTimePredicates.OnDayOfWeek<DateContext>("on", c => c.When).Schema;

        IReadOnlyList<PredicateArgumentProblem> problems = schema.ArgumentValidator!(
            Args(("days", Strings("Monday")), ("offset", LiteralValue.OfString(offset)))
        );

        Assert.Equal(invalid, problems.Count > 0);
    }

    /// <summary>A time zone name is rejected with a message that says time zone names are not supported.</summary>
    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("UTC")]
    public void ArgumentValidator_TimeZoneName_SaysNamesAreNotSupported_Test(string offset)
    {
        PredicateSchema schema = DateTimePredicates.InMonth<DateContext>("inMonth", c => c.When).Schema;

        PredicateArgumentProblem problem = Assert.Single(
            schema.ArgumentValidator!(Args(("months", Longs(1)), ("offset", LiteralValue.OfString(offset))))
        );

        Assert.Contains("time zone name", problem.Message, StringComparison.Ordinal);
        Assert.Contains("not supported", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>An unknown day name, an empty day list, a month out of 1 to 12 and an empty month list are each one problem.</summary>
    [Fact]
    public void ArgumentValidator_BadDaysOrMonths_ReportsOneProblemEach_Test()
    {
        Func<PredicateArguments, IReadOnlyList<PredicateArgumentProblem>> days = DateTimePredicates
            .OnDayOfWeek<DateContext>("on", c => c.When)
            .Schema.ArgumentValidator!;
        Func<PredicateArguments, IReadOnlyList<PredicateArgumentProblem>> months = DateTimePredicates
            .InMonth<DateContext>("inMonth", c => c.When)
            .Schema.ArgumentValidator!;
        LiteralValue utc = LiteralValue.OfString("Z");

        Assert.Single(days(Args(("days", Strings("Monday", "Funday")), ("offset", utc))));
        Assert.Single(days(Args(("days", Strings()), ("offset", utc))));
        Assert.Single(days(Args(("days", Strings("1")), ("offset", utc))));
        Assert.Single(months(Args(("months", Longs(0, 12)), ("offset", utc))));
        Assert.Single(months(Args(("months", Longs(13)), ("offset", utc))));
        Assert.Single(months(Args(("months", Longs()), ("offset", utc))));
        Assert.Empty(months(Args(("months", Longs(1, 12)), ("offset", utc))));
    }

    /// <summary>
    /// The window rules are checked at compile time: equal start and end, both or neither of end and duration, a duration
    /// out of range and a bad time of day are each a problem.
    /// </summary>
    [Theory]
    [InlineData("09:00", "09:00", "")]
    [InlineData("09:00", "17:00", "PT8H")]
    [InlineData("09:00", "", "")]
    [InlineData("09:00", "", "PT24H")]
    [InlineData("09:00", "", "PT0S")]
    [InlineData("09:00", "", "P1D")]
    [InlineData("25:00", "17:00", "")]
    [InlineData("09:00", "9:00", "")]
    public void ArgumentValidator_InvalidWindow_ReportsAProblem_Test(string start, string end, string duration)
    {
        PredicateSchema schema = DateTimePredicates.InTimeWindow<DateContext>("inWindow", c => c.When).Schema;

        IReadOnlyList<PredicateArgumentProblem> problems = schema.ArgumentValidator!(
            Window(("start", start), ("end", end), ("duration", duration), ("offset", "Z"))
        );

        Assert.Single(problems);
    }

    /// <summary>
    /// An end that comes from a data source is absent at compile time, so the validator does not report "neither end nor
    /// duration"; a valid literal window has no problem.
    /// </summary>
    [Fact]
    public void ArgumentValidator_EndFromADataSource_IsNotReportedMissing_Test()
    {
        PredicateSchema schema = DateTimePredicates.InTimeWindow<DateContext>("inWindow", c => c.When).Schema;
        PredicateArguments fromSource = Args(
            ("start", LiteralValue.OfString("09:00")),
            ("duration", LiteralValue.OfString(string.Empty)),
            ("offset", LiteralValue.OfString("Z"))
        );

        Assert.Empty(schema.ArgumentValidator!(fromSource));
        Assert.Empty(schema.ArgumentValidator(Window(("start", "09:00"), ("duration", "PT1H30M"), ("offset", "Z"))));
    }

    /// <summary>
    /// A bad value that reaches evaluation (from a data source) throws before the selector runs, so the engine records a
    /// fault even for a null selected value. The value is never corrected.
    /// </summary>
    [Fact]
    public async Task Evaluate_InvalidOffsetAtEvaluation_ThrowsArgumentException_Test()
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> on) =
            DateTimePredicates.OnDayOfWeek<DateContext>("on", c => c.When);
        PredicateArguments args = Args(("days", Strings("Monday")), ("offset", LiteralValue.OfString("Europe/Paris")));

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await RunAsync(on, new DateContext(null), args)
        );

        Assert.Contains("Europe/Paris", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A null selected value is Unknown for both members of each pair by default, and False with its twin True under NullBehavior.False.</summary>
    [Fact]
    public async Task Evaluate_NullSelectedValue_FollowsNullBehavior_Test()
    {
        DateContext missing = new(null);
        PredicateArguments days = Args(("days", Strings("Monday")), ("offset", LiteralValue.OfString("Z")));
        PredicateArguments months = Args(("months", Longs(1)), ("offset", LiteralValue.OfString("Z")));
        PredicateArguments window = Window(("start", "09:00"), ("end", "17:00"), ("offset", "Z"));

        const NullBehavior asFalse = NullBehavior.False;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> onDay = DateTimePredicates
            .OnDayOfWeek<DateContext>("p", c => c.When)
            .Evaluate;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notOnDay = DateTimePredicates
            .NotOnDayOfWeek<DateContext>("t", c => c.When)
            .Evaluate;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inMonth = DateTimePredicates
            .InMonth<DateContext>("p", c => c.When, nullBehavior: asFalse)
            .Evaluate;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInMonth = DateTimePredicates
            .NotInMonth<DateContext>("t", c => c.When, nullBehavior: asFalse)
            .Evaluate;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inWindow = DateTimePredicates
            .InTimeWindow<DateContext>("p", c => c.When)
            .Evaluate;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInWindow = DateTimePredicates
            .NotInTimeWindow<DateContext>("t", c => c.When, nullBehavior: asFalse)
            .Evaluate;

        Assert.Equal(TruthValue.Unknown, await RunAsync(onDay, missing, days));
        Assert.Equal(TruthValue.Unknown, await RunAsync(notOnDay, missing, days));
        Assert.Equal(TruthValue.False, await RunAsync(inMonth, missing, months));
        Assert.Equal(TruthValue.True, await RunAsync(notInMonth, missing, months));
        Assert.Equal(TruthValue.Unknown, await RunAsync(inWindow, missing, window));
        Assert.Equal(TruthValue.True, await RunAsync(notInWindow, missing, window));
    }

    private static async Task AssertWindowAsync(DateTimeOffset instant, PredicateArguments args, TruthValue expected)
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inWindow) =
            DateTimePredicates.InTimeWindow<DateContext>("inWindow", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInWindow) =
            DateTimePredicates.NotInTimeWindow<DateContext>("notInWindow", c => c.When);
        DateContext context = new(instant);

        Assert.Equal(expected, await RunAsync(inWindow, context, args));
        Assert.Equal(Complement(expected), await RunAsync(notInWindow, context, args));
    }

    /// <summary>Builds window arguments the way the compiler does: omitted end or duration is empty text, and the edge flags take their defaults.</summary>
    private static PredicateArguments Window(params (string Name, string Value)[] values)
    {
        Dictionary<string, LiteralValue> args = new(StringComparer.Ordinal)
        {
            ["end"] = LiteralValue.OfString(string.Empty),
            ["duration"] = LiteralValue.OfString(string.Empty),
            ["includeStart"] = LiteralValue.OfBoolean(true),
            ["includeEnd"] = LiteralValue.OfBoolean(false),
        };
        foreach ((string name, string value) in values)
        {
            args[name] = LiteralValue.OfString(value);
        }

        return new PredicateArguments(args);
    }

    private static DateTimeOffset At(string date, string localTime, string offset)
    {
        string zone = offset == "Z" ? "+00:00" : offset;
        return DateTimeOffset.Parse($"{date}T{localTime}{zone}", CultureInfo.InvariantCulture);
    }

    private static LiteralValue Longs(params long[] values)
    {
        return LiteralValue.OfArray(LiteralKind.Int64, [.. values.Select(LiteralValue.OfInt64)]);
    }

    private static LiteralValue Strings(params string[] values)
    {
        return LiteralValue.OfArray(LiteralKind.String, [.. values.Select(LiteralValue.OfString)]);
    }

    private static PredicateArguments Args(params (string Name, LiteralValue Value)[] values)
    {
        return new PredicateArguments(values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal));
    }

    private static ValueTask<TruthValue> RunAsync(
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate,
        DateContext context,
        PredicateArguments args
    )
    {
        return evaluate(context, args, CancellationToken.None);
    }

    private static TruthValue Complement(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }
}
