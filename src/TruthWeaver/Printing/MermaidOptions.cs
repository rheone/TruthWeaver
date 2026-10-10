namespace TruthWeaver.Printing;

using TruthWeaver.Evaluation;

/// <summary>
/// Options for <see cref="MermaidTreePrinter"/> and <c>CompiledRule.PrintMermaid</c>. The default
/// instance produces the same output as the option-less overloads.
/// </summary>
public sealed record MermaidOptions
{
    /// <summary>Gets the layout direction of the flowchart. Defaults to <see cref="MermaidDirection.TopDown"/>.</summary>
    public MermaidDirection Direction { get; init; } = MermaidDirection.TopDown;

    /// <summary>
    /// Gets a value indicating whether each node role gets its own shape: a hexagon for an operator, a
    /// rounded box for a term and a circle for a constant. When <see langword="false"/> (the default),
    /// every node is a plain rectangle.
    /// </summary>
    public bool NodeShapes { get; init; }

    /// <summary>
    /// Gets a value indicating whether a term renders as two lines: its label in bold, then its argument
    /// values in plain text. The labels use Mermaid markdown strings, which GitHub renders; the output
    /// contains no raw HTML. Dimmed or smaller text is not available, because GitHub's sanitizer removes
    /// the HTML and CSS that it needs. The argument line appears only when <see cref="ShowArgumentValues"/>
    /// is <see langword="true"/>. Defaults to <see langword="false"/>.
    /// </summary>
    public bool TwoLineTermLabels { get; init; }

    /// <summary>
    /// Gets the colors of the evaluation states and of the highlight and mute classes. Defaults to
    /// <see cref="MermaidPalette.Light"/>.
    /// </summary>
    public MermaidPalette Palette { get; init; } = MermaidPalette.Light;

    /// <summary>Gets how operator labels are rendered. Defaults to <see cref="Printing.OperatorStyle.Word"/>.</summary>
    public OperatorStyle OperatorStyle { get; init; } = OperatorStyle.Word;

    /// <summary>Gets a value indicating whether a term's rule-text argument values appear in its label. Defaults to <see langword="true"/>.</summary>
    public bool ShowArgumentValues { get; init; } = true;

    /// <summary>
    /// Gets a callback that picks a style for any node, or <see langword="null"/> (the default) for none. The
    /// callback runs for every node after evaluation coloring, and a style that it returns replaces the
    /// evaluation color of that node. A <see langword="null"/> result leaves the node as it is. The printer
    /// defines the highlight and mute classes from <see cref="Palette"/>, and only when a node uses them.
    /// </summary>
    public Func<OutlineNode, NodeStyle?>? NodeStyle { get; init; }

    /// <summary>
    /// Gets the operand count above which a flat <c>AND</c> or <c>OR</c> of only terms and constants is drawn
    /// inside a Mermaid <c>subgraph</c> box, or <see langword="null"/> (the default) to turn compaction off.
    /// A chain with this many operands or fewer, and a chain that holds an operator, keep the plain layout.
    /// No operand is hidden.
    /// </summary>
    public int? CompactChainThreshold { get; init; }
}
