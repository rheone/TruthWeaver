namespace TruthWeaver.Predicates;

/// <summary>
/// Sets the <see cref="NullBehavior"/> that built-in predicate factories use when the caller does not pass one. A host
/// opens a scope around its registration code, so it states the choice once and not on every factory call. The scope
/// applies to a factory call made on the same logical thread of execution while the scope is open, and it is read when
/// the factory runs, not when the rule is evaluated. A <see cref="NullBehavior"/> passed to a factory always wins.
/// </summary>
/// <remarks>
/// <para>
/// The predicate factories return their schema and delegate before <c>PredicateRegistryBuilder&lt;TContext&gt;</c> sees
/// them, and the core package does not reference this package, so the registry builder cannot hold this setting. A scope
/// reaches the factories without a parameter on each call. Without a scope, the default is
/// <see cref="NullBehavior.Unknown"/>.
/// </para>
/// <para>
/// Scopes nest. A scope for one <see cref="PredicateFamily"/> beats a scope for all families when both are open, whichever
/// opened first, and among scopes of the same kind the innermost wins. <c>IsNull</c> and <c>IsNotNull</c> take no
/// option, and faults are never affected.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using (NullBehaviorScope.Begin(NullBehavior.False))
/// using (NullBehaviorScope.Begin(PredicateFamily.Collection, NullBehavior.Unknown))
/// {
///     builder.Add(NumericPredicates.IsGreaterThan&lt;User&gt;("ageAbove", u =&gt; u.Age)); // answers False for a null age
///     builder.Add(CollectionPredicates.Contains&lt;User&gt;("hasRole", u =&gt; u.Roles)); // answers Unknown for a null list
/// }
/// </code>
/// </example>
public sealed class NullBehaviorScope : IDisposable
{
    private static readonly AsyncLocal<NullBehaviorScope?> Current = new();

    private readonly NullBehaviorScope? parent;
    private readonly PredicateFamily? family;
    private readonly NullBehavior behavior;
    private bool disposed;

    private NullBehaviorScope(NullBehaviorScope? parent, PredicateFamily? family, NullBehavior behavior)
    {
        this.parent = parent;
        this.family = family;
        this.behavior = behavior;
    }

    /// <summary>Opens a scope that sets the default for every predicate family.</summary>
    /// <param name="behavior">The default for a factory call that passes no <see cref="NullBehavior"/>. <see cref="NullBehavior.Default"/> is not a choice and is rejected.</param>
    /// <returns>The scope. Dispose it to restore the previous default.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> is not <see cref="NullBehavior.Unknown"/> or <see cref="NullBehavior.False"/>.</exception>
    public static NullBehaviorScope Begin(NullBehavior behavior)
    {
        return Open(null, behavior);
    }

    /// <summary>Opens a scope that sets the default for one predicate family only.</summary>
    /// <param name="family">The family the default applies to.</param>
    /// <param name="behavior">The default for a factory call of <paramref name="family"/> that passes no <see cref="NullBehavior"/>. <see cref="NullBehavior.Default"/> is not a choice and is rejected.</param>
    /// <returns>The scope. Dispose it to restore the previous default.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="family"/> is not a defined family, or <paramref name="behavior"/> is not <see cref="NullBehavior.Unknown"/> or <see cref="NullBehavior.False"/>.</exception>
    public static NullBehaviorScope Begin(PredicateFamily family, NullBehavior behavior)
    {
        if (!Enum.IsDefined(family))
        {
            throw new ArgumentOutOfRangeException(nameof(family), family, "The predicate family is not defined.");
        }

        return Open(family, behavior);
    }

    /// <summary>Restores the default that was in effect before this scope opened. Disposing twice does nothing.</summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        Current.Value = this.parent;
    }

    /// <summary>
    /// Turns the value a factory received into the behavior it uses: an explicit <see cref="NullBehavior.Unknown"/> or
    /// <see cref="NullBehavior.False"/> is kept, and <see cref="NullBehavior.Default"/> becomes the open scope's choice for
    /// <paramref name="family"/>, or <see cref="NullBehavior.Unknown"/> when no scope applies.
    /// </summary>
    internal static NullBehavior Resolve(NullBehavior requested, PredicateFamily family)
    {
        if (requested != NullBehavior.Default)
        {
            return requested;
        }

        NullBehavior? allFamilies = null;
        for (NullBehaviorScope? scope = Current.Value; scope is not null; scope = scope.parent)
        {
            if (scope.family == family)
            {
                // The innermost scope of this family wins outright.
                return scope.behavior;
            }

            if (scope.family is null)
            {
                allFamilies ??= scope.behavior;
            }
        }

        return allFamilies ?? NullBehavior.Unknown;
    }

    private static NullBehaviorScope Open(PredicateFamily? family, NullBehavior behavior)
    {
        if (behavior is not (NullBehavior.Unknown or NullBehavior.False))
        {
            throw new ArgumentOutOfRangeException(
                nameof(behavior),
                behavior,
                "A scope sets Unknown or False. NullBehavior.Default means 'use the scope', so it cannot be the scope's own choice."
            );
        }

        NullBehaviorScope scope = new(Current.Value, family, behavior);
        Current.Value = scope;
        return scope;
    }
}
