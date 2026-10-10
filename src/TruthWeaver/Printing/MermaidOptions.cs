namespace TruthWeaver.Printing;

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
}
