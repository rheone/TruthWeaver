namespace TruthWeaver.Printing;

/// <summary>One node of a format-agnostic render tree: a label plus its <see cref="RenderState"/>.</summary>
/// <param name="Label">The node's display label, from <see cref="Evaluation.OutlineNode.Label"/>.</param>
/// <param name="State">The node's rendering state.</param>
/// <param name="Children">The node's rendered operands, in source order.</param>
/// <param name="Kind">The node's role, from <see cref="Evaluation.OutlineNode.Kind"/>; an outline node with operands is always an operator.</param>
/// <param name="Heading">The label without the argument text; a printer that lays out the argument text on its own line uses it.</param>
/// <param name="Arguments">The argument text, or <see langword="null"/> when the node has none or the caller hides it.</param>
/// <param name="Source">The outline node that this node renders, or <see langword="null"/> for a hand-built node.</param>
internal sealed record RenderNode(
    string Label,
    RenderState State,
    IReadOnlyList<RenderNode> Children,
    Evaluation.OutlineNodeKind Kind = Evaluation.OutlineNodeKind.Term,
    string? Heading = null,
    string? Arguments = null,
    Evaluation.OutlineNode? Source = null
);
