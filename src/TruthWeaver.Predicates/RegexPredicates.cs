namespace TruthWeaver.Predicates;

using System.Text.RegularExpressions;

/// <summary>
/// Ready-made, generic regular-expression predicate factories, each parameterized by a selector
/// supplied at registration.
/// </summary>
public static class RegexPredicates
{
    /// <summary>The number of distinct pattern and option pairs the pattern cache holds before it clears itself.</summary>
    internal const int PatternCacheCapacity = 256;

    private const string FlagsArgumentName = "flags";

    private const string FlagsArgumentDescription =
        "Optional regular-expression flags, as single letters: i (ignore case), m (multiline), s (single line), "
        + "n (explicit capture), x (ignore pattern white space). Defaults to none.";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly RegexPatternCache Cache = new(MatchTimeout, PatternCacheCapacity);

    /// <summary>
    /// Creates a predicate that is true when the selected string matches the given regular-expression
    /// pattern argument (<see cref="System.Text.RegularExpressions.Regex"/>). The optional rule argument <c>flags</c> is a
    /// string of the single letters <c>i</c> (ignore case, with the invariant culture), <c>m</c> (multiline),
    /// <c>s</c> (single line), <c>n</c> (explicit capture) and <c>x</c> (ignore pattern white space). It is empty when
    /// omitted, which means no options. The compiled <see cref="Regex"/> is cached per distinct pattern and options pair in
    /// a process-wide cache of at most 256 entries, so a pattern reused across many terms or evaluations is compiled once,
    /// not once per evaluation call. A pattern or flags string that is a literal in the rule is checked when the rule
    /// compiles: an invalid pattern, an unknown flag letter or a repeated flag letter is a <c>TRE0026</c> error. A pattern
    /// or flags string that comes from a data source is known only at evaluation, where an invalid value surfaces as a
    /// fault: the underlying <see cref="Regex"/> constructor or the flags parser throws, and the evaluator absorbs that as
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
    ) IsMatch<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Is Match",
        string argumentName = "pattern",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected string matches the given regular-expression pattern "
            + "(System.Text.RegularExpressions). The optional flags argument is a string of the letters i (ignore case, "
            + "invariant culture), m (multiline), s (single line), n (explicit capture) and x (ignore pattern white space). "
            + "The compiled pattern is cached per distinct pattern and flags, not recompiled per evaluation. A literal "
            + "pattern or flags string is validated when the rule compiles (an invalid one is a compile error); an invalid "
            + "value from a data source surfaces as an evaluation-time fault (Unknown), even when the selected value is null. "
            + "A null selected value is Unknown, never a fault, unless the host registers it with "
            + "NullBehavior.False, which makes it False.";
        return Create(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Regex),
            argumentName,
            "The regular-expression pattern to match against.",
            negate: false
        );
    }

    /// <summary>
    /// Creates the <c>IsNotX</c> twin of <see cref="IsMatch{TContext}"/>: true when the selected string does not match
    /// the pattern. It is the Strong Kleene complement of <c>IsMatch</c>, so a <see langword="null"/> selection
    /// answers <see cref="TruthValue.Unknown"/> by default and <see cref="TruthValue.True"/> under
    /// <see cref="NullBehavior.False"/>. It takes the same optional <c>flags</c> argument. A literal pattern or flags
    /// string is checked when the rule compiles, as for <c>IsMatch</c>. An invalid pattern or flags string from a data
    /// source throws at evaluation time exactly as <c>IsMatch</c> does, which the evaluator records as a <c>Fault</c> and
    /// <see cref="TruthValue.Unknown"/> (ADR-0001).
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
    ) IsNotMatch<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Is Not Match",
        string argumentName = "pattern",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected string does not match the given regular-expression pattern "
            + "(System.Text.RegularExpressions). The optional flags argument is the same as for IsMatch. The K3 complement "
            + "of IsMatch: a null selected value answers Unknown (never a fault), unless the host registers both with "
            + "NullBehavior.False, which makes IsMatch False and this predicate True. "
            + "A literal pattern or flags string is validated when the rule compiles; an invalid value from a data source "
            + "surfaces as an evaluation-time fault (Unknown).";
        return Create(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Regex),
            argumentName,
            "The regular-expression pattern the selected value must not match.",
            negate: true
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
        bool negate
    )
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [
                new PredicateArgumentSchema(argumentName, argumentDescription, LiteralKind.String),
                new PredicateArgumentSchema(
                    FlagsArgumentName,
                    FlagsArgumentDescription,
                    LiteralKind.String,
                    Required: false,
                    Default: LiteralValue.OfString(string.Empty)
                ),
            ]
        )
        {
            ArgumentValidator = args => ValidateArguments(args, argumentName),
        };

        return (
            schema,
            (context, args, _) =>
            {
                // Read and validate the pattern and flags first, so a bad data-source value faults even when the
                // selection is null. A null selection must not hide an invalid pattern until the first non-null value.
                string pattern = args.GetString(argumentName);

                // The schema defaults flags to empty. TryGet keeps a delegate that is invoked directly, with only the
                // pattern, behaving as that default instead of failing on a missing argument.
                args.TryGetString(FlagsArgumentName, out string? flags);
                RegexOptions options = ParseFlags(flags ?? string.Empty);
                Regex regex = Cache.Get(pattern, options);

                string? selected = selector(context);
                if (selected is null)
                {
                    return PredicateResult.ForNullAsync(nullBehavior, negate);
                }

                return PredicateResult.FromBoolAsync(regex.IsMatch(selected) != negate);
            }
        );
    }

    /// <summary>
    /// Converts the flags string to options at evaluation. A flags string that a data source supplies is not checked at
    /// compile time, so an invalid one throws here and the evaluator records a fault.
    /// </summary>
    private static RegexOptions ParseFlags(string flags)
    {
        return RegexFlags.TryParse(flags, out RegexOptions options, out string? error)
            ? options
            : throw new ArgumentException($"'{FlagsArgumentName}' is not valid: {error}", FlagsArgumentName);
    }

    /// <summary>
    /// Checks a literal flags string and a literal pattern at compile time. A value that comes from a data source is
    /// absent from <paramref name="args"/> and is checked at evaluation instead. The pattern is checked under the
    /// options the flags name, because a flag such as <c>x</c> changes which patterns are valid.
    /// </summary>
    private static List<PredicateArgumentProblem> ValidateArguments(PredicateArguments args, string argumentName)
    {
        List<PredicateArgumentProblem> problems = [];
        RegexOptions options = RegexOptions.None;
        bool flagsValid = true;
        if (
            args.TryGetString(FlagsArgumentName, out string? flags)
            && !RegexFlags.TryParse(flags, out options, out string? error)
        )
        {
            flagsValid = false;
            problems.Add(
                new PredicateArgumentProblem(
                    $"'{FlagsArgumentName}' is not valid: {error}",
                    $"zero or more of the letters {RegexFlags.Letters}, each at most once",
                    $"'{FlagsArgumentName}' is '{flags}'",
                    "Remove the unknown or repeated letter."
                )
            );
        }

        // With invalid flags the options are unknown, so the pattern is not checked: one problem at a time.
        if (flagsValid && args.TryGetString(argumentName, out string? pattern))
        {
            try
            {
                Cache.Validate(pattern, options);
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
        }

        return problems;
    }
}
