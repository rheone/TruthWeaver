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
