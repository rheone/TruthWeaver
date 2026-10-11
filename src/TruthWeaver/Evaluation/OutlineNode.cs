namespace TruthWeaver.Evaluation;

/// <summary>
/// One node of a rule's outline: the static, human-readable tree of a compiled expression — an operator's or a
/// predicate's <c>Label</c>/<c>Description</c>, plus its operands' own outline nodes, recursively. The root
/// node returned by <see cref="CompiledRule{TContext}.Outline"/> is the rule's outline.
/// Produced by <see cref="CompiledRule{TContext}.Outline"/> so a rule-authoring UI or generated
/// documentation can render an entire compiled rule without needing access to the closed-set AST
/// types themselves (ADR-0004).
/// </summary>
/// <param name="Label">A short, human-friendly display name for this node.</param>
/// <param name="Description">A human-readable description of what this node means.</param>
/// <param name="Operands">This node's operands, outlined the same way, or empty for a leaf node.</param>
/// <param name="ArgumentText">
/// A term's rule-text arguments, rendered as comma-joined <c>name: value</c> pairs, or
/// <see langword="null"/> for an operator, a constant, or a zero-argument term.
/// </param>
/// <param name="Kind">
/// The node's role. The printers use it to pick a node shape. A node with operands is always an operator,
/// whatever its <paramref name="Kind"/>; a hand-built leaf defaults to <see cref="OutlineNodeKind.Term"/>.
/// </param>
public sealed record OutlineNode(
    string Label,
    string Description,
    IReadOnlyList<OutlineNode> Operands,
    string? ArgumentText = null,
    OutlineNodeKind Kind = OutlineNodeKind.Term
);
