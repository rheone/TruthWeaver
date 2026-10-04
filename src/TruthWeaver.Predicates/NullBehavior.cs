namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Chooses what a built-in predicate answers when its selector returns <see langword="null"/>. The host
/// picks this once, when it registers the predicate; rule authors cannot change it.
/// </summary>
/// <remarks>
/// <see cref="False"/> keeps the historical behaviour: a missing value is a definite, fault-free
/// <see cref="TruthValue.False"/>, so <c>NOT predicate</c> is <see cref="TruthValue.True"/> for a null
/// value. <see cref="Unknown"/> is the Strong Kleene reading of a missing value: the predicate answers
/// <see cref="TruthValue.Unknown"/> (still without a fault), <c>NOT</c> of it stays
/// <see cref="TruthValue.Unknown"/>, and <c>Decision.IsSatisfied</c> remains fail-closed.
/// </remarks>
public enum NullBehavior
{
    /// <summary>A null selected value yields a definite <see cref="TruthValue.False"/> (the default).</summary>
    False = 0,

    /// <summary>A null selected value yields <see cref="TruthValue.Unknown"/>, with no fault recorded.</summary>
    Unknown = 1,
}
