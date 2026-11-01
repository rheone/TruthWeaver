namespace TruthWeaver.Predicates;

using System.Globalization;

/// <content>
/// The calendar predicates. Each reads the selected instant in a fixed offset from UTC (<c>Z</c> or <c>±hh:mm</c>) that
/// the rule gives in the <c>offset</c> argument. There are no time zone names and no daylight-saving rules: a host that
/// needs local time in a zone with daylight saving selects the instant already in the right offset, or registers one
/// predicate per offset. The argument names are fixed. Every argument value is checked at compile time when it is a
/// literal (<c>TRE0026</c>), and at evaluation otherwise, where a bad value throws <see cref="ArgumentException"/> and
/// the engine records <see cref="TruthValue.Unknown"/> with a fault.
/// </content>
public static partial class DateTimePredicates
{
    private const string OffsetArgument = "offset";
    private const string DaysArgument = "days";
    private const string MonthsArgument = "months";
    private const string StartArgument = "start";
    private const string EndArgument = "end";
    private const string DurationArgument = "duration";
    private const string IncludeStartArgument = "includeStart";
    private const string IncludeEndArgument = "includeEnd";

    private const string OffsetNote =
        " The instant is read in the fixed offset given by the 'offset' argument: 'Z' or '+hh:mm' / '-hh:mm'. Time zone names are not accepted."
        + " A null selected value is Unknown, never a fault, unless the host registers it with NullBehavior.False, "
        + "which makes the positive predicate False (and its twin True).";

    private static readonly PredicateArgumentSchema OffsetSchema = new(
        OffsetArgument,
        "The fixed offset from UTC to read the instant in: 'Z' or '+hh:mm' / '-hh:mm', for example '+05:30'. Time zone names such as 'Europe/Paris' are not accepted.",
        LiteralKind.String
    );

    /// <summary>
    /// Creates a predicate that is true when the selected instant, read in the fixed offset of the <c>offset</c>
    /// argument, falls on one of the days in the <c>days</c> argument. Days are English names (<c>Monday</c> to
    /// <c>Sunday</c>), matched ignoring case. An empty list or an unknown name is an authoring error.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsOnDayOfWeek<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is On Day Of Week",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected instant falls on one of the listed days of the week (English names, Monday to Sunday, case ignored)."
            + OffsetNote;
        return DayOfWeekPredicate(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.DateTime),
            negate: false
        );
    }

    /// <summary>
    /// Creates the <c>IsNotOnDayOfWeek</c> twin of <see cref="IsOnDayOfWeek{TContext}"/>: its Strong Kleene complement (true
    /// when the instant, read in the offset, falls on none of the listed days; Unknown stays Unknown).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotOnDayOfWeek<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Not On Day Of Week",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "The Strong Kleene complement of IsOnDayOfWeek: True when the selected instant falls on none of the listed days of the week, False when it falls on one, Unknown when IsOnDayOfWeek is Unknown."
            + OffsetNote;
        return DayOfWeekPredicate(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.DateTime),
            negate: true
        );
    }

    /// <summary>
    /// Creates a predicate that is true when the selected instant, read in the fixed offset of the <c>offset</c>
    /// argument, falls in one of the months in the <c>months</c> argument. Months are numbers from 1 (January) to 12
    /// (December). An empty list or a number out of range is an authoring error.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsInMonth<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is In Month",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected instant falls in one of the listed months (numbers from 1, January, to 12, December)."
            + OffsetNote;
        return MonthPredicate(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.DateTime),
            negate: false
        );
    }

    /// <summary>
    /// Creates the <c>IsNotInMonth</c> twin of <see cref="IsInMonth{TContext}"/>: its Strong Kleene complement (true when the
    /// instant, read in the offset, falls in none of the listed months; Unknown stays Unknown).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotInMonth<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Not In Month",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "The Strong Kleene complement of IsInMonth: True when the selected instant falls in none of the listed months, False when it falls in one, Unknown when IsInMonth is Unknown."
            + OffsetNote;
        return MonthPredicate(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.DateTime),
            negate: true
        );
    }

    /// <summary>
    /// Creates a predicate that is true when the time of day of the selected instant, read in the fixed offset of the
    /// <c>offset</c> argument, is inside a daily window. The window starts at <c>start</c> and ends at <c>end</c>, or
    /// <c>duration</c> after <c>start</c>. By default the window is <c>[start, end)</c>: <c>includeStart</c> defaults to
    /// <see langword="true"/> and <c>includeEnd</c> to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>start</c> and <c>end</c> are times of day written <c>hh:mm</c> or <c>hh:mm:ss</c>. <c>duration</c> is an ISO 8601
    /// time duration (<c>PTnHnMnS</c>), longer than zero and shorter than 24 hours. A call gives exactly one of
    /// <c>end</c> and <c>duration</c>. An omitted argument means "not given"; an empty text is a value, and an invalid one.
    /// </para>
    /// <para>
    /// A start later than the end makes a window that crosses midnight, for example 22:00 to 06:00. A start equal to the
    /// end is an authoring error, because the window is either empty or the whole day.
    /// </para>
    /// </remarks>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsInTimeWindow<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is In Time Window",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the time of day of the selected instant is inside the window from 'start' to 'end' (or 'start' plus 'duration'). "
            + "By default the start is inside and the end is outside; 'includeStart' and 'includeEnd' change this. A start later than the end crosses midnight."
            + OffsetNote;
        return TimeWindowPredicate(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.DateTime),
            negate: false
        );
    }

    /// <summary>
    /// Creates the <c>IsNotInTimeWindow</c> twin of <see cref="IsInTimeWindow{TContext}"/>: its Strong Kleene complement (true
    /// when the time of day, read in the offset, is outside the window; Unknown stays Unknown). The arguments and their
    /// rules are those of <see cref="IsInTimeWindow{TContext}"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotInTimeWindow<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Not In Time Window",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "The Strong Kleene complement of IsInTimeWindow: True when the time of day of the selected instant is outside the window, False when it is inside, Unknown when IsInTimeWindow is Unknown."
            + OffsetNote;
        return TimeWindowPredicate(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.DateTime),
            negate: true
        );
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) TimeWindowPredicate<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, DateTimeOffset?> selector,
        NullBehavior nullBehavior,
        bool negate
    )
    {
        // end and duration have no default: an omitted one is absent. The validator tells it from a data-source argument,
        // which is also absent, by PredicateArguments.IsDeferred, and reports a call that gives neither at compile time.
        PredicateArgumentSchema[] arguments =
        [
            new(StartArgument, "The time of day the window starts: 'hh:mm' or 'hh:mm:ss'.", LiteralKind.String),
            new(
                EndArgument,
                "The time of day the window ends: 'hh:mm' or 'hh:mm:ss'. Give this or 'duration', not both.",
                LiteralKind.String,
                Required: false
            ),
            new(
                DurationArgument,
                "The length of the window as an ISO 8601 time duration, for example 'PT8H' or 'PT1H30M'. Longer than zero and shorter than 24 hours. Give this or 'end', not both.",
                LiteralKind.String,
                Required: false
            ),
            new(
                IncludeStartArgument,
                "Whether the start time is inside the window. Defaults to true.",
                LiteralKind.Boolean,
                Required: false,
                Default: LiteralValue.OfBoolean(true)
            ),
            new(
                IncludeEndArgument,
                "Whether the end time is inside the window. Defaults to false.",
                LiteralKind.Boolean,
                Required: false,
                Default: LiteralValue.OfBoolean(false)
            ),
            OffsetSchema,
        ];
        return FixedOffsetPredicate(name, label, description, arguments, selector, nullBehavior, negate, BindTimeWindow);
    }

    /// <summary>Reads the window and <c>offset</c> arguments into a test over the selected instant.</summary>
    private static Func<DateTimeOffset, bool>? BindTimeWindow(PredicateArguments args, List<PredicateArgumentProblem> problems)
    {
        bool offsetRead = TryReadOffset(args, problems, out TimeSpan offset);
        bool startRead = TryReadTimeOfDay(args, StartArgument, problems, out TimeSpan start);
        TimeSpan? end = ReadWindowEnd(args, problems, start, startRead);

        if (startRead && end is { } endTime && endTime == start)
        {
            string text = FormatTimeOfDay(start);
            problems.Add(
                new PredicateArgumentProblem(
                    $"the window starts and ends at {text}, so it is either empty or the whole day.",
                    $"'{EndArgument}' different from '{StartArgument}'",
                    $"'{StartArgument}' and '{EndArgument}' are both {text}",
                    "Change the end, or remove the predicate if the whole day is meant."
                )
            );
        }

        // An absent flag comes from a data source at evaluation; the compiler fills the defaults for an omitted one.
        if (
            !offsetRead
            || end is not { } windowEnd
            || problems.Count > 0
            || !args.TryGetBool(IncludeStartArgument, out bool includeStart)
            || !args.TryGetBool(IncludeEndArgument, out bool includeEnd)
        )
        {
            return null;
        }

        return instant =>
        {
            TimeSpan time = LocalCalendar.TimeOfDayOf(instant, offset);
            bool afterStart = time > start || (includeStart && time == start);
            bool beforeEnd = time < windowEnd || (includeEnd && time == windowEnd);

            // A window that crosses midnight is the union of [start, 24:00) and [00:00, end).
            return start < windowEnd ? afterStart && beforeEnd : afterStart || beforeEnd;
        };
    }

    /// <summary>
    /// Reads the end of the window from <c>end</c> or <c>start</c> plus <c>duration</c>. Returns <see langword="null"/> when
    /// the end is not known: an argument from a data source at compile time, or a problem. An argument that is absent
    /// and not deferred was omitted, which means "not given". At evaluation nothing is deferred, so an absent argument
    /// is an omitted one.
    /// </summary>
    private static TimeSpan? ReadWindowEnd(
        PredicateArguments args,
        List<PredicateArgumentProblem> problems,
        TimeSpan start,
        bool startRead
    )
    {
        bool endGiven = args.TryGetString(EndArgument, out _);
        bool durationGiven = args.TryGetString(DurationArgument, out string? durationText);

        // A data-source argument has no value yet, so the call may still give it. Only an argument that is neither given
        // nor deferred is known to be omitted.
        bool endOmitted = !endGiven && !args.IsDeferred(EndArgument);
        bool durationOmitted = !durationGiven && !args.IsDeferred(DurationArgument);

        if ((endGiven && durationGiven) || (endOmitted && durationOmitted))
        {
            problems.Add(
                new PredicateArgumentProblem(
                    endGiven
                        ? $"the window gives both '{EndArgument}' and '{DurationArgument}'."
                        : $"the window gives neither '{EndArgument}' nor '{DurationArgument}'.",
                    $"exactly one of '{EndArgument}' and '{DurationArgument}'",
                    endGiven ? "both" : "neither",
                    $"Give either '{EndArgument}' or '{DurationArgument}'."
                )
            );
            return null;
        }

        if (endGiven)
        {
            return TryReadTimeOfDay(args, EndArgument, problems, out TimeSpan end) ? end : null;
        }

        if (!durationGiven)
        {
            return null;
        }

        if (!FixedOffsetText.TryParseDuration(durationText!, out TimeSpan duration, out string error))
        {
            problems.Add(
                new PredicateArgumentProblem(
                    error,
                    "an ISO 8601 time duration longer than zero and shorter than 24 hours",
                    $"'{DurationArgument}' is '{durationText}'",
                    "Use 'PTnHnMnS', for example 'PT8H'."
                )
            );
            return null;
        }

        // The duration is shorter than a day, so the end wraps past midnight at most once.
        return startRead ? TimeSpan.FromTicks((start + duration).Ticks % TimeSpan.TicksPerDay) : null;
    }

    /// <summary>Reads one time-of-day argument. Returns <see langword="false"/> when it is absent or not valid; only the second adds a problem.</summary>
    private static bool TryReadTimeOfDay(
        PredicateArguments args,
        string argument,
        List<PredicateArgumentProblem> problems,
        out TimeSpan time
    )
    {
        time = TimeSpan.Zero;
        if (!args.TryGetString(argument, out string? text))
        {
            return false;
        }

        if (FixedOffsetText.TryParseTimeOfDay(text, out time, out string error))
        {
            return true;
        }

        problems.Add(
            new PredicateArgumentProblem(
                error,
                "a time of day 'hh:mm' or 'hh:mm:ss'",
                $"'{argument}' is '{text}'",
                "Use a time of day such as '09:00'."
            )
        );
        return false;
    }

    private static string FormatTimeOfDay(TimeSpan time)
    {
        return time.ToString(time.Seconds == 0 ? @"hh\:mm" : @"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) MonthPredicate<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, DateTimeOffset?> selector,
        NullBehavior nullBehavior,
        bool negate
    )
    {
        PredicateArgumentSchema[] arguments =
        [
            new(
                MonthsArgument,
                "The months to match: numbers from 1 (January) to 12 (December). At least one.",
                LiteralKind.Int64Array
            ),
            OffsetSchema,
        ];
        return FixedOffsetPredicate(name, label, description, arguments, selector, nullBehavior, negate, BindMonths);
    }

    /// <summary>Reads the <c>months</c> and <c>offset</c> arguments into a test over the selected instant.</summary>
    private static Func<DateTimeOffset, bool>? BindMonths(PredicateArguments args, List<PredicateArgumentProblem> problems)
    {
        bool offsetRead = TryReadOffset(args, problems, out TimeSpan offset);
        if (!args.TryGetRaw(MonthsArgument, out _))
        {
            return null;
        }

        IReadOnlyList<long> numbers = args.GetInt64Array(MonthsArgument);
        HashSet<int> months = [];
        foreach (long number in numbers)
        {
            if (number is >= 1 and <= 12)
            {
                months.Add((int)number);
                continue;
            }

            string text = number.ToString(CultureInfo.InvariantCulture);
            problems.Add(
                new PredicateArgumentProblem(
                    $"{text} in '{MonthsArgument}' is not a month number.",
                    "month numbers from 1 to 12",
                    text,
                    "Use a number from 1 (January) to 12 (December)."
                )
            );
        }

        if (numbers.Count == 0)
        {
            problems.Add(EmptyList(MonthsArgument, "month"));
        }

        return offsetRead && problems.Count == 0 ? instant => months.Contains(LocalCalendar.MonthOf(instant, offset)) : null;
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) DayOfWeekPredicate<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, DateTimeOffset?> selector,
        NullBehavior nullBehavior,
        bool negate
    )
    {
        PredicateArgumentSchema[] arguments =
        [
            new(
                DaysArgument,
                "The days of the week to match: English names from Monday to Sunday, case ignored. At least one.",
                LiteralKind.StringArray
            ),
            OffsetSchema,
        ];
        return FixedOffsetPredicate(name, label, description, arguments, selector, nullBehavior, negate, BindDays);
    }

    /// <summary>Reads the <c>days</c> and <c>offset</c> arguments into a test over the selected instant.</summary>
    private static Func<DateTimeOffset, bool>? BindDays(PredicateArguments args, List<PredicateArgumentProblem> problems)
    {
        bool offsetRead = TryReadOffset(args, problems, out TimeSpan offset);
        if (!args.TryGetRaw(DaysArgument, out _))
        {
            return null;
        }

        IReadOnlyList<string> names = args.GetStringArray(DaysArgument);
        HashSet<DayOfWeek> days = [];
        foreach (string text in names)
        {
            if (FixedOffsetText.TryParseDayOfWeek(text, out DayOfWeek day))
            {
                days.Add(day);
                continue;
            }

            problems.Add(
                new PredicateArgumentProblem(
                    $"'{text}' in '{DaysArgument}' is not a day of the week.",
                    "English day names from Monday to Sunday",
                    $"'{text}'",
                    "Use a full English day name, such as 'Monday'."
                )
            );
        }

        if (names.Count == 0)
        {
            problems.Add(EmptyList(DaysArgument, "day"));
        }

        return offsetRead && problems.Count == 0 ? instant => days.Contains(LocalCalendar.DayOfWeekOf(instant, offset)) : null;
    }

    /// <summary>
    /// Builds a calendar predicate. <paramref name="bind"/> reads the arguments into a test over the selected instant, or
    /// adds problems. The schema's validator runs it over the literal arguments at compile time, where an argument from a
    /// data source is absent and skipped. Evaluation runs it again over every argument, before the selector, so a bad
    /// value from a data source faults even for a null selected value.
    /// </summary>
    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) FixedOffsetPredicate<TContext>(
        string name,
        string label,
        string description,
        PredicateArgumentSchema[] arguments,
        Func<TContext, DateTimeOffset?> selector,
        NullBehavior nullBehavior,
        bool negate,
        Func<PredicateArguments, List<PredicateArgumentProblem>, Func<DateTimeOffset, bool>?> bind
    )
    {
        PredicateSchema schema = new(name, label, description, arguments)
        {
            ArgumentValidator = args =>
            {
                List<PredicateArgumentProblem> problems = [];
                _ = bind(args, problems);
                return problems;
            },
        };

        return (
            schema,
            (context, args, _) =>
            {
                List<PredicateArgumentProblem> problems = [];
                Func<DateTimeOffset, bool>? test = bind(args, problems);
                if (test is null)
                {
                    string reason = problems.Count > 0 ? problems[0].Message : "a required argument has no value.";
                    throw new ArgumentException($"Predicate '{name}' has invalid argument values: {reason}", nameof(args));
                }

                return selector(context) is { } instant
                    ? PredicateResult.FromBoolAsync(test(instant) != negate)
                    : PredicateResult.ForNullAsync(nullBehavior, negate);
            }
        );
    }

    /// <summary>Reads the <c>offset</c> argument. Returns <see langword="false"/> when it is absent or not valid; only the second adds a problem.</summary>
    private static bool TryReadOffset(PredicateArguments args, List<PredicateArgumentProblem> problems, out TimeSpan offset)
    {
        offset = TimeSpan.Zero;
        if (!args.TryGetString(OffsetArgument, out string? text))
        {
            return false;
        }

        if (FixedOffsetText.TryParseOffset(text, out offset, out string error))
        {
            return true;
        }

        problems.Add(
            new PredicateArgumentProblem(
                error,
                "'Z' or a fixed offset '+hh:mm' / '-hh:mm'",
                $"'{OffsetArgument}' is '{text}'",
                "Use 'Z' or a fixed offset such as '+01:00'."
            )
        );
        return false;
    }

    private static PredicateArgumentProblem EmptyList(string argument, string noun)
    {
        return new PredicateArgumentProblem(
            $"'{argument}' is empty, so the predicate can never be True.",
            string.Create(CultureInfo.InvariantCulture, $"at least one {noun} in '{argument}'"),
            $"'{argument}' is []",
            $"Add at least one {noun}."
        );
    }
}
