namespace TruthWeaver.Printing;

using System.Text;
using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// Renders a rule's <see cref="OutlineNode"/> tree (from <c>CompiledRule.Outline()</c>) as
/// Mermaid <c>flowchart</c> syntax, for delivery to a UI that renders Mermaid diagrams. Optionally
/// colored by one evaluation's <see cref="Decision.TraceTree"/>, via the shared
/// <see cref="RuleRenderTree"/>.
/// </summary>
public static class MermaidTreePrinter
{
    /// <summary>Prints a rule's structure only, with no evaluation coloring.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/EQUIVALENT operator labels. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public static string Print(OutlineNode root, OperatorStyle style = OperatorStyle.Word, bool showArgumentValues = true)
    {
        return Print(RuleRenderTree.Build(root, style, showArgumentValues), new MermaidOptions());
    }

    /// <summary>Prints a rule's structure only, with no evaluation coloring, using <paramref name="options"/>.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="options">The diagram direction, node shapes, operator style and argument-value switch.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public static string Print(OutlineNode root, MermaidOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Print(RuleRenderTree.Build(root, options.OperatorStyle, options.ShowArgumentValues), options);
    }

    /// <summary>Prints a rule's structure, colored by one evaluation's result and short-circuit path.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="traceTree">The matching <see cref="Decision.TraceTree"/> from that evaluation.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/EQUIVALENT operator labels. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public static string Print(
        OutlineNode root,
        TraceNode traceTree,
        OperatorStyle style = OperatorStyle.Word,
        bool showArgumentValues = true
    )
    {
        return Print(RuleRenderTree.Build(root, traceTree, style, showArgumentValues), new MermaidOptions());
    }

    /// <summary>Prints a rule's structure, colored by one evaluation, using <paramref name="options"/>.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="traceTree">The matching <see cref="Decision.TraceTree"/> from that evaluation.</param>
    /// <param name="options">The diagram direction, node shapes, operator style and argument-value switch.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public static string Print(OutlineNode root, TraceNode traceTree, MermaidOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Print(RuleRenderTree.Build(root, traceTree, options.OperatorStyle, options.ShowArgumentValues), options);
    }

    private static string Print(RenderNode root, MermaidOptions options)
    {
        StringBuilder text = new();
        text.Append("flowchart ").Append(DirectionCode(options.Direction)).Append('\n');
        text.Append("    Start([\"Start\"]) --> n0\n");
        List<string> classAssignments = [];
        int nextId = 0;
        bool anyColored = WriteNode(root, options, text, classAssignments, ref nextId) != RenderState.NoData;

        foreach (string assignment in classAssignments)
        {
            text.Append(assignment).Append('\n');
        }

        if (anyColored || classAssignments.Count > 0)
        {
            WriteClassDefs(text);
        }

        return text.ToString();
    }

    private static RenderState WriteNode(
        RenderNode node,
        MermaidOptions options,
        StringBuilder text,
        List<string> classAssignments,
        ref int nextId
    )
    {
        string id = $"n{nextId++}";
        (string open, string close) = options.NodeShapes ? ShapeFor(node.Kind) : ("[", "]");
        text.Append("    ")
            .Append(id)
            .Append(open)
            .Append('"')
            .Append(Escape(node.Label))
            .Append('"')
            .Append(close)
            .Append('\n');

        string? className = ClassFor(node.State);
        if (className is not null)
        {
            classAssignments.Add($"    class {id} {className}");
        }

        foreach (RenderNode child in node.Children)
        {
            int childId = nextId;
            WriteNode(child, options, text, classAssignments, ref nextId);
            text.Append("    ").Append(id).Append(" --> n").Append(childId).Append('\n');
        }

        return node.State;
    }

    private static string DirectionCode(MermaidDirection direction)
    {
        return direction switch
        {
            MermaidDirection.TopDown => "TD",
            MermaidDirection.LeftRight => "LR",
            MermaidDirection.BottomTop => "BT",
            MermaidDirection.RightLeft => "RL",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unhandled Mermaid direction."),
        };
    }

    /// <summary>
    /// The Mermaid open and close delimiters per node role: hexagon, rounded box and circle. They are
    /// part of the classic flowchart syntax, so GitHub's sanitizing renderer accepts them.
    /// </summary>
    private static (string Open, string Close) ShapeFor(OutlineNodeKind kind)
    {
        return kind switch
        {
            OutlineNodeKind.Operator => ("{{", "}}"),
            OutlineNodeKind.Constant => ("((", "))"),
            OutlineNodeKind.Term => ("(", ")"),
            _ => ("[", "]"),
        };
    }

    private static string? ClassFor(RenderState state)
    {
        return state switch
        {
            RenderState.True => "brTrue",
            RenderState.False => "brFalse",
            RenderState.Indeterminate => "brUnknown",
            RenderState.Skipped => "brSkipped",
            RenderState.NoData => null,
            _ => null,
        };
    }

    private static void WriteClassDefs(StringBuilder text)
    {
        text.Append("    classDef brTrue fill:#d4edda,stroke:#28a745,color:#155724;\n");
        text.Append("    classDef brFalse fill:#f8d7da,stroke:#dc3545,color:#721c24;\n");
        text.Append("    classDef brUnknown fill:#fff3cd,stroke:#ffc107,color:#856404;\n");
        text.Append("    classDef brSkipped fill:#e9ecef,stroke:#adb5bd,color:#6c757d,stroke-dasharray: 4 3;\n");
    }

    private static string Escape(string label)
    {
        return label.Replace("\"", "#quot;").Replace("\r", " ").Replace("\n", " ");
    }
}
