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

    /// <summary>Gets how operator labels are rendered. Defaults to <see cref="Printing.OperatorStyle.Word"/>.</summary>
    public OperatorStyle OperatorStyle { get; init; } = OperatorStyle.Word;

    /// <summary>Gets a value indicating whether a term's rule-text argument values appear in its label. Defaults to <see langword="true"/>.</summary>
    public bool ShowArgumentValues { get; init; } = true;
}
