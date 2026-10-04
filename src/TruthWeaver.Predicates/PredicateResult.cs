namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Adapts the built-in predicates' internally boolean comparisons to the Kleene
/// <see cref="TruthValue"/> a predicate delegate returns. Built-in predicates always reach a definite
/// answer (a null selected value is a definite <see cref="TruthValue.False"/> unless the
/// host opted into <see cref="NullBehavior.Unknown"/>), so a comparison itself only produces <c>True</c>
/// and <c>False</c>.
/// </summary>
internal static class PredicateResult
{
    /// <summary>Wraps a definite boolean outcome as a completed predicate result.</summary>
    /// <param name="value">The boolean outcome of the comparison.</param>
    /// <returns>A completed <see cref="ValueTask{TruthValue}"/> holding <see cref="TruthValue.True"/> or <see cref="TruthValue.False"/>.</returns>
    public static ValueTask<TruthValue> FromBoolAsync(bool value)
    {
        return ValueTask.FromResult(value ? TruthValue.True : TruthValue.False);
    }

    /// <summary>Gets the completed result for a null selected value under the host's chosen behaviour.</summary>
    /// <param name="nullBehavior">What the host configured a null selected value to mean.</param>
    /// <returns>A completed <see cref="ValueTask{TruthValue}"/> holding <see cref="TruthValue.False"/> or <see cref="TruthValue.Unknown"/>; never a fault.</returns>
    public static ValueTask<TruthValue> ForNullAsync(NullBehavior nullBehavior)
    {
        return ValueTask.FromResult(nullBehavior == NullBehavior.Unknown ? TruthValue.Unknown : TruthValue.False);
    }
}
