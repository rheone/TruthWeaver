namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made, generic collection predicate factories, each parameterized by a selector supplied at
/// registration.
/// </summary>
public static class CollectionPredicates
{
    /// <summary>
    /// Creates an order-insensitive, duplicate-insensitive set-equality predicate: true when the
    /// selected collection and the literal array argument contain the same distinct elements. This is
    /// a deliberate divergence from CONTEXT.md's "array-valued arguments are order-sensitive"
    /// term-identity rule — that rule governs *term identity* (whether two terms are the same
    /// variable for memoization/canonical-equality purposes), not this predicate's own *evaluation*
    /// semantics, so it is not a contradiction of that rule. Comparison is case-sensitive (ordinal),
    /// consistent with CONTEXT.md's general "argument values are case-sensitive" stance; there is no
    /// case-insensitive variant.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">
    /// Reads the collection to compare from the context. A <see langword="null"/> result answers per
    /// <paramref name="nullBehavior"/>, never a fault.
    /// </param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison set.</param>
    /// <param name="nullBehavior">
    /// How a <see langword="null"/> selected collection reads: <see cref="NullBehavior.Unknown"/> (the default), which
    /// makes the answer <see cref="TruthValue.Unknown"/>, or <see cref="NullBehavior.False"/>, which reads it as an empty
    /// collection. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) SetEquals<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Set Equals",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "True when the selected collection and the argument array contain the same elements, "
            + "ignoring order and duplicates (set semantics, not sequence semantics — a deliberate "
            + "divergence from CONTEXT.md's array-argument order-sensitive term-identity rule, which "
            + "governs term identity, not this predicate's evaluation semantics). Comparison is "
            + "case-sensitive (ordinal); no case-insensitive variant is provided. A null selected "
            + "collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, "
            + "which reads it as an empty collection.";
        return BuildSetEquals(name, label, description, selector, nullBehavior, argumentName, negate: false);
    }

    /// <summary>
    /// Creates the <c>NotSetEquals</c> twin of <see cref="SetEquals{TContext}"/>: true when the selected collection and the
    /// literal array argument do not contain the same distinct elements. It is the Strong Kleene complement of
    /// <c>SetEquals</c>. By default a <see langword="null"/> collection answers <see cref="TruthValue.Unknown"/>; under
    /// <see cref="NullBehavior.False"/> it is an empty collection, as for <c>SetEquals</c>, and the answer is the
    /// complement.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison set.</param>
    /// <param name="nullBehavior">
    /// How <c>SetEquals</c> reads a <see langword="null"/> selected collection: <see cref="NullBehavior.Unknown"/> (the
    /// default, the same as <c>SetEquals</c>) or <see cref="NullBehavior.False"/>, which reads it as an empty collection.
    /// This twin answers the complement. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotSetEquals<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Set Equals",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        const string description =
            "The Strong Kleene complement of SetEquals: True when the selected collection and the argument array do not "
            + "contain the same elements, ignoring order and duplicates. Comparison is case-sensitive (ordinal). A null "
            + "selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, "
            + "which reads it as an empty collection.";
        return BuildSetEquals(name, label, description, selector, nullBehavior, argumentName, negate: true);
    }

    /// <summary>
    /// Creates a predicate that is true when the selected collection has no elements. A
    /// <see langword="null"/> collection counts as empty, so the answer is always definite and there is no <see cref="NullBehavior"/> option.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result is an empty collection.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsEmpty<TContext>(string name, Func<TContext, IReadOnlyCollection<string>?> selector, string label = "Is Empty")
    {
        return BuildEmptiness(
            name,
            label,
            "True when the selected collection has no elements. A null collection counts as empty, so the answer is always definite.",
            selector,
            expectEmpty: true
        );
    }

    /// <summary>
    /// Creates the <c>IsNotEmpty</c> twin of <see cref="IsEmpty{TContext}"/>: true when the selected collection
    /// has at least one element. A <see langword="null"/> collection counts as empty, so it is
    /// <see cref="TruthValue.False"/>; the answer is always definite and there is no <see cref="NullBehavior"/> option.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result is an empty collection.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotEmpty<TContext>(string name, Func<TContext, IReadOnlyCollection<string>?> selector, string label = "Is Not Empty")
    {
        return BuildEmptiness(
            name,
            label,
            "The Strong Kleene complement of IsEmpty: True when the selected collection has at least one element. A null collection counts as empty, so it is False; the answer is always definite.",
            selector,
            expectEmpty: false
        );
    }

    /// <summary>Creates a predicate that is true when the selected collection contains the argument value (ordinal, case-sensitive).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the value.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Contains<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Contains",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected collection contains the argument value (ordinal, case-sensitive). A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.String,
            "The string the collection must contain.",
            static (selected, args, argument) => selected.Contains(args.GetString(argument), StringComparer.Ordinal)
        );
    }

    /// <summary>Creates the <c>NotContains</c> twin of <see cref="Contains{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the value.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotContains<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Contains",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of Contains: True when Contains is False, False when Contains is True, and Unknown when Contains is Unknown. Contains is true when the selected collection contains the argument value (ordinal, case-sensitive). A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.String,
            "The string the collection must contain.",
            static (selected, args, argument) => selected.Contains(args.GetString(argument), StringComparer.Ordinal)
        );
    }

    /// <summary>Creates a predicate that is true when at least one element of the selected collection is in the argument array (ordinal, case-sensitive).</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) ContainsAny<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Contains Any",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when at least one element of the selected collection is in the argument array (ordinal, case-sensitive). A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.StringArray,
            "The candidate strings; one matching element is enough.",
            static (selected, args, argument) =>
                selected.Any(new HashSet<string>(args.GetStringArray(argument), StringComparer.Ordinal).Contains)
        );
    }

    /// <summary>Creates the <c>NotContainsAny</c> twin of <see cref="ContainsAny{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotContainsAny<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Contains Any",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of ContainsAny: True when ContainsAny is False, False when ContainsAny is True, and Unknown when ContainsAny is Unknown. ContainsAny is true when at least one element of the selected collection is in the argument array (ordinal, case-sensitive). A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.StringArray,
            "The candidate strings; one matching element is enough.",
            static (selected, args, argument) =>
                selected.Any(new HashSet<string>(args.GetStringArray(argument), StringComparer.Ordinal).Contains)
        );
    }

    /// <summary>Creates a predicate that is true when every string in the argument array is an element of the selected collection (ordinal, case-sensitive); an empty array is vacuously true.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) ContainsAll<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Contains All",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when every string in the argument array is an element of the selected collection (ordinal, case-sensitive); an empty array is vacuously true. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.StringArray,
            "The strings the collection must all contain.",
            static (selected, args, argument) =>
                args.GetStringArray(argument).All(new HashSet<string>(selected, StringComparer.Ordinal).Contains)
        );
    }

    /// <summary>Creates the <c>NotContainsAll</c> twin of <see cref="ContainsAll{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotContainsAll<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Contains All",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of ContainsAll: True when ContainsAll is False, False when ContainsAll is True, and Unknown when ContainsAll is Unknown. ContainsAll is true when every string in the argument array is an element of the selected collection (ordinal, case-sensitive); an empty array is vacuously true. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.StringArray,
            "The strings the collection must all contain.",
            static (selected, args, argument) =>
                args.GetStringArray(argument).All(new HashSet<string>(selected, StringComparer.Ordinal).Contains)
        );
    }

    /// <summary>Creates a predicate that is true when every element of the selected collection is in the argument array (ordinal, case-sensitive); an empty collection is vacuously true.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsSubsetOf<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Is Subset Of",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when every element of the selected collection is in the argument array (ordinal, case-sensitive); an empty collection is vacuously true. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.StringArray,
            "The strings the collection elements must come from.",
            static (selected, args, argument) =>
                selected.All(new HashSet<string>(args.GetStringArray(argument), StringComparer.Ordinal).Contains)
        );
    }

    /// <summary>Creates the <c>IsNotSubsetOf</c> twin of <see cref="IsSubsetOf{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotSubsetOf<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Is Not Subset Of",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of IsSubsetOf: True when IsSubsetOf is False, False when IsSubsetOf is True, and Unknown when IsSubsetOf is Unknown. IsSubsetOf is true when every element of the selected collection is in the argument array (ordinal, case-sensitive); an empty collection is vacuously true. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.StringArray,
            "The strings the collection elements must come from.",
            static (selected, args, argument) =>
                selected.All(new HashSet<string>(args.GetStringArray(argument), StringComparer.Ordinal).Contains)
        );
    }

    /// <summary>Creates a predicate that is true when the selected string is one of the strings in the argument array (ordinal, case-sensitive). The selector returns one scalar value: a collection selector does not compile, so use ContainsAny, ContainsAll or IsSubsetOf for a collection.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected string answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) In<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected string is one of the strings in the argument array (ordinal, case-sensitive). The selector returns one scalar value: a collection selector does not compile, so use ContainsAny, ContainsAll or IsSubsetOf for a collection. A null selected string is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.StringArray,
            "The candidate strings the selected value may equal.",
            static (selected, args, argument) => args.GetStringArray(argument).Contains(selected, StringComparer.Ordinal)
        );
    }

    /// <summary>Creates the <c>NotIn</c> twin of <see cref="In{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the values.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected string answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotIn<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Not In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of In: True when In is False, False when In is True, and Unknown when In is Unknown. In is true when the selected string is one of the strings in the argument array (ordinal, case-sensitive). The selector returns one scalar value: a collection selector does not compile, so use ContainsAny, ContainsAll or IsSubsetOf for a collection. A null selected string is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.StringArray,
            "The candidate strings the selected value may equal.",
            static (selected, args, argument) => args.GetStringArray(argument).Contains(selected, StringComparer.Ordinal)
        );
    }

    /// <summary>Creates a predicate that is true when the selected collection has exactly the argument number of elements.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) CountEqual<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Count Equal",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected collection has exactly the argument number of elements. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count == args.GetInt64(argument)
        );
    }

    /// <summary>Creates the <c>NotCountEqual</c> twin of <see cref="CountEqual{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotCountEqual<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Count Equal",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of CountEqual: True when CountEqual is False, False when CountEqual is True, and Unknown when CountEqual is Unknown. CountEqual is true when the selected collection has exactly the argument number of elements. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count == args.GetInt64(argument)
        );
    }

    /// <summary>Creates a predicate that is true when the selected collection has fewer elements than the argument count.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) CountLessThan<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Count Less Than",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected collection has fewer elements than the argument count. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count < args.GetInt64(argument)
        );
    }

    /// <summary>Creates the <c>NotCountLessThan</c> twin of <see cref="CountLessThan{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotCountLessThan<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Count Less Than",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of CountLessThan: True when CountLessThan is False, False when CountLessThan is True, and Unknown when CountLessThan is Unknown. CountLessThan is true when the selected collection has fewer elements than the argument count. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count < args.GetInt64(argument)
        );
    }

    /// <summary>Creates a predicate that is true when the selected collection has more elements than the argument count.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) CountGreaterThan<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Count Greater Than",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected collection has more elements than the argument count. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count > args.GetInt64(argument)
        );
    }

    /// <summary>Creates the <c>NotCountGreaterThan</c> twin of <see cref="CountGreaterThan{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotCountGreaterThan<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Count Greater Than",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of CountGreaterThan: True when CountGreaterThan is False, False when CountGreaterThan is True, and Unknown when CountGreaterThan is Unknown. CountGreaterThan is true when the selected collection has more elements than the argument count. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count > args.GetInt64(argument)
        );
    }

    /// <summary>Creates a predicate that is true when the selected collection has at most the argument count of elements.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) CountLessThanOrEqual<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Count Less Than Or Equal",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected collection has at most the argument count of elements. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count <= args.GetInt64(argument)
        );
    }

    /// <summary>Creates the <c>NotCountLessThanOrEqual</c> twin of <see cref="CountLessThanOrEqual{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotCountLessThanOrEqual<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Count Less Than Or Equal",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of CountLessThanOrEqual: True when CountLessThanOrEqual is False, False when CountLessThanOrEqual is True, and Unknown when CountLessThanOrEqual is Unknown. CountLessThanOrEqual is true when the selected collection has at most the argument count of elements. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count <= args.GetInt64(argument)
        );
    }

    /// <summary>Creates a predicate that is true when the selected collection has at least the argument count of elements.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">What a <see langword="null"/> selected collection answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) CountGreaterThanOrEqual<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Count Greater Than Or Equal",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "True when the selected collection has at least the argument count of elements. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            false,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count >= args.GetInt64(argument)
        );
    }

    /// <summary>Creates the <c>NotCountGreaterThanOrEqual</c> twin of <see cref="CountGreaterThanOrEqual{TContext}"/>: the Strong Kleene complement, so a definite answer is inverted and <see cref="TruthValue.Unknown"/> stays <see cref="TruthValue.Unknown"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the collection from the context. A <see langword="null"/> result follows <paramref name="nullBehavior"/>.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the count.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected collection answers for the positive predicate: <see cref="NullBehavior.Unknown"/>
    /// (the default) or <see cref="NullBehavior.False"/>. This twin answers the complement: <see cref="TruthValue.Unknown"/> or
    /// <see cref="TruthValue.True"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NotCountGreaterThanOrEqual<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Not Count Greater Than Or Equal",
        string argumentName = "count",
        NullBehavior nullBehavior = NullBehavior.Unknown
    )
    {
        return Build(
            name,
            label,
            "The Strong Kleene complement of CountGreaterThanOrEqual: True when CountGreaterThanOrEqual is False, False when CountGreaterThanOrEqual is True, and Unknown when CountGreaterThanOrEqual is Unknown. CountGreaterThanOrEqual is true when the selected collection has at least the argument count of elements. A null selected collection is Unknown, never a fault, unless the host registers it with NullBehavior.False, which makes the positive predicate False (and its twin True).",
            selector,
            nullBehavior,
            true,
            argumentName,
            LiteralKind.Int64,
            "The element count to compare against.",
            static (selected, args, argument) => selected.Count >= args.GetInt64(argument)
        );
    }

    /// <summary>
    /// Builds the emptiness predicates. A null collection is an empty one, so neither answer is ever
    /// <see cref="TruthValue.Unknown"/>.
    /// </summary>
    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) BuildEmptiness<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        bool expectEmpty
    )
    {
        return (
            PredicateSchema.NoArguments(name, label, description),
            (context, _, _) =>
            {
                bool isEmpty = selector(context) is not { Count: > 0 };
                return PredicateResult.FromBoolAsync(isEmpty == expectEmpty);
            }
        );
    }

    /// <summary>
    /// Builds a one-argument comparison predicate over a reference-typed selected value. A null selection
    /// yields the host's <see cref="NullBehavior"/> answer; <paramref name="negate"/> then applies the Strong
    /// Kleene complement, so <see cref="TruthValue.Unknown"/> stays Unknown and a definite answer inverts.
    /// </summary>
    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Build<TContext, TSelected>(
        string name,
        string label,
        string description,
        Func<TContext, TSelected?> selector,
        NullBehavior nullBehavior,
        bool negate,
        string argumentName,
        LiteralKind kind,
        string argumentDescription,
        Func<TSelected, PredicateArguments, string, bool> test
    )
        where TSelected : class
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, argumentDescription, kind)]
        );

        return (
            schema,
            (context, args, _) =>
            {
                TSelected? selected = selector(context);
                if (selected is null)
                {
                    return PredicateResult.ForNullAsync(nullBehavior, negate);
                }

                bool answer = test(selected, args, argumentName);
                return PredicateResult.FromBoolAsync(answer != negate);
            }
        );
    }

    /// <summary>
    /// Builds the set-equality predicate. Under <see cref="NullBehavior.False"/> a null collection is an empty one;
    /// under <see cref="NullBehavior.Unknown"/> it answers Unknown. <paramref name="negate"/> applies the Strong Kleene
    /// complement to every answer.
    /// </summary>
    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) BuildSetEquals<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        NullBehavior nullBehavior,
        string argumentName,
        bool negate
    )
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, "The set of string values to compare against.", LiteralKind.StringArray)]
        );

        return (
            schema,
            (context, args, _) =>
            {
                IReadOnlyCollection<string>? selected = selector(context);
                if (selected is null && nullBehavior == NullBehavior.Unknown)
                {
                    return PredicateResult.ForNullAsync(nullBehavior, negate);
                }

                HashSet<string> selectedSet = selected is null
                    ? new(StringComparer.Ordinal)
                    : new(selected, StringComparer.Ordinal);
                HashSet<string> targetSet = new(args.GetStringArray(argumentName), StringComparer.Ordinal);
                return PredicateResult.FromBoolAsync(selectedSet.SetEquals(targetSet) != negate);
            }
        );
    }
}
