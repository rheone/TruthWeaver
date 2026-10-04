namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Adapts the built-in predicates' internally boolean comparisons to the Kleene
/// <see cref="TruthValue"/> a predicate delegate returns. Built-in predicates always reach a definite
/// answer for a non-null selected value, so a comparison itself only produces <c>True</c> and <c>False</c>. A null
/// selected value answers per <see cref="NullBehavior"/>, complemented for a <c>NotX</c> twin.
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

    /// <summary>
    /// Gets the completed result for a null selected value under the host's chosen behaviour. A positive predicate
    /// answers <see cref="TruthValue.False"/> or <see cref="TruthValue.Unknown"/>. A <c>NotX</c> twin answers the
    /// Strong Kleene complement of that answer, so under <see cref="NullBehavior.False"/> it is
    /// <see cref="TruthValue.True"/> and under <see cref="NullBehavior.Unknown"/> it stays
    /// <see cref="TruthValue.Unknown"/>.
    /// </summary>
    /// <param name="nullBehavior">What the host configured a null selected value to mean.</param>
    /// <param name="negate"><see langword="true"/> for a <c>NotX</c> twin.</param>
    /// <returns>A completed <see cref="ValueTask{TruthValue}"/>; never a fault.</returns>
    public static ValueTask<TruthValue> ForNullAsync(NullBehavior nullBehavior, bool negate = false)
    {
        TruthValue positive = nullBehavior == NullBehavior.Unknown ? TruthValue.Unknown : TruthValue.False;
        return ValueTask.FromResult(negate ? Not(positive) : positive);
    }

    /// <summary>The Strong Kleene <c>NOT</c>: swaps <see cref="TruthValue.True"/> and <see cref="TruthValue.False"/> and keeps <see cref="TruthValue.Unknown"/>.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The complement of <paramref name="value"/>.</returns>
    public static TruthValue Not(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }
}
