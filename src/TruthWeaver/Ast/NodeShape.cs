namespace TruthWeaver.Ast;

/// <summary>
/// The structural shape of an operator <see cref="Expression"/> node: its canonical op-name, its
/// threshold <c>K</c> (only for <see cref="ThresholdExpression"/> and, as its lower bound, <see cref="BetweenExpression"/>;
/// <see langword="null"/> otherwise), the upper bound <c>Max</c> (only for <see cref="BetweenExpression"/>),
/// and its operand list, in source order.
/// </summary>
/// <param name="OpName">
/// The node's canonical, format-neutral op-name, e.g. <c>"And"</c>, <c>"Xor"</c>, or, for a
/// <see cref="ThresholdExpression"/>, its <see cref="ThresholdComparison"/>'s name (e.g.
/// <c>"AtLeast"</c>). Each consumer maps this to its own format-specific keyword or label; the seam
/// only settles what the name structurally is, not how any one format spells it.
/// </param>
/// <param name="K">The threshold value for a <see cref="ThresholdExpression"/>, or the lower bound (<c>min</c>) for a <see cref="BetweenExpression"/>; otherwise <see langword="null"/>.</param>
/// <param name="Operands">The node's operands, in source order.</param>
/// <param name="Max">The upper bound (<c>max</c>) for a <see cref="BetweenExpression"/>; otherwise <see langword="null"/>.</param>
internal readonly record struct NodeShape(string OpName, int? K, IReadOnlyList<Expression> Operands, int? Max = null);
