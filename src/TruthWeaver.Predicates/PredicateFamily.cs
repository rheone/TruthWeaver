namespace TruthWeaver.Predicates;

/// <summary>
/// Names a group of built-in predicate factories. A host uses it to set a <see cref="NullBehavior"/> default for one
/// group through <see cref="NullBehaviorScope.Begin(PredicateFamily, NullBehavior)"/>. Each member matches the static
/// class that holds the factories of the group.
/// </summary>
public enum PredicateFamily
{
    /// <summary>The factories of <see cref="NumericPredicates"/>.</summary>
    Numeric = 0,

    /// <summary>The factories of <see cref="ScalarPredicates"/> (boolean, GUID and date-time equality, membership and default tests).</summary>
    Scalar = 1,

    /// <summary>The factories of <see cref="StringPredicates"/>.</summary>
    String = 2,

    /// <summary>The factories of <see cref="DateTimePredicates"/>: ordering, range, clock, day-of-week, month and time-window predicates.</summary>
    DateTime = 3,

    /// <summary>The factories of <see cref="CollectionPredicates"/>.</summary>
    Collection = 4,

    /// <summary>The factories of <see cref="RegexPredicates"/>.</summary>
    Regex = 5,

    /// <summary>The factories of <see cref="TypePredicates"/>.</summary>
    Type = 6,
}
