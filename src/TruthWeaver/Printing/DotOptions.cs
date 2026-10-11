namespace TruthWeaver.Printing;

/// <summary>The options for <see cref="Evaluation.CompiledRule{TContext}.PrintDot(DotOptions?)"/>.</summary>
public sealed record DotOptions
{
    /// <summary>
    /// Gets the default options: top-down layout, a distinct shape for each node role, word operators, argument values shown and
    /// the constants as the words <c>True</c>, <c>False</c> and <c>Unknown</c>.
    /// </summary>
    public static DotOptions Default { get; } = new();

    /// <summary>Gets the layout direction (the Graphviz <c>rankdir</c>). Defaults to <see cref="MermaidDirection.TopDown"/>.</summary>
    public MermaidDirection Direction { get; init; } = MermaidDirection.TopDown;

    /// <summary>
    /// Gets a value indicating whether each node role gets its own shape: a hexagon for an operator, a rounded box for a term and
    /// a circle for a constant. When <see langword="false"/>, every node is a plain box. Defaults to <see langword="true"/>.
    /// </summary>
    public bool NodeShapes { get; init; } = true;

    /// <summary>Gets how operator labels are written. Defaults to <see cref="Printing.OperatorStyle.Word"/>.</summary>
    public OperatorStyle OperatorStyle { get; init; } = OperatorStyle.Word;

    /// <summary>Gets a value indicating whether a term's rule-text argument values appear in its label. Defaults to <see langword="true"/>.</summary>
    public bool ShowArgumentValues { get; init; } = true;

    /// <summary>Gets how the constants <c>True</c>, <c>False</c> and <c>Unknown</c> are written. Defaults to <see cref="ConstantFormat.Default"/>.</summary>
    public ConstantFormat Constants { get; init; } = ConstantFormat.Default;
}
