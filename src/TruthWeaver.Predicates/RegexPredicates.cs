namespace TruthWeaver.Predicates;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made, generic regular-expression predicate factories, each parameterized by a selector
/// supplied at registration.
/// </summary>
public static class RegexPredicates
{
    private static readonly ConcurrentDictionary<string, Regex> PatternCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a predicate that is true when the selected string matches the given regular-expression
    /// pattern argument (<see cref="System.Text.RegularExpressions.Regex"/>). The compiled
    /// <see cref="Regex"/> is cached per distinct pattern string in a process-wide cache, so a pattern
    /// reused across many terms or evaluations is compiled once, not once per evaluation call.
    /// <see cref="PredicateArgumentSchema"/> only declares the argument's <see cref="LiteralKind"/>,
    /// not pattern well-formedness, so an invalid pattern is not caught at compile time — it surfaces
    /// as an evaluation-time fault: the underlying <see cref="Regex"/> constructor throws, and the
    /// evaluator absorbs that as <see cref="TruthValue.Unknown"/> per ADR-0001's Kleene failure model,
    /// the same as any other predicate-evaluation failure.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">
    /// Reads the string value to test from the context. A <see langword="null"/> result is treated as
    /// not-matching (false), never a fault, unless <paramref name="nullBehavior"/> is
    /// <see cref="NullBehavior.Unknown"/>.
    /// </param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the regular-expression pattern.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
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
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string matches the given regular-expression pattern "
            + "(System.Text.RegularExpressions). The compiled pattern is cached per distinct pattern "
            + "string, not recompiled per evaluation. The pattern is not validated at registration or "
            + "compile time; an invalid pattern surfaces as an evaluation-time fault (Unknown), per "
            + "ADR-0001's Kleene failure model. A null selected value is treated as not-matching "
            + "(false), never a fault, unless the host registers it with NullBehavior.Unknown.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The regular-expression pattern to match against.",
            negate: false
        );
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="Matches{TContext}"/>: true when the selected string does not match
    /// the pattern. It is the Strong Kleene complement of <c>Matches</c>, so a <see langword="null"/> selection
    /// answers <see cref="TruthValue.Unknown"/> by default and <see cref="TruthValue.True"/> under
    /// <see cref="NullBehavior.False"/>. An invalid pattern throws at evaluation time exactly as <c>Matches</c> does, which
    /// the evaluator records as a <c>Fault</c> and <see cref="TruthValue.Unknown"/> (ADR-0001).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the regular-expression pattern.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
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
            + "(System.Text.RegularExpressions). The K3 complement of Matches: a null selected value answers "
            + "Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes Matches False "
            + "and this twin True. "
            + "An invalid pattern surfaces as an evaluation-time fault (Unknown), per ADR-0001's Kleene failure model.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
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
            [new PredicateArgumentSchema(argumentName, argumentDescription, LiteralKind.String)]
        );

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
                return PredicateResult.FromBoolAsync(CompiledPattern(pattern).IsMatch(selected) != negate);
            }
        );
    }

    /// <summary>Gets the cached, compiled <see cref="Regex"/> for a pattern, compiling and caching it on first use.</summary>
    /// <param name="pattern">The regular-expression pattern, exactly as supplied in rule text.</param>
    /// <returns>The compiled, cached <see cref="Regex"/> for <paramref name="pattern"/>.</returns>
    private static Regex CompiledPattern([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
    {
        return PatternCache.GetOrAdd(pattern, static p => new Regex(p, RegexOptions.None, TimeSpan.FromSeconds(1)));
    }
}
