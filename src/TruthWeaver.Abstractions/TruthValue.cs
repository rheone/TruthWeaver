namespace TruthWeaver.Abstractions;

/// <summary>
/// A three-valued (Kleene) truth value. Never represented as <c>bool?</c>:
/// a dedicated enum keeps <see cref="Unknown"/> out of accidental boolean contexts and gives
/// callers switch exhaustiveness. See ADR-0001 for the truth tables and the reasoning.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> is the zero value, so <c>default(TruthValue)</c>, an element of a new array and an unset field
/// are <see cref="Unknown"/>, never a definite answer. The underlying integers (<c>Unknown = 0</c>, <c>False = 1</c>,
/// <c>True = 2</c>) are not the truth order (<c>False &lt; Unknown &lt; True</c>): do not compare or persist them, and
/// store a value by its member name.
/// </remarks>
public enum TruthValue
{
    /// <summary>
    /// The expression could not be determined: either a predicate faulted, or the value
    /// depends on an operand that itself is <see cref="Unknown"/> in a way the operator's
    /// truth table cannot resolve. This is the zero value, <c>default(TruthValue)</c>.
    /// </summary>
    Unknown = 0,

    /// <summary>The expression is definitely false.</summary>
    False = 1,

    /// <summary>The expression is definitely true.</summary>
    True = 2,
}
