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
    /// Creates a predicate that is true when the selected string is the non-null empty string. A
    /// <see langword="null"/> selection is a missing value, not an empty one, so it answers per
    /// <paramref name="nullBehavior"/> (<see cref="NullBehavior.Unknown"/> by default).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default for this
    /// member) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsEmpty<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Is Empty",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string is the empty string (\"\"). A null selected value is a missing value, "
            + "not an empty one: it answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return CreateNoArgument(name, label, description, selector, nullBehavior, static selected => selected.Length == 0);
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="IsEmpty{TContext}"/>: true when the selected string is non-null and
    /// not empty. It is the Strong Kleene complement of <c>IsEmpty</c>, so a <see langword="null"/> selection
    /// answers <see cref="TruthValue.Unknown"/> by default and <see cref="TruthValue.True"/> under <see cref="NullBehavior.False"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
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
    ) IsNotEmpty<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Is Not Empty",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string is not the empty string. The K3 complement of IsEmpty: a null selected value "
            + "answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes IsEmpty "
            + "False and this twin True.";
        return CreateNoArgument(
            name,
            label,
            description,
            selector,
            nullBehavior,
            static selected => selected.Length == 0,
            negate: true
        );
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="IsNullOrEmpty{TContext}"/>: true when the selected string is
    /// non-null and not empty. A null test, so it is always definite (a <see langword="null"/> selection is <see cref="TruthValue.False"/>).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNullOrEmpty<TContext>(string name, Func<TContext, string?> selector, string label = "Is Not Null Or Empty")
    {
        PredicateSchema schema = PredicateSchema.NoArguments(
            name,
            label,
            "True when the selected string is neither null nor the empty string (\"\"). Always definite: null is false."
        );

        return (schema, (context, _, _) => PredicateResult.FromBoolAsync(!string.IsNullOrEmpty(selector(context))));
    }

    /// <summary>
    /// Creates a predicate that is true when the selected string is <see langword="null"/>, empty or only
    /// whitespace (<see cref="string.IsNullOrWhiteSpace(string)"/>). A null test, so it is always definite.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNullOrWhiteSpace<TContext>(string name, Func<TContext, string?> selector, string label = "Is Null Or White Space")
    {
        PredicateSchema schema = PredicateSchema.NoArguments(
            name,
            label,
            "True when the selected string is null, empty or only whitespace. Always definite: null is true."
        );

        return (schema, (context, _, _) => PredicateResult.FromBoolAsync(string.IsNullOrWhiteSpace(selector(context))));
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="IsNullOrWhiteSpace{TContext}"/>: true when the selected string has
    /// at least one non-whitespace character. A null test, so it is always definite (a <see langword="null"/> selection is <see cref="TruthValue.False"/>).
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNullOrWhiteSpace<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Is Not Null Or White Space"
    )
    {
        PredicateSchema schema = PredicateSchema.NoArguments(
            name,
            label,
            "True when the selected string has at least one non-whitespace character. Always definite: null is false."
        );

        return (schema, (context, _, _) => PredicateResult.FromBoolAsync(!string.IsNullOrWhiteSpace(selector(context))));
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="Equals{TContext}"/>: an ordinal, case-sensitive inequality test. It is
    /// the Strong Kleene complement of <c>Equals</c>, so a <see langword="null"/> selection answers
    /// <see cref="TruthValue.Unknown"/> by default and <see cref="TruthValue.True"/> under <see cref="NullBehavior.False"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotEqual<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Not Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string differs from the argument (ordinal, case-sensitive, never "
            + "culture-sensitive). The K3 complement of Equals: a null selected value answers Unknown "
            + "(never a fault) unless the host registers it with NullBehavior.False, which makes Equals False and this twin True.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The string the selected value must not equal.",
            static (selected, target) => string.Equals(selected, target, StringComparison.Ordinal),
            negate: true
        );
    }

    /// <summary>
    /// Creates the <c>NotX</c> twin of <see cref="Contains{TContext}"/>: an ordinal test that the selected string does
    /// not contain the substring. It is the Strong Kleene complement of <c>Contains</c>, so a
    /// <see langword="null"/> selection answers <see cref="TruthValue.Unknown"/> by default and
    /// <see cref="TruthValue.True"/> under <see cref="NullBehavior.False"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the substring.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotContains<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Not Contains",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected string does not contain the argument as a substring (ordinal, never "
            + "culture-sensitive). The K3 complement of Contains: a null selected value answers Unknown "
            + "(never a fault) unless the host registers it with NullBehavior.False, which makes Contains False and this twin True.";
        return Create(
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The substring the selected value must not contain.",
            static (selected, target) => selected.Contains(target, StringComparison.Ordinal),
            negate: true
        );
    }

    /// <summary>
    /// Creates an ordinal string-equality predicate whose case-sensitivity and whitespace-trimming
    /// behavior are rule-text arguments rather than fixed at registration. Like every other method in this
    /// class the comparison is ordinal (<see cref="StringComparison.Ordinal"/>, or
    /// <see cref="StringComparison.OrdinalIgnoreCase"/> when <c>ignoreCase</c> is set), never
    /// culture-sensitive, so the predicate has no <c>culture</c> argument. The per-rule flags are a deliberate divergence from the fixed-behavior
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
            + "leading/trailing-whitespace trimming. A null selected value is treated as not-equal "
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

                if (trim)
                {
                    selected = selected.Trim();
                    target = target.Trim();
                }

                StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                return PredicateResult.FromBoolAsync(string.Equals(selected, target, comparison));
            }
        );
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) CreateNoArgument<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, string?> selector,
        NullBehavior nullBehavior,
        Func<string, bool> test,
        bool negate = false
    )
    {
        return (
            PredicateSchema.NoArguments(name, label, description),
            (context, _, _) =>
            {
                string? selected = selector(context);
                return selected is null
                    ? PredicateResult.ForNullAsync(nullBehavior, negate)
                    : PredicateResult.FromBoolAsync(test(selected) != negate);
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
        Func<string, string, bool> compare,
        bool negate = false
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
                    ? PredicateResult.ForNullAsync(nullBehavior, negate)
                    : PredicateResult.FromBoolAsync(compare(selected, args.GetString(argumentName)) != negate);
            }
        );
    }
}
