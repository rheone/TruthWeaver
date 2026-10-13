namespace TruthWeaver.Evaluation;

/// <summary>
/// The coverage of one node of a rule (an operator, a term or a constant at one place in the tree) over a set of
/// decisions (<see cref="RuleCoverage"/>). Each operand of an operator is a branch of that operator.
/// </summary>
/// <param name="Path">The tree path of the node: <c>$</c> for the root, <c>$.operands[1]</c> for the second operand of the root.</param>
/// <param name="Text">The text of the node as the trace shows it: the operator label, the term or the constant.</param>
/// <param name="EvaluatedCount">The number of decisions in which the node was evaluated.</param>
/// <param name="NotEvaluatedCount">The number of decisions in which the node, or a node above it, was skipped by short-circuiting or a fault budget.</param>
/// <param name="TrueCount">The number of decisions in which the node was <see cref="TruthValue.True"/>.</param>
/// <param name="FalseCount">The number of decisions in which the node was <see cref="TruthValue.False"/>.</param>
/// <param name="UnknownCount">The number of decisions in which the node was <see cref="TruthValue.Unknown"/>.</param>
/// <param name="DecidedCount">
/// The number of decisions the node decided: a different value at this place only, with every other evaluated node
/// kept and every skipped node read as <see cref="TruthValue.Unknown"/>, changes the result of the rule.
/// </param>
/// <param name="Operands">The operands (branches) of the node, in source order; empty for a term or a constant.</param>
public sealed record NodeCoverage(
    string Path,
    string Text,
    int EvaluatedCount,
    int NotEvaluatedCount,
    int TrueCount,
    int FalseCount,
    int UnknownCount,
    int DecidedCount,
    EquatableArray<NodeCoverage> Operands
)
{
    /// <summary>Gets the operands of this node that decided no decision, in source order.</summary>
    public IReadOnlyList<NodeCoverage> UndecidedOperands => [.. this.Operands.Where(operand => operand.DecidedCount == 0)];
}
