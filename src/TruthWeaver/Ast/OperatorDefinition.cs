namespace TruthWeaver.Ast;

/// <summary>
/// The descriptive facts about one operator in the closed set (ADR-0004): how it is named, how many operands it takes,
/// and how it is labelled and described. Holds no behaviour: evaluation, analysis and rewriting differ per operator and
/// keep switching on the node type.
/// </summary>
/// <param name="OpName">The canonical, format-neutral op-name, equal to <see cref="NodeShape.OpName"/> for the node.</param>
/// <param name="TreeFormatName">The op string the JSON and YAML tree formats write, e.g. <c>"atLeast"</c>.</param>
/// <param name="MinOperands">The fewest operands the operator accepts.</param>
/// <param name="MaxOperands">The most operands the operator accepts, or <see langword="null"/> when unbounded.</param>
/// <param name="LabelTemplate">The display label; <c>{K}</c> and <c>{Max}</c> stand for the node's threshold and upper bound.</param>
/// <param name="DescriptionTemplate">The human-readable description; <c>{K}</c> and <c>{Max}</c> as in <paramref name="LabelTemplate"/>.</param>
internal sealed record OperatorDefinition(
    string OpName,
    string TreeFormatName,
    int MinOperands,
    int? MaxOperands,
    string LabelTemplate,
    string DescriptionTemplate
)
{
    /// <summary>Gets the display label for a node of this operator, with its threshold and bound filled in.</summary>
    /// <param name="shape">The node's structural shape.</param>
    /// <returns>The label.</returns>
    public string Label(NodeShape shape)
    {
        return Fill(this.LabelTemplate, shape);
    }

    /// <summary>Gets the description for a node of this operator, with its threshold and bound filled in.</summary>
    /// <param name="shape">The node's structural shape.</param>
    /// <returns>The description.</returns>
    public string Describe(NodeShape shape)
    {
        return Fill(this.DescriptionTemplate, shape);
    }

    private static string Fill(string template, NodeShape shape)
    {
        return template
            .Replace("{K}", shape.K?.ToString(), StringComparison.Ordinal)
            .Replace("{Max}", shape.Max?.ToString(), StringComparison.Ordinal);
    }
}
