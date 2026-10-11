namespace TruthWeaver.Predicates;

/// <summary>
/// Ready-made predicate factories over nullable <c>Boolean</c>, <c>Guid</c> and <c>DateTimeOffset</c> selections.
/// Each member has an overload per kind, with arguments of the matching <c>LiteralKind</c>. The members are
/// <c>IsEqualTo</c>, <c>IsNotEqualTo</c>, <c>IsIn</c>, <c>IsNotIn</c>, <c>IsNull</c>, <c>IsNotNull</c>, <c>IsDefault</c> and
/// <c>IsNotDefault</c>.
/// <para>
/// Ordering and range members are not defined for <c>Boolean</c> and <c>Guid</c>, where an order has no meaning for a
/// rule. For <c>DateTimeOffset</c>, use the date-time comparison predicates for ordering and ranges. <c>DateTimeOffset</c>
/// values compare by instant, so the same instant in two offsets is equal.
/// </para>
/// <para>
/// A null selection is a missing value. <c>IsEqualTo</c>, <c>IsNotEqualTo</c>, <c>IsIn</c>, <c>IsNotIn</c>, <c>IsDefault</c> and
/// <c>IsNotDefault</c> answer <see cref="TruthValue.Unknown"/> for it by default, and a host can pass
/// <see cref="NullBehavior.False"/>. The null tests are definite. Every positive member has a registered <c>NotX</c>
/// twin that is the Strong Kleene complement for every selection, a null one included, so under
/// <see cref="NullBehavior.False"/> a twin answers <see cref="TruthValue.True"/> for a null selection.
/// </para>
/// </summary>
public static class ScalarPredicates
{
    /// <summary>Creates an equality predicate over a nullable <see cref="bool"/> selection.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsEqualTo<TContext>(
        string name,
        Func<TContext, bool?> selector,
        string label = "Is Equal To",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected boolean value equals the argument. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Boolean,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            "The boolean value the selected value must equal.",
            static r => r == 0
        );
    }

    /// <summary>Creates an equality predicate over a nullable <see cref="Guid"/> selection.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsEqualTo<TContext>(
        string name,
        Func<TContext, Guid?> selector,
        string label = "Is Equal To",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected GUID value equals the argument. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Guid,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            "The GUID value the selected value must equal.",
            static r => r == 0
        );
    }

    /// <summary>Creates an equality predicate over a nullable <see cref="DateTimeOffset"/> selection.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsEqualTo<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Equal To",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected date-time value equals the argument. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.DateTimeOffset,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            "The date-time value the selected value must equal.",
            static r => r == 0
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsEqualTo</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotEqualTo<TContext>(
        string name,
        Func<TContext, bool?> selector,
        string label = "Is Not Equal To",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected boolean value differs from the argument. The K3 complement of IsEqualTo. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Boolean,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            "The boolean value the selected value must not equal.",
            static r => r == 0,
            negate: true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsEqualTo</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotEqualTo<TContext>(
        string name,
        Func<TContext, Guid?> selector,
        string label = "Is Not Equal To",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected GUID value differs from the argument. The K3 complement of IsEqualTo. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.Guid,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            "The GUID value the selected value must not equal.",
            static r => r == 0,
            negate: true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsEqualTo</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotEqualTo<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Not Equal To",
        string argumentName = "value",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected date-time value differs from the argument. The K3 complement of IsEqualTo. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Compare(
            ScalarKinds.DateTimeOffset,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            "The date-time value the selected value must not equal.",
            static r => r == 0,
            negate: true
        );
    }

    /// <summary>Creates a scalar-membership predicate: true when the selected value is in the literal candidate array.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsIn<TContext>(
        string name,
        Func<TContext, bool?> selector,
        string label = "Is In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected boolean value is one of the candidate values. Scalar membership only. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Boolean,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            false
        );
    }

    /// <summary>Creates a scalar-membership predicate: true when the selected value is in the literal candidate array.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsIn<TContext>(
        string name,
        Func<TContext, Guid?> selector,
        string label = "Is In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected GUID value is one of the candidate values. Scalar membership only. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Guid,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            false
        );
    }

    /// <summary>Creates a scalar-membership predicate: true when the selected value is in the literal candidate array.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the candidate array.</param>
    /// <param name="nullBehavior">
    /// What a <see langword="null"/> selected value answers: <see cref="NullBehavior.Unknown"/> (the default) or <see cref="NullBehavior.False"/>. Neither is a fault.
    /// </param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsIn<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected date-time value is one of the candidate values. Scalar membership only. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.DateTimeOffset,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            false
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsIn</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotIn<TContext>(
        string name,
        Func<TContext, bool?> selector,
        string label = "Is Not In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected boolean value is not one of the candidate values. The K3 complement of IsIn. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Boolean,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsIn</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotIn<TContext>(
        string name,
        Func<TContext, Guid?> selector,
        string label = "Is Not In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected GUID value is not one of the candidate values. The K3 complement of IsIn. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.Guid,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsIn</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
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
    ) IsNotIn<TContext>(
        string name,
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Not In",
        string argumentName = "values",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected date-time value is not one of the candidate values. The K3 complement of IsIn. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.Membership(
            ScalarKinds.DateTimeOffset,
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            argumentName,
            true
        );
    }

    /// <summary>Creates a definite null test. A null selection is <see cref="TruthValue.True"/>, never <see cref="TruthValue.Unknown"/>; it has no <see cref="NullBehavior"/> option.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNull<TContext>(string name, Func<TContext, bool?> selector, string label = "Is Null")
    {
        const string description =
            "True when the selected value is null. Always definite: null is true, a value is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, false);
    }

    /// <summary>Creates a definite null test. A null selection is <see cref="TruthValue.True"/>, never <see cref="TruthValue.Unknown"/>; it has no <see cref="NullBehavior"/> option.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNull<TContext>(string name, Func<TContext, Guid?> selector, string label = "Is Null")
    {
        const string description =
            "True when the selected value is null. Always definite: null is true, a value is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, false);
    }

    /// <summary>Creates a definite null test. A null selection is <see cref="TruthValue.True"/>, never <see cref="TruthValue.Unknown"/>; it has no <see cref="NullBehavior"/> option.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNull<TContext>(string name, Func<TContext, DateTimeOffset?> selector, string label = "Is Null")
    {
        const string description =
            "True when the selected value is null. Always definite: null is true, a value is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, false);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsNull</c>. Definite: a null selection is <see cref="TruthValue.False"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNull<TContext>(string name, Func<TContext, bool?> selector, string label = "Is Not Null")
    {
        const string description = "True when the selected value is not null. Always definite: null is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, true);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsNull</c>. Definite: a null selection is <see cref="TruthValue.False"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNull<TContext>(string name, Func<TContext, Guid?> selector, string label = "Is Not Null")
    {
        const string description = "True when the selected value is not null. Always definite: null is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, true);
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsNull</c>. Definite: a null selection is <see cref="TruthValue.False"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNull<TContext>(string name, Func<TContext, DateTimeOffset?> selector, string label = "Is Not Null")
    {
        const string description = "True when the selected value is not null. Always definite: null is false, never Unknown.";
        return ScalarPredicateCore.NullTest(name, label, description, selector, true);
    }

    /// <summary>Creates a predicate that is true when the selected value is <c>default(bool)</c>. A null selection is a missing value, not a default, so it answers per <paramref name="nullBehavior"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, bool?> selector,
        string label = "Is Default",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected boolean value is default(bool) (false). A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.DefaultTest(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is <c>default(Guid)</c>. A null selection is a missing value, not a default, so it answers per <paramref name="nullBehavior"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, Guid?> selector,
        string label = "Is Default",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected GUID value is default(Guid) (Guid.Empty). A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.DefaultTest(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is <c>default(DateTimeOffset)</c>. A null selection is a missing value, not a default, so it answers per <paramref name="nullBehavior"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Default",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected date-time value is default(DateTimeOffset) (0001-01-01T00:00:00+00:00). A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False.";
        return ScalarPredicateCore.DefaultTest(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            false
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsDefault</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the boolean value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, bool?> selector,
        string label = "Is Not Default",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected boolean value is not default(bool) (false). The K3 complement of IsDefault. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.DefaultTest(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsDefault</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the GUID value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, Guid?> selector,
        string label = "Is Not Default",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected GUID value is not default(Guid) (Guid.Empty). The K3 complement of IsDefault. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.DefaultTest(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            true
        );
    }

    /// <summary>Creates the <c>NotX</c> twin of <c>IsDefault</c>: the Strong Kleene complement, so a null selection answers the complement of the positive answer.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the date-time value from the context. A <see langword="null"/> result is a missing value.</param>
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
        Func<TContext, DateTimeOffset?> selector,
        string label = "Is Not Default",
        NullBehavior nullBehavior = NullBehavior.Default
    )
    {
        const string description =
            "True when the selected date-time value is not default(DateTimeOffset) (0001-01-01T00:00:00+00:00). The K3 complement of IsDefault. A null selected value answers Unknown (never a fault) unless the host registers it with NullBehavior.False, which makes the positive predicate False and this twin True.";
        return ScalarPredicateCore.DefaultTest(
            name,
            label,
            description,
            selector,
            NullBehaviorScope.Resolve(nullBehavior, PredicateFamily.Scalar),
            true
        );
    }
}
