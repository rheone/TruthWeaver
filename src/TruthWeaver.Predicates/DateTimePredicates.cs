namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made <see cref="DateTimeOffset"/> comparison predicate factories, each parameterized by a
/// <c>Func&lt;TContext, DateTimeOffset?&gt;</c> selector supplied at registration and
/// <see cref="DateTimeOffset"/> literal arguments supplied in rule text. Values compare by instant, so the same
/// moment written with different offsets is equal.
/// </summary>
/// <remarks>
/// <para>
/// There is no <see cref="DateTime"/> literal kind and no <see cref="DateTime"/> overload. A <see cref="DateTime"/>
/// has no offset and its <see cref="DateTime.Kind"/> may be unspecified, so converting it depends on the host's time
/// zone. A host that holds a <see cref="DateTime"/> converts it to a <see cref="DateTimeOffset"/> in its selector,
/// for example <c>c =&gt; new DateTimeOffset(c.CreatedUtc, TimeSpan.Zero)</c> for a UTC value.
/// </para>
/// <para>
/// A null selected value is <see cref="TruthValue.Unknown"/> by default (the K3 reading of a missing value), or a
/// definite <see cref="TruthValue.False"/> when the host passes <see cref="NullBehavior.False"/>. Every positive
/// predicate has a <c>NotX</c> twin that is its Strong Kleene complement. <c>Outside</c> is the twin of
/// <c>Between</c>.
/// </para>
/// </remarks>
public static class DateTimePredicates
{
    private const string NullNote =
        " A null selected value is Unknown, never a fault, unless the host registers it with NullBehavior.False, "
        + "which makes the positive predicate False (and its twin True). A DateTime is converted to a DateTimeOffset "
        + "by the host in the selector; there is no DateTime argument kind.";

    private const string ClockNote =
        " The predicate takes no rule-text arguments. A null selected value is Unknown, never a fault, unless the host registers it with NullBehavior.False, "
        + "which makes the positive predicate False (and its twin True).";

    /// <summary>Creates a strict after (<c>&gt;</c>) predicate: true when the selected instant is later than the argument.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>. Convert a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> here.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the instant to compare against.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default for this comparison family) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) After<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "After",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected instant is strictly later than the argument (>); an equal instant is False. Instants compare regardless of offset."
            + NullNote;
        return Single(
            name,
            label,
            description,
            selector,
            nullBehavior,
            negate: false,
            argumentName,
            static (selected, bound) => selected > bound
        );
    }

    /// <summary>Creates the <c>NotAfter</c> twin of <see cref="After{TContext}"/>: its Strong Kleene complement (true for an equal or earlier instant, Unknown stays Unknown).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>. Convert a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> here.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the instant to compare against.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotAfter<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Not After",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "The Strong Kleene complement of After: True when the selected instant is equal to or earlier than the argument, False when it is later, Unknown when After is Unknown."
            + NullNote;
        return Single(
            name,
            label,
            description,
            selector,
            nullBehavior,
            negate: true,
            argumentName,
            static (selected, bound) => selected > bound
        );
    }

    /// <summary>Creates a strict before (<c>&lt;</c>) predicate: true when the selected instant is earlier than the argument.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>. Convert a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> here.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the instant to compare against.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default for this comparison family) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Before<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Before",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected instant is strictly earlier than the argument (<); an equal instant is False. Instants compare regardless of offset."
            + NullNote;
        return Single(
            name,
            label,
            description,
            selector,
            nullBehavior,
            negate: false,
            argumentName,
            static (selected, bound) => selected < bound
        );
    }

    /// <summary>Creates the <c>NotBefore</c> twin of <see cref="Before{TContext}"/>: its Strong Kleene complement (true for an equal or later instant, Unknown stays Unknown).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>. Convert a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> here.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the instant to compare against.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotBefore<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Not Before",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "The Strong Kleene complement of Before: True when the selected instant is equal to or later than the argument, False when it is earlier, Unknown when Before is Unknown."
            + NullNote;
        return Single(
            name,
            label,
            description,
            selector,
            nullBehavior,
            negate: true,
            argumentName,
            static (selected, bound) => selected < bound
        );
    }

    /// <summary>
    /// Creates an inclusive range predicate: true when <c>lower &lt;= selected &lt;= upper</c>. Reversed bounds
    /// (<c>lower &gt; upper</c>) are an authoring error: evaluation throws <see cref="ArgumentException"/>, which the
    /// engine records as an <see cref="TruthValue.Unknown"/> result with a fault. The bounds are never swapped silently.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>. Convert a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> here.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="lowerName">The rule-text argument name for the inclusive lower bound.</param>
    /// <param name="upperName">The rule-text argument name for the inclusive upper bound.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default for this comparison family) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Between<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Between",
        string lowerName = "lower",
        string upperName = "upper",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected instant is between the two bounds, inclusive on both ends (lower <= value <= upper). Reversed bounds (lower later than upper) are an authoring error and fault the evaluation; they are never swapped."
            + NullNote;
        return Range(name, label, description, selector, nullBehavior, negate: false, lowerName, upperName);
    }

    /// <summary>
    /// Creates the <c>Outside</c> twin of <see cref="Between{TContext}"/>: its exact Strong Kleene complement
    /// (true when the instant is earlier than the lower bound or later than the upper bound). Reversed bounds are
    /// an authoring error and throw <see cref="ArgumentException"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>. Convert a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> here.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="lowerName">The rule-text argument name for the inclusive lower bound of the excluded range.</param>
    /// <param name="upperName">The rule-text argument name for the inclusive upper bound of the excluded range.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers for <c>Between</c>: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Outside<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Outside",
        string lowerName = "lower",
        string upperName = "upper",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "The Strong Kleene complement of Between: True when the selected instant is earlier than the lower bound or later than the upper bound, False when it is within the inclusive range, Unknown when Between is Unknown. Reversed bounds are an authoring error and fault the evaluation."
            + NullNote;
        return Range(name, label, description, selector, nullBehavior, negate: true, lowerName, upperName);
    }

    /// <summary>
    /// Creates a predicate that is true when the selected instant is later than the current time, read from
    /// <paramref name="timeProvider"/>. An instant equal to now is <see cref="TruthValue.False"/>.
    /// </summary>
    /// <remarks>
    /// The predicate reads the clock once each time the engine evaluates it, at evaluation time and not at
    /// registration. The engine evaluates one term (one predicate name with the same arguments) once per
    /// <c>Evaluate</c> call and reuses the answer, so a repeated term sees one instant. Two different terms, such as
    /// <c>AfterNow</c> and <c>BeforeNow</c> over different selectors, each read the clock and may see different
    /// instants when the clock advances between them. A shared snapshot across terms needs engine support that does
    /// not exist, so a host that needs one injects a <see cref="TimeProvider"/> that returns a fixed instant per
    /// evaluation.
    /// </remarks>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="timeProvider">The host-supplied clock. There is no ambient default, so a test can pass a fake provider.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) AfterNow<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        TimeProvider timeProvider,
        string label = "After Now",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected instant is strictly later than now, read from the host's TimeProvider; an instant equal to now is False."
            + ClockNote;
        return Clock(name, label, description, selector, timeProvider, nullBehavior, negate: false, after: true);
    }

    /// <summary>
    /// Creates the <c>NotAfterNow</c> twin of <see cref="AfterNow{TContext}"/>: its Strong Kleene complement (true for
    /// an instant equal to or earlier than now, Unknown stays Unknown). It is not the same predicate as
    /// <see cref="BeforeNow{TContext}"/>, because an instant equal to now makes both positive forms false.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="timeProvider">The host-supplied clock. There is no ambient default.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>.</param>
    /// <returns>The predicate's schema and evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotAfterNow<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        TimeProvider timeProvider,
        string label = "Not After Now",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "The Strong Kleene complement of AfterNow: True when the selected instant is equal to or earlier than now (read from the host's TimeProvider), False when it is later, Unknown when AfterNow is Unknown."
            + ClockNote;
        return Clock(name, label, description, selector, timeProvider, nullBehavior, negate: true, after: true);
    }

    /// <summary>
    /// Creates a predicate that is true when the selected instant is earlier than the current time, read from
    /// <paramref name="timeProvider"/>. An instant equal to now is <see cref="TruthValue.False"/>. See
    /// <see cref="AfterNow{TContext}"/> for when the clock is read.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="timeProvider">The host-supplied clock. There is no ambient default, so a test can pass a fake provider.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) BeforeNow<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        TimeProvider timeProvider,
        string label = "Before Now",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected instant is strictly earlier than now, read from the host's TimeProvider; an instant equal to now is False."
            + ClockNote;
        return Clock(name, label, description, selector, timeProvider, nullBehavior, negate: false, after: false);
    }

    /// <summary>
    /// Creates the <c>NotBeforeNow</c> twin of <see cref="BeforeNow{TContext}"/>: its Strong Kleene complement (true
    /// for an instant equal to or later than now, Unknown stays Unknown). It is not the same predicate as
    /// <see cref="AfterNow{TContext}"/>, because an instant equal to now makes both positive forms false.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the instant from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="timeProvider">The host-supplied clock. There is no ambient default.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>.</param>
    /// <returns>The predicate's schema and evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotBeforeNow<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        TimeProvider timeProvider,
        string label = "Not Before Now",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "The Strong Kleene complement of BeforeNow: True when the selected instant is equal to or later than now (read from the host's TimeProvider), False when it is earlier, Unknown when BeforeNow is Unknown."
            + ClockNote;
        return Clock(name, label, description, selector, timeProvider, nullBehavior, negate: true, after: false);
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Clock<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, DateTimeOffset?> selector,
        TimeProvider timeProvider,
        NullBehavior nullBehavior,
        bool negate,
        bool after
    )
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        PredicateSchema schema = new(name, label, description, []);

        return (
            schema,
            (context, _, _) =>
            {
                DateTimeOffset? selected = selector(context);
                if (selected is not { } value)
                {
                    return NullAnswerAsync(nullBehavior, negate);
                }

                // The clock is read here, per evaluation, never at registration or from an ambient source.
                DateTimeOffset now = timeProvider.GetUtcNow();
                bool positive = after ? value > now : value < now;
                return PredicateResult.FromBoolAsync(positive != negate);
            }
        );
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Single<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, DateTimeOffset?> selector,
        NullBehavior nullBehavior,
        bool negate,
        string argumentName,
        Func<DateTimeOffset, DateTimeOffset, bool> test
    )
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [
                new PredicateArgumentSchema(
                    argumentName,
                    "The instant to compare the selected value against.",
                    LiteralKind.DateTimeOffset
                ),
            ]
        );

        return (
            schema,
            (context, args, _) =>
            {
                DateTimeOffset? selected = selector(context);
                return selected is { } value
                    ? PredicateResult.FromBoolAsync(test(value, args.GetDateTimeOffset(argumentName)) != negate)
                    : NullAnswerAsync(nullBehavior, negate);
            }
        );
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Range<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, DateTimeOffset?> selector,
        NullBehavior nullBehavior,
        bool negate,
        string lowerName,
        string upperName
    )
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [
                new PredicateArgumentSchema(lowerName, "The inclusive lower bound.", LiteralKind.DateTimeOffset),
                new PredicateArgumentSchema(upperName, "The inclusive upper bound.", LiteralKind.DateTimeOffset),
            ]
        );

        return (
            schema,
            (context, args, _) =>
            {
                DateTimeOffset lower = args.GetDateTimeOffset(lowerName);
                DateTimeOffset upper = args.GetDateTimeOffset(upperName);
                if (lower > upper)
                {
                    // Reversed bounds hide an authoring mistake, so they fault instead of being swapped.
                    throw new ArgumentException(
                        $"The lower bound '{lower:O}' is later than the upper bound '{upper:O}' in predicate '{name}'.",
                        lowerName
                    );
                }

                DateTimeOffset? selected = selector(context);
                return selected is { } value
                    ? PredicateResult.FromBoolAsync((value >= lower && value <= upper) != negate)
                    : NullAnswerAsync(nullBehavior, negate);
            }
        );
    }

    /// <summary>Gets the answer for a null selection: the configured positive answer, complemented for a twin.</summary>
    private static ValueTask<TruthValue> NullAnswerAsync(NullBehavior nullBehavior, bool negate)
    {
        // Unknown complements to itself; a configured False complements to True for the twin.
        return negate && nullBehavior == NullBehavior.False
            ? PredicateResult.FromBoolAsync(true)
            : PredicateResult.ForNullAsync(nullBehavior);
    }
}
