namespace TruthWeaver.Predicates;

using System.Text.RegularExpressions;

/// <summary>
/// Ready-made, generic regular-expression predicate factories, each parameterized by a selector
/// supplied at registration.
/// </summary>
public static class RegexPredicates
{
    /// <summary>The number of distinct patterns each pattern cache holds before it clears itself.</summary>
    internal const int PatternCacheCapacity = 256;

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly RegexPatternCache CaseSensitiveCache = new(RegexOptions.None, MatchTimeout, PatternCacheCapacity);

    private static readonly RegexPatternCache IgnoreCaseCache = new(
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout,
        PatternCacheCapacity
    );

    /// <summary>
    /// Creates a predicate that is true when the selected string matches the given regular-expression
    /// pattern argument (<see cref="System.Text.RegularExpressions.Regex"/>), with no regex options. The compiled
    /// <see cref="Regex"/> is cached per distinct pattern string in a process-wide cache of at most 256 patterns, so a
    /// pattern reused across many terms or evaluations is compiled once, not once per evaluation call. A pattern that is
    /// a literal in the rule is checked when the rule compiles: an invalid one is a <c>TRE0026</c> error. A pattern that
    /// comes from a data source is known only at evaluation, where an invalid pattern surfaces as a fault: the
    /// underlying <see cref="Regex"/> constructor throws, and the evaluator absorbs that as
    /// <see cref="TruthValue.Unknown"/> per ADR-0001's Kleene failure model, the same as any other predicate-evaluation
    /// failure.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">
    /// Reads the string value to test from the context. A <see langword="null"/> result answers per
    /// <paramref name="nullBehavior"/>, never a fault.
    /// </param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the regular-expression pattern.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or
    /// <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Matches<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Matches",
        string argumentName = "pattern",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string matches the given regular-expression pattern "
            + "(System.Text.RegularExpressions). The compiled pattern is cached per distinct pattern "
            + "string, not recompiled per evaluation. A literal pattern is validated when the rule compiles "
            + "(an invalid one is a compile error); an invalid pattern from a data source surfaces as an "
            + "evaluation-time fault (Unknown). "
            + "A null selected value is Unknown, never a fault, unless the host registers it with "
            + "NullBehavior.False, which makes it False.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The regular-expression pattern to match against.",
            negate: false,
            CaseSensitiveCache
        );
    }

    /// <summary>
    /// Creates the case-insensitive twin of <see cref="Matches{TContext}"/>: true when the selected string matches the
    /// pattern, ignoring case (<see cref="RegexOptions.IgnoreCase"/> with <see cref="RegexOptions.CultureInvariant"/>).
    /// It has the same cache bound, compile-time check of a literal pattern, null handling and fault behavior as
    /// <c>Matches</c>. An inline option in the pattern, such as <c>(?-i)</c>, still takes effect.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">
    /// Reads the string value to test from the context. A <see langword="null"/> result answers per
    /// <paramref name="nullBehavior"/>, never a fault.
    /// </param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the regular-expression pattern.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or
    /// <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) MatchesIgnoreCase<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Matches Ignore Case",
        string argumentName = "pattern",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string matches the given regular-expression pattern (System.Text.RegularExpressions), "
            + "ignoring case with the invariant culture. A literal pattern is validated when the rule compiles; an invalid "
            + "pattern from a data source surfaces as an evaluation-time fault (Unknown). "
            + "A null selected value is Unknown, never a fault, unless the host registers it with "
            + "NullBehavior.False, which makes it False.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The regular-expression pattern to match against, ignoring case.",
            negate: false,
            IgnoreCaseCache
        );
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="Matches{TContext}"/>: true when the selected string does not match
    /// the pattern. It is the Strong Kleene complement of <c>Matches</c>, so a <see langword="null"/> selection
    /// answers <see cref="TruthValue.Unknown"/> by default and <see cref="TruthValue.True"/> under
    /// <see cref="NullBehavior.False"/>. A literal pattern is checked when the rule compiles, as for <c>Matches</c>. An
    /// invalid pattern from a data source throws at evaluation time exactly as <c>Matches</c> does, which the evaluator
    /// records as a <c>Fault</c> and <see cref="TruthValue.Unknown"/> (ADR-0001).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the regular-expression pattern.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default, the same as the positive predicate) or <see cref="NullBehavior.False"/>. This twin answers the
    /// complement: <see cref="TruthValue.Unknown"/> or <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotMatches<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Not Matches",
        string argumentName = "pattern",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string does not match the given regular-expression pattern "
            + "(System.Text.RegularExpressions). The K3 complement of Matches: a null selected value answers Unknown "
            + "(never a fault), unless the host registers both with NullBehavior.False, which makes Matches "
            + "False and this predicate True. "
            + "A literal pattern is validated when the rule compiles; an invalid pattern from a data source "
            + "surfaces as an evaluation-time fault (Unknown).";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The regular-expression pattern the selected value must not match.",
            negate: true,
            CaseSensitiveCache
        );
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="MatchesIgnoreCase{TContext}"/>: true when the selected string does not
    /// match the pattern, ignoring case. It is the Strong Kleene complement of <c>MatchesIgnoreCase</c>, so a
    /// <see langword="null"/> selection answers <see cref="TruthValue.Unknown"/> by default and
    /// <see cref="TruthValue.True"/> under <see cref="NullBehavior.False"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the regular-expression pattern.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default, the same as the positive predicate) or <see cref="NullBehavior.False"/>. This twin answers the
    /// complement: <see cref="TruthValue.Unknown"/> or <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotMatchesIgnoreCase<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Not Matches Ignore Case",
        string argumentName = "pattern",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string does not match the given regular-expression pattern "
            + "(System.Text.RegularExpressions), ignoring case with the invariant culture. The K3 complement of "
            + "MatchesIgnoreCase: a null selected value answers Unknown (never a fault), unless the host registers both with "
            + "NullBehavior.False, which makes MatchesIgnoreCase False and this predicate True. "
            + "A literal pattern is validated when the rule compiles; an invalid pattern from a data source "
            + "surfaces as an evaluation-time fault (Unknown).";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The regular-expression pattern the selected value must not match, ignoring case.",
            negate: true,
            IgnoreCaseCache
        );
    }

    /// <summary>
    /// Builds a pattern predicate. A null selection answers per <paramref name="nullBehavior"/>, and
    /// <paramref name="negate"/> applies the Strong Kleene complement to every answer, the null one included.
    /// </summary>
    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Create<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, string?> selector,
        NullBehavior nullBehavior,
        string argumentName,
        string argumentDescription,
        bool negate,
        RegexPatternCache cache
    )
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, argumentDescription, LiteralKind.String)]
        )
        {
            ArgumentValidator = args => ValidatePattern(args, argumentName, cache),
        };

        return (
            schema,
            (context, args, _) =>
            {
                string? selected = selector(context);
                if (selected is null)
                {
                    return PredicateResult.ForNullAsync(nullBehavior, negate);
                }

                string pattern = args.GetString(argumentName);
                return PredicateResult.FromBoolAsync(cache.Get(pattern).IsMatch(selected) != negate);
            }
        );
    }

    /// <summary>
    /// Checks a literal pattern at compile time. A pattern that comes from a data source is absent from
    /// <paramref name="args"/> and is checked at evaluation instead.
    /// </summary>
    private static List<PredicateArgumentProblem> ValidatePattern(
        PredicateArguments args,
        string argumentName,
        RegexPatternCache cache
    )
    {
        List<PredicateArgumentProblem> problems = [];
        if (!args.TryGetString(argumentName, out string? pattern))
        {
            return problems;
        }

        try
        {
            cache.Validate(pattern);
        }
        catch (ArgumentException exception)
        {
            problems.Add(
                new PredicateArgumentProblem(
                    $"'{argumentName}' is not a valid regular expression: {exception.Message}",
                    "a valid .NET regular expression",
                    $"'{argumentName}' is '{pattern}'",
                    "Fix the pattern. Escape a literal metacharacter with a backslash."
                )
            );
        }

        return problems;
    }
}
