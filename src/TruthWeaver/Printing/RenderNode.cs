namespace TruthWeaver.Printing;

/// <summary>One node of a format-agnostic render tree: a label plus its <see cref="RenderState"/>.</summary>
/// <param name="Label">The node's display label, from <see cref="Evaluation.OutlineNode.Label"/>.</param>
/// <param name="State">The node's rendering state.</param>
/// <param name="Children">The node's rendered operands, in source order.</param>
/// <param name="Kind">The node's role, from <see cref="Evaluation.OutlineNode.Kind"/>; an outline node with operands is always an operator.</param>
internal sealed record RenderNode(
    string Label,
    RenderState State,
    IReadOnlyList<RenderNode> Children,
    Evaluation.OutlineNodeKind Kind = Evaluation.OutlineNodeKind.Term
);
