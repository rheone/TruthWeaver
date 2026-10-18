namespace TruthWeaver.Tool.Core;

/// <summary>One node of a <see cref="RuleTree"/>, for a front end that lists the tree and edits it.</summary>
/// <param name="Path">
/// The path of the node in the tree, in the form of the JSON and YAML diagnostics and of <c>CompiledRule.Notes</c>: <c>$</c>
/// is the root and <c>$.operands[1]</c> is its second operand.
/// </param>
/// <param name="Depth">The depth of the node: 0 for the root.</param>
/// <param name="Kind">The kind of the node.</param>
/// <param name="Label">
/// A short text for the node: the operator name with its count or bounds (such as <c>atLeast k=2</c>), the canonical text of
/// a term, or the constant.
/// </param>
/// <param name="OperandCount">The number of operands of an operator; 0 for a term or a constant.</param>
/// <param name="Literals">
/// The names of the literal values of the node that <see cref="RuleTreeEditor.ChangeLiteral"/> can change, with their
/// current text as JSON: <c>const</c> for a constant, each argument name for a term, and <c>k</c>, <c>min</c> or <c>max</c>
/// for an operator that has them.
/// </param>
/// <param name="Note">The note of the node, or <see langword="null"/>.</param>
public sealed record RuleTreeNode(
    string Path,
    int Depth,
    RuleTreeNodeKind Kind,
    string Label,
    int OperandCount,
    IReadOnlyList<KeyValuePair<string, string>> Literals,
    string? Note
);
