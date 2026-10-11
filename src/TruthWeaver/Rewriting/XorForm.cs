namespace TruthWeaver.Rewriting;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The primitive form of <c>XOR(l, r)</c>, <c>(l AND NOT r) OR (NOT l AND r)</c>: its builder and its recogniser side by
/// side, so a change to one cannot drift from the other. Adding a derived operator needs one form type like this one, not
/// an edit in each of <see cref="PrimitiveExpander"/> and <see cref="Compressor"/>.
/// </summary>
/// <remarks>
/// The recogniser relies on what the builder produces: a two-term <c>OR</c> of two-term <c>AND</c>s, the negations on the
/// second operand of the first term and the first operand of the second. It does not assume the order of the two
/// disjuncts, because <c>OR</c> is commutative and a rewrite elsewhere may have swapped them. Operands are compared with
/// structural equality, so a shared and a rebuilt copy of an operand match alike.
/// </remarks>
internal static class XorForm
{
    /// <summary>Builds the primitive form. Unknown whenever either operand is.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The two-term <c>OR</c>.</returns>
    public static OrExpression Build(Expression left, Expression right)
    {
        return Forms.Or(Forms.And(left, Forms.Not(right)), Forms.And(Forms.Not(left), right));
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
            && a[1] is NotExpression notRight
            && b[0] is NotExpression notLeft
            && notLeft.Operand.Equals(a[0])
            && notRight.Operand.Equals(b[1])
        )
        {
            left = a[0];
            right = b[1];
            return true;
        }

        return false;
    }
}
