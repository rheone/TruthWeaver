namespace TruthWeaver.Rewriting;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The primitive form of <c>EQUIVALENT(l, r)</c>, <c>(l AND r) OR (NOT l AND NOT r)</c>: its builder and its recogniser
/// side by side (see <see cref="XorForm"/> for why they live together).
/// </summary>
/// <remarks>
/// The recogniser relies on the builder's shape: a two-term <c>OR</c> whose first term is <c>l AND r</c> and whose second
/// negates both operands. It accepts the two disjuncts in either order because <c>OR</c> is commutative.
/// </remarks>
internal static class EquivalentForm
{
    /// <summary>Builds the primitive form: the negation of XOR, Unknown whenever either operand is.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The two-term <c>OR</c>.</returns>
    public static OrExpression Build(Expression left, Expression right)
    {
        return Forms.Or(Forms.And(left, right), Forms.And(Forms.Not(left), Forms.Not(right)));
    }

    /// <summary>Recognises the primitive form in either disjunct order.</summary>
    /// <param name="disjuncts">The operands of an <c>OR</c>.</param>
    /// <param name="left">The left operand when matched.</param>
    /// <param name="right">The right operand when matched.</param>
    /// <returns><see langword="true"/> when <paramref name="disjuncts"/> is exactly the form.</returns>
    public static bool TryMatch(
        EquatableArray<Expression> disjuncts,
        [NotNullWhen(true)] out Expression? left,
        [NotNullWhen(true)] out Expression? right
    )
    {
        left = null;
        right = null;
        return disjuncts.Count == 2
            && (
                TryMatchOrdered(disjuncts[0], disjuncts[1], out left, out right)
                || TryMatchOrdered(disjuncts[1], disjuncts[0], out left, out right)
            );
    }

    private static bool TryMatchOrdered(
        Expression first,
        Expression second,
        [NotNullWhen(true)] out Expression? left,
        [NotNullWhen(true)] out Expression? right
    )
    {
        left = null;
        right = null;
        if (
            first is AndExpression { Operands: { Count: 2 } a }
            && second is AndExpression { Operands: { Count: 2 } b }
            && b[0] is NotExpression notLeft
            && b[1] is NotExpression notRight
            && notLeft.Operand.Equals(a[0])
            && notRight.Operand.Equals(a[1])
        )
        {
            left = a[0];
            right = a[1];
            return true;
        }

        return false;
    }
}
