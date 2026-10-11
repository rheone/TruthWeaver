namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>Node constructors shared by the derived-operator form types, so each form reads as its formula.</summary>
internal static class Forms
{
    /// <summary>Builds <c>NOT operand</c>.</summary>
    /// <param name="operand">The negated expression.</param>
    /// <returns>The negation.</returns>
    public static NotExpression Not(Expression operand)
    {
        return new NotExpression(operand);
    }

    /// <summary>Builds an <c>AND</c> of the operands, in order.</summary>
    /// <param name="operands">The conjuncts (at least two).</param>
    /// <returns>The conjunction.</returns>
    public static AndExpression And(params Expression[] operands)
    {
        return new AndExpression(new EquatableArray<Expression>(operands));
    }

    /// <summary>Builds an <c>OR</c> of the operands, in order.</summary>
    /// <param name="operands">The disjuncts (at least two).</param>
    /// <returns>The disjunction.</returns>
    public static OrExpression Or(params Expression[] operands)
    {
        return new OrExpression(new EquatableArray<Expression>(operands));
    }
}
