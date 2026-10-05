namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made predicate factories over nullable <c>Int64</c> and <c>Decimal</c> selections, each parameterized by a
/// selector supplied at registration. Every member has an overload for <c>long?</c> (arguments of kind
/// <c>Int64</c>) and for <c>decimal?</c> (arguments of kind <c>Decimal</c>). The members are <c>Equal</c>,
/// <c>NotEqual</c>, <c>LessThan</c>, <c>GreaterThan</c>, <c>LessThanOrEqual</c>, <c>GreaterThanOrEqual</c>,
/// <c>Between</c>, <c>Outside</c>, <c>In</c>, <c>NotIn</c>, <c>IsNull</c>, <c>IsNotNull</c>, <c>IsDefault</c> and
/// <c>IsNotDefault</c>.
/// <para>
/// No value is promoted between kinds inside one predicate. The selector's kind fixes the argument kind: a
/// <c>long?</c> selector takes <c>Int64</c> literals (<c>5</c>, never <c>1.5</c>) and a <c>decimal?</c> selector takes
/// <c>Decimal</c> literals (a whole number such as <c>5</c> is accepted). To compare an integer value with a
/// decimal literal, widen it in the selector (<c>c =&gt; (decimal?)c.Count</c>). The widening is exact, so no
/// precision is lost. Decimal values compare by value, so <c>1.0</c> equals <c>1.00</c>. A selector over an
/// <c>int</c> property binds to the <c>long?</c> overload.
/// </para>
/// <para>
/// A null selection is a missing value. The comparison, range, membership and default members answer
/// <see cref="TruthValue.Unknown"/> for it by default, and a host can pass <see cref="NullBehavior.False"/>. The null
/// tests (<c>IsNull</c>, <c>IsNotNull</c>) are definite. Every positive member has a registered <c>NotX</c> twin that is
/// the Strong Kleene complement for every selection, a null one included, so under <see cref="NullBehavior.False"/> a
/// twin answers <see cref="TruthValue.True"/> for a null selection. The pairs are <c>Equal</c>/<c>NotEqual</c>,
/// <c>LessThan</c>/<c>GreaterThanOrEqual</c>, <c>GreaterThan</c>/<c>LessThanOrEqual</c>, <c>Between</c>/<c>Outside</c>, <c>In</c>/<c>NotIn</c>,
/// <c>IsNull</c>/<c>IsNotNull</c> and <c>IsDefault</c>/<c>IsNotDefault</c>.
/// </para>
/// <para>
/// <c>Between</c> and <c>Outside</c> are inclusive on both bounds. Reversed bounds (<c>lower</c> greater than
/// <c>upper</c>) are an authoring error. When both bounds are literals, the compiler reports a <c>TRE0026</c> error and
/// the rule does not compile. A reversed bound that is not a literal throws <see cref="ArgumentException"/> at
/// evaluation time, which the evaluator records as a <c>Fault</c> and <see cref="TruthValue.Unknown"/>. The bounds are
/// never swapped silently.
/// </para>
/// </summary>
public static class NumericPredicates
{
    /// <summary>Creates an equality predicate over a nullable <see cref="long"/> selection.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Equal<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value equals the argument. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The integer value the selected value must equal.",
            static r => r == 0
        );
    }

    /// <summary>Creates an equality predicate over a nullable <see cref="decimal"/> selection.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Equal<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value equals the argument. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The decimal value the selected value must equal.",
            static r => r == 0
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>Equal</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, long?> selector,
        string label = "Not Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value differs from the argument. The K3 complement of Equal. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The integer value the selected value must not equal.",
            static r => r == 0,
            negate: true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>Equal</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, decimal?> selector,
        string label = "Not Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value differs from the argument. The K3 complement of Equal. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The decimal value the selected value must not equal.",
            static r => r == 0,
            negate: true
        );
    }

    /// <summary>Creates a <c>LessThan</c> predicate over a nullable <see cref="long"/> selection. Its <c>NotX</c> twin is <c>GreaterThanOrEqual</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) LessThan<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Less Than",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is less than the argument. The K3 complement of GreaterThanOrEqual. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The integer value to compare the selected value against.",
            static r => r < 0
        );
    }

    /// <summary>Creates a <c>LessThan</c> predicate over a nullable <see cref="decimal"/> selection. Its <c>NotX</c> twin is <c>GreaterThanOrEqual</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) LessThan<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Less Than",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is less than the argument. The K3 complement of GreaterThanOrEqual. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The decimal value to compare the selected value against.",
            static r => r < 0
        );
    }

    /// <summary>Creates a <c>GreaterThan</c> predicate over a nullable <see cref="long"/> selection. Its <c>NotX</c> twin is <c>LessThanOrEqual</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) GreaterThan<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Greater Than",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is greater than the argument. The K3 complement of LessThanOrEqual. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The integer value to compare the selected value against.",
            static r => r > 0
        );
    }

    /// <summary>Creates a <c>GreaterThan</c> predicate over a nullable <see cref="decimal"/> selection. Its <c>NotX</c> twin is <c>LessThanOrEqual</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) GreaterThan<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Greater Than",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is greater than the argument. The K3 complement of LessThanOrEqual. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The decimal value to compare the selected value against.",
            static r => r > 0
        );
    }

    /// <summary>Creates a <c>LessThanOrEqual</c> predicate over a nullable <see cref="long"/> selection. Its <c>NotX</c> twin is <c>GreaterThan</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) LessThanOrEqual<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Less Than Or Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is less than or equal to the argument. The K3 complement of GreaterThan. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The integer value to compare the selected value against.",
            static r => r > 0,
            negate: true
        );
    }

    /// <summary>Creates a <c>LessThanOrEqual</c> predicate over a nullable <see cref="decimal"/> selection. Its <c>NotX</c> twin is <c>GreaterThan</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) LessThanOrEqual<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Less Than Or Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is less than or equal to the argument. The K3 complement of GreaterThan. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The decimal value to compare the selected value against.",
            static r => r > 0,
            negate: true
        );
    }

    /// <summary>Creates a <c>GreaterThanOrEqual</c> predicate over a nullable <see cref="long"/> selection. Its <c>NotX</c> twin is <c>LessThan</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) GreaterThanOrEqual<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Greater Than Or Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is greater than or equal to the argument. The K3 complement of LessThan. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The integer value to compare the selected value against.",
            static r => r < 0,
            negate: true
        );
    }

    /// <summary>Creates a <c>GreaterThanOrEqual</c> predicate over a nullable <see cref="decimal"/> selection. Its <c>NotX</c> twin is <c>LessThan</c> (the K3 complement).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) GreaterThanOrEqual<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Greater Than Or Equal",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is greater than or equal to the argument. The K3 complement of LessThan. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            "The decimal value to compare the selected value against.",
            static r => r < 0,
            negate: true
        );
    }

    /// <summary>Creates a <c>Between</c> predicate: <c>lower &lt;= value &lt;= upper</c>, inclusive on both ends. Reversed literal bounds are a <c>TRE0026</c> compile error; a reversed bound that is not a literal throws <see cref="ArgumentException"/> at evaluation time, which the evaluator records as a <c>Fault</c>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="lowerName">The rule-text argument name for the inclusive lower bound.</param>
    /// <param name="upperName">The rule-text argument name for the inclusive upper bound.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Between<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Between",
        string lowerName = "lower",
        string upperName = "upper",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value lies between the bounds, inclusive on both ends. Reversed bounds (lower greater than upper) are an authoring error, never swapped: a compile error for literal bounds, otherwise an argument error at evaluation. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Range(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            lowerName,
            upperName,
            false
        );
    }

    /// <summary>Creates a <c>Between</c> predicate: <c>lower &lt;= value &lt;= upper</c>, inclusive on both ends. Reversed literal bounds are a <c>TRE0026</c> compile error; a reversed bound that is not a literal throws <see cref="ArgumentException"/> at evaluation time, which the evaluator records as a <c>Fault</c>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="lowerName">The rule-text argument name for the inclusive lower bound.</param>
    /// <param name="upperName">The rule-text argument name for the inclusive upper bound.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Between<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Between",
        string lowerName = "lower",
        string upperName = "upper",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value lies between the bounds, inclusive on both ends. Reversed bounds (lower greater than upper) are an authoring error, never swapped: a compile error for literal bounds, otherwise an argument error at evaluation. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Range(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            lowerName,
            upperName,
            false
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>Between</c>: <c>value &lt; lower</c> or <c>value &gt; upper</c>. Reversed literal bounds are a <c>TRE0026</c> compile error; a reversed bound that is not a literal throws <see cref="ArgumentException"/> at evaluation time.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="lowerName">The rule-text argument name for the inclusive lower bound.</param>
    /// <param name="upperName">The rule-text argument name for the inclusive upper bound.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Outside<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Outside",
        string lowerName = "lower",
        string upperName = "upper",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value lies outside the bounds. The exact K3 complement of Between, so the bounds are inclusive. Reversed bounds are an authoring error, never swapped: a compile error for literal bounds, otherwise an argument error at evaluation. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Range(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            lowerName,
            upperName,
            true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>Between</c>: <c>value &lt; lower</c> or <c>value &gt; upper</c>. Reversed literal bounds are a <c>TRE0026</c> compile error; a reversed bound that is not a literal throws <see cref="ArgumentException"/> at evaluation time.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="lowerName">The rule-text argument name for the inclusive lower bound.</param>
    /// <param name="upperName">The rule-text argument name for the inclusive upper bound.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Outside<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Outside",
        string lowerName = "lower",
        string upperName = "upper",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value lies outside the bounds. The exact K3 complement of Between, so the bounds are inclusive. Reversed bounds are an authoring error, never swapped: a compile error for literal bounds, otherwise an argument error at evaluation. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Range(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            lowerName,
            upperName,
            true
        );
    }

    /// <summary>Creates a scalar-membership predicate: true when the selected value is in the literal candidate array.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) In<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is one of the candidate values. Scalar membership only. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            false
        );
    }

    /// <summary>Creates a scalar-membership predicate: true when the selected value is in the literal candidate array.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) In<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is one of the candidate values. Scalar membership only. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            false
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>In</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotIn<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Not In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is not one of the candidate values. The K3 complement of In. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Int64,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>In</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotIn<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Not In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is not one of the candidate values. The K3 complement of In. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Decimal,
            name,
            label,
            description,
            selector,
            nullBehavior,
            argumentName,
            true
        );
    }

    /// <summary>Creates a definite null test. A null selection is <see cref="TruthValue.True"/>, never <see cref="TruthValue.Unknown"/>; it has no <see cref="NullBehavior"/> option.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNull<TContext>(string name, Func<TContext, long?> selector, string label = "Is Null")
    {
        const string description =
            "True when the selected value is null. Always definite: null is true, a value is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, false);
    }

    /// <summary>Creates a definite null test. A null selection is <see cref="TruthValue.True"/>, never <see cref="TruthValue.Unknown"/>; it has no <see cref="NullBehavior"/> option.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNull<TContext>(string name, Func<TContext, decimal?> selector, string label = "Is Null")
    {
        const string description =
            "True when the selected value is null. Always definite: null is true, a value is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, false);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsNull</c>. Definite: a null selection is <see cref="TruthValue.False"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNull<TContext>(string name, Func<TContext, long?> selector, string label = "Is Not Null")
    {
        const string description = "True when the selected value is not null. Always definite: null is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, true);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsNull</c>. Definite: a null selection is <see cref="TruthValue.False"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNull<TContext>(string name, Func<TContext, decimal?> selector, string label = "Is Not Null")
    {
        const string description = "True when the selected value is not null. Always definite: null is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, true);
    }

    /// <summary>Creates a predicate that is true when the selected value is <c>default(long)</c>. A null selection is a missing value, not a default, so it answers per <paramref name="nullBehavior"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsDefault<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Is Default",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is default(long) (0). A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.DefaultTest(name, label, description, selector, nullBehavior, false);
    }

    /// <summary>Creates a predicate that is true when the selected value is <c>default(decimal)</c>. A null selection is a missing value, not a default, so it answers per <paramref name="nullBehavior"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsDefault<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Is Default",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is default(decimal) (0). A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.DefaultTest(name, label, description, selector, nullBehavior, false);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsDefault</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the integer value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotDefault<TContext>(
        string name,
        Func<TContext, long?> selector,
        string label = "Is Not Default",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected integer value is not default(long) (0). The K3 complement of IsDefault. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.DefaultTest(name, label, description, selector, nullBehavior, true);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsDefault</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the decimal value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotDefault<TContext>(
        string name,
        Func<TContext, decimal?> selector,
        string label = "Is Not Default",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected decimal value is not default(decimal) (0). The K3 complement of IsDefault. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.DefaultTest(name, label, description, selector, nullBehavior, true);
    }
}
