namespace TruthWeaver.Predicates;

using System.Globalization;
using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made, generic string-comparison predicate factories, each parameterized by a
/// <c>Func&lt;TContext, string?&gt;</c> value selector supplied at registration and a single
/// <c>string</c> comparison-target argument supplied in rule text. Every predicate here uses
/// ordinal comparison only — never culture-sensitive comparison — so rule behavior never depends on
/// the host process's current culture.
/// </summary>
public static class StringPredicates
{
    /// <summary>Creates a case-sensitive (ordinal) string-equality predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Equals<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Equals",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string equals the argument exactly (ordinal, case-sensitive "
            + "comparison). A null selected value is treated as not-equal (false), never a fault, unless the host registers it with NullBehavior.Unknown.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The string the selected value must equal.",
            static (selected, target) => string.Equals(selected, target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates a case-insensitive (ordinal) string-equality predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) EqualsIgnoreCase<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Equals (Ignore Case)",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string equals the argument, ignoring case. Comparison is ordinal "
            + "case-insensitive, never culture-sensitive, so behavior never depends on the host's "
            + "current culture. A null selected value is treated as not-equal (false), never a fault, unless the host registers it with NullBehavior.Unknown.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The string the selected value must equal, ignoring case.",
            static (selected, target) => string.Equals(selected, target, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>Creates an ordinal <c>StartsWith</c> predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the prefix.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) StartsWith<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Starts With",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string starts with the argument (ordinal comparison, never "
            + "culture-sensitive). A null selected value is treated as not-matching (false), never a fault, unless the host registers it with NullBehavior.Unknown.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The prefix the selected value must start with.",
            static (selected, target) => selected.StartsWith(target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates an ordinal <c>EndsWith</c> predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the suffix.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) EndsWith<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Ends With",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string ends with the argument (ordinal comparison, never "
            + "culture-sensitive). A null selected value is treated as not-matching (false), never a fault, unless the host registers it with NullBehavior.Unknown.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The suffix the selected value must end with.",
            static (selected, target) => selected.EndsWith(target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates an ordinal <c>Contains</c> predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the substring.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Contains<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Contains",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string contains the argument as a substring (ordinal comparison, "
            + "never culture-sensitive). A null selected value is treated as not-matching (false), "
            + "never a fault, unless the host registers it with NullBehavior.Unknown.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The substring the selected value must contain.",
            static (selected, target) => selected.Contains(target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates a predicate that is true when the selected string is <see langword="null"/> or <see cref="string.Empty"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNullOrEmpty<TContext>(string name, Func<TContext, string?> selector, string label = "Is Null Or Empty")
    {
        PredicateSchema schema = PredicateSchema.NoArguments(
            name,
            label,
            "True when the selected string is null or the empty string (\"\")."
        );

        return (schema, (context, _, _) => PredicateResult.FromBoolAsync(string.IsNullOrEmpty(selector(context))));
    }

    /// <summary>
    /// Creates an ordinal string-equality predicate whose case-sensitivity and whitespace-trimming
    /// behavior are rule-text arguments rather than fixed at registration. Like every other method in this
    /// class the comparison is ordinal (<see cref="StringComparison.Ordinal"/>, or
    /// <see cref="StringComparison.OrdinalIgnoreCase"/> when <c>ignoreCase</c> is set), never
    /// culture-sensitive. The <c>culture</c> argument is retained only for compatibility with existing
    /// rules and must be empty. The per-rule flags are a deliberate divergence from the fixed-behavior
    /// predicates here (see this class's type-level remarks), which make a behavior variant a distinct,
    /// separately-named predicate; this method exists for a rule author who genuinely needs to choose
    /// case-sensitivity and trimming per rule, not per predicate name. The same divergence-for-a-reason pattern
    /// <see cref="CollectionPredicates.SetEquals{TContext}"/> already documents for a different rule.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.False"/> (the default) or
    /// <see cref="NullBehavior.Unknown"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentException">
    /// The rule-text <c>culture</c> argument is not empty. This is not caught at compile time (the
    /// schema only declares the argument's <see cref="LiteralKind"/>); it surfaces as an evaluation-time fault
    /// (<see cref="TruthValue.Unknown"/>) per ADR-0001's Kleene failure model, the same treatment
    /// <see cref="RegexPredicates.Matches{TContext}"/> gives an invalid regular-expression pattern.
    /// </exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) EqualsConfigurable<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Equals (Configurable)",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.False
    )
    {
        const string description =
            "True when the selected string equals the argument, under configurable comparison rules: "
            + "ordinal and case-insensitive by default (ignoreCase), with optional "
            + "leading/trailing-whitespace trimming. The culture argument is retained for compatibility and must be empty. A null selected value is treated as not-equal "
            + "(false), never a fault, unless the host registers it with NullBehavior.Unknown.";
        PredicateSchema schema = new(
            name,
            label,
            description,
            [
                new PredicateArgumentSchema(argumentName, "The string the selected value must equal.", LiteralKind.String),
                new PredicateArgumentSchema(
                    "ignoreCase",
                    "Whether the comparison ignores case. Defaults to true.",
                    LiteralKind.Boolean,
                    Required: false,
                    Default: LiteralValue.OfBoolean(true)
                ),
                new PredicateArgumentSchema(
                    "culture",
                    "Retained for compatibility with existing rules; must be empty. Comparison is always ordinal, never culture-sensitive.",
                    LiteralKind.String,
                    Required: false,
                    Default: LiteralValue.OfString(string.Empty)
                ),
                new PredicateArgumentSchema(
                    "trim",
                    "Whether both sides are trimmed of leading/trailing whitespace before comparing. Defaults to false.",
                    LiteralKind.Boolean,
                    Required: false,
                    Default: LiteralValue.OfBoolean(false)
                ),
            ]
        );

        return (
            schema,
            (context, args, _) =>
            {
                string? selected = selector(context);
                if (selected is null)
                {
                    return PredicateResult.ForNullAsync(nullBehavior);
                }

                string target = args.GetString(argumentName);
                bool ignoreCase = args.GetBool("ignoreCase");
                bool trim = args.GetBool("trim");
                string culture = args.GetString("culture");

                if (trim)
                {
                    selected = selected.Trim();
                    target = target.Trim();
                }

                // Culture-sensitive comparison is not offered (CONTEXT.md, Predicate catalog rules): the argument
                // remains only so existing rules that spell culture: "" keep compiling.
                if (!string.IsNullOrEmpty(culture))
                {
                    throw new ArgumentException("EqualsConfigurable compares ordinally; the culture argument must be empty.");
                }

                StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                return PredicateResult.FromBoolAsync(string.Equals(selected, target, comparison));
            }
        );
    }

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
        Func<string, string, bool> compare
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
                return selected is null
                    ? PredicateResult.ForNullAsync(nullBehavior)
                    : PredicateResult.FromBoolAsync(compare(selected, args.GetString(argumentName)));
            }
        );
    }
}
