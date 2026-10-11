namespace TruthWeaver.Rewriting;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The primitive form of <c>If(c, t, f)</c>, the multiplexer <c>(c AND t) OR (NOT c AND f)</c> plus the consensus term
/// <c>(t AND f)</c>: its builder and its recogniser side by side (see <see cref="XorForm"/> for why they live together).
/// </summary>
/// <remarks>
/// The consensus term is what keeps an Unknown condition from guessing a branch (ADR-0005 decision 13). The recogniser
/// relies on the builder's order: a three-term <c>OR</c> of two-term <c>AND</c>s in the order multiplexer-true,
/// multiplexer-false, consensus. Unlike <see cref="XorForm"/> it does not try other orders.
/// </remarks>
internal static class IfForm
{
    /// <summary>Builds the primitive form.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="whenTrue">The branch taken when the condition is True.</param>
    /// <param name="whenFalse">The branch taken when the condition is False.</param>
    /// <returns>The three-term <c>OR</c>.</returns>
    public static OrExpression Build(Expression condition, Expression whenTrue, Expression whenFalse)
    {
        return Forms.Or(
            Forms.And(condition, whenTrue),
            Forms.And(Forms.Not(condition), whenFalse),
            Forms.And(whenTrue, whenFalse)
        );
    }

    /// <summary>Recognises the primitive form.</summary>
    /// <param name="disjuncts">The operands of an <c>OR</c>.</param>
    /// <param name="condition">The condition when matched.</param>
    /// <param name="whenTrue">The True branch when matched.</param>
    /// <param name="whenFalse">The False branch when matched.</param>
    /// <returns><see langword="true"/> when <paramref name="disjuncts"/> is exactly the form.</returns>
    public static bool TryMatch(
        EquatableArray<Expression> disjuncts,
        [NotNullWhen(true)] out Expression? condition,
        [NotNullWhen(true)] out Expression? whenTrue,
        [NotNullWhen(true)] out Expression? whenFalse
    )
    {
        condition = null;
        whenTrue = null;
        whenFalse = null;
        if (
            disjuncts.Count == 3
            && disjuncts[0] is AndExpression { Operands: { Count: 2 } first }
            && disjuncts[1] is AndExpression { Operands: { Count: 2 } second }
            && disjuncts[2] is AndExpression { Operands: { Count: 2 } consensus }
            && second[0] is NotExpression negated
            && negated.Operand.Equals(first[0])
            && consensus[0].Equals(first[1])
            && consensus[1].Equals(second[1])
        )
        {
            condition = first[0];
            whenTrue = first[1];
            whenFalse = second[1];
            return true;
        }

        return false;
    }
}
