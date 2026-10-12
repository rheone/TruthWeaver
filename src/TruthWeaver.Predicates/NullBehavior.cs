namespace TruthWeaver.Predicates;

/// <summary>
/// Chooses what a built-in predicate answers when its selector returns <see langword="null"/>. The host
/// picks this when it registers the predicate, for one predicate or, through <see cref="NullBehaviorScope"/>, for every
/// predicate it registers without an explicit choice; rule authors cannot change it. Every built-in predicate that takes
/// a <see cref="NullBehavior"/> uses <see cref="Unknown"/> unless a scope sets another default.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> is the Strong Kleene reading of a missing value: the predicate cannot evaluate it, so it answers
/// <see cref="TruthValue.Unknown"/> (without a fault), <c>NOT</c> of it stays <see cref="TruthValue.Unknown"/>, and
/// <c>Decision.IsSatisfied</c> remains fail-closed. <see cref="False"/> makes a missing value a definite, fault-free
/// answer. In both cases <c>NOT predicate</c> and the predicate's <c>NotX</c> twin agree for a null value.
/// </remarks>
public enum NullBehavior
{
    /// <summary>
    /// A null selected value yields <see cref="TruthValue.Unknown"/>, with no fault recorded. This is the zero value, so a
    /// forgotten or defaulted <see cref="NullBehavior"/> is the documented default.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A null selected value yields a definite <see cref="TruthValue.False"/> for a positive predicate and
    /// <see cref="TruthValue.True"/> for its <c>NotX</c> twin. The exceptions are
    /// <see cref="CollectionPredicates.SetEquals{TContext}"/>, which reads a null collection as an empty collection, so it
    /// answers <see cref="TruthValue.True"/> when the argument array is empty, and
    /// <see cref="CollectionPredicates.NotSetEquals{TContext}"/>, which answers the complement.
    /// </summary>
    False = 1,

    /// <summary>
    /// No choice for this predicate: it follows the open <see cref="NullBehaviorScope"/> for its
    /// <see cref="PredicateFamily"/>, and answers as <see cref="Unknown"/> when no scope applies. This is the value every
    /// factory parameter has when the caller passes nothing. A factory resolves it when it runs, so a predicate never
    /// evaluates with this value.
    /// </summary>
    Default = 2,
}
