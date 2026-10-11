namespace TruthWeaver.Rewriting;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The primitive forms of the four inspections (<c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c>, <c>IsKnown</c>): the
/// builder and the recogniser for the two paired forms, side by side (see <see cref="XorForm"/> for why they live
/// together).
/// </summary>
/// <remarks>
/// The inspections look at the K3 <em>state</em>, which no connective alone can see, but <c>COALESCE</c> can:
/// <c>COALESCE(x, False)</c> maps Unknown to False and leaves True/False alone. From it:
/// <c>IsTrue(x) = COALESCE(x, False)</c>; <c>IsFalse(x) = COALESCE(NOT x, False)</c>;
/// <c>IsUnknown(x) = COALESCE(x, True) AND COALESCE(NOT x, True)</c> (both are True only when x is Unknown: a True x makes
/// the second False and a False x makes the first False); <c>IsKnown(x) = COALESCE(x, False) OR COALESCE(NOT x, False)</c>.
/// All four therefore expand to the kernel and no inspection is left as a semantic boundary. Only the two paired forms
/// are recognised: <c>IsTrue</c> and <c>IsFalse</c> are a bare <c>COALESCE</c>, which the compressor leaves as written.
/// The recogniser takes the two members in either order because <c>AND</c> and <c>OR</c> are commutative.
/// </remarks>
internal static class InspectionForm
{
    /// <summary>Builds the primitive form of an inspection.</summary>
    /// <param name="kind">Which inspection.</param>
    /// <param name="operand">The inspected expression.</param>
    /// <returns>A <c>COALESCE</c>, or the <c>AND</c> / <c>OR</c> of two for the paired forms.</returns>
    public static Expression Build(InspectionKind kind, Expression operand)
    {
        return kind switch
        {
            InspectionKind.IsTrue => Coalesce(operand, TruthValue.False),
            InspectionKind.IsFalse => Coalesce(Forms.Not(operand), TruthValue.False),
            InspectionKind.IsUnknown => Forms.And(
                Coalesce(operand, TruthValue.True),
                Coalesce(Forms.Not(operand), TruthValue.True)
            ),
            InspectionKind.IsKnown => Forms.Or(
                Coalesce(operand, TruthValue.False),
                Coalesce(Forms.Not(operand), TruthValue.False)
            ),
            _ => throw new InvalidOperationException($"Unhandled inspection kind '{kind}'."),
        };
    }

    /// <summary>
    /// Recognises <c>COALESCE(x, fallback)</c> beside <c>COALESCE(NOT x, fallback)</c> in either order: the two halves of
    /// <c>IsKnown</c> (fallback <c>False</c>, inside an <c>OR</c>) and <c>IsUnknown</c> (fallback <c>True</c>, inside an
    /// <c>AND</c>). The caller supplies the operands of the matching connective.
    /// </summary>
    /// <param name="pair">The two operands of the <c>OR</c> (for <c>IsKnown</c>) or <c>AND</c> (for <c>IsUnknown</c>).</param>
    /// <param name="kind">The inspection looked for: <c>IsKnown</c> or <c>IsUnknown</c>.</param>
    /// <param name="operand">The inspected expression when matched.</param>
    /// <returns><see langword="true"/> when <paramref name="pair"/> is exactly the form of <paramref name="kind"/>.</returns>
    public static bool TryMatchPair(
        EquatableArray<Expression> pair,
        InspectionKind kind,
        [NotNullWhen(true)] out Expression? operand
    )
    {
        operand = null;
        if (pair.Count != 2 || kind is not (InspectionKind.IsKnown or InspectionKind.IsUnknown))
        {
            return false;
        }

        TruthValue fallback = kind == InspectionKind.IsKnown ? TruthValue.False : TruthValue.True;
        return TryMatchOrdered(pair[0], pair[1], fallback, out operand)
            || TryMatchOrdered(pair[1], pair[0], fallback, out operand);
    }

    private static CoalesceExpression Coalesce(Expression operand, TruthValue fallback)
    {
        return new CoalesceExpression(new EquatableArray<Expression>([operand, new ConstantExpression(fallback)]));
    }

    private static bool TryMatchOrdered(
        Expression first,
        Expression second,
        TruthValue fallback,
        [NotNullWhen(true)] out Expression? operand
    )
    {
        operand = null;
        if (
            first is CoalesceExpression { Operands: { Count: 2 } a }
            && second is CoalesceExpression { Operands: { Count: 2 } b }
            && a[1] is ConstantExpression { Value: var fa }
            && b[1] is ConstantExpression { Value: var fb }
            && fa == fallback
            && fb == fallback
            && b[0] is NotExpression negated
            && negated.Operand.Equals(a[0])
        )
        {
            operand = a[0];
            return true;
        }

        return false;
    }
}
