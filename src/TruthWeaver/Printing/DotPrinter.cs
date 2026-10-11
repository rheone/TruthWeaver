namespace TruthWeaver.Printing;

using System.Text;
using TruthWeaver.Evaluation;

/// <summary>
/// Renders a rule as a Graphviz DOT <c>digraph</c>: one node for each operator, term and constant, and one edge from each
/// operator to each operand. The node shapes follow the Mermaid shape rule: a hexagon for an operator, a rounded box for a
/// term and a circle for a constant. Node ids are <c>n0</c>, <c>n1</c> and so on, in depth-first order, so the same rule always
/// gets the same ids. It prints from the shared <see cref="RuleRenderTree"/>.
/// </summary>
internal static class DotPrinter
{
    /// <summary>Prints a rule's structure only.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="options">The DOT options.</param>
    /// <returns>The DOT text.</returns>
    public static string Print(OutlineNode root, DotOptions options)
    {
        return Print(RuleRenderTree.Build(root, options.OperatorStyle, options.ShowArgumentValues, options.Constants), options);
    }

    /// <summary>Prints a rule's structure, with a fill color on each node of one evaluation.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="traceTree">The matching <see cref="Decision.TraceTree"/>.</param>
    /// <param name="options">The DOT options.</param>
    /// <returns>The DOT text.</returns>
    public static string Print(OutlineNode root, TraceNode traceTree, DotOptions options)
    {
        return Print(
            RuleRenderTree.Build(root, traceTree, options.OperatorStyle, options.ShowArgumentValues, options.Constants),
            options
        );
    }

    private static string Print(RenderNode root, DotOptions options)
    {
        StringBuilder dot = new();
        dot.Append("digraph TruthWeaver {\n");
        dot.Append("    rankdir=").Append(RankDirection(options.Direction)).Append(";\n");
        int nextId = 0;
        WriteNode(root, options, dot, ref nextId);
        return dot.Append("}\n").ToString();
    }

    private static void WriteNode(RenderNode node, DotOptions options, StringBuilder dot, ref int nextId)
    {
        string id = $"n{nextId++}";
        (string shape, string style) = options.NodeShapes ? ShapeFor(node.Kind) : ("box", string.Empty);
        string? fill = FillFor(node.State);
        if (fill is not null)
        {
            style = style.Length == 0 ? "filled" : style + ",filled";
        }

        dot.Append("    ").Append(id).Append(" [label=\"").Append(Escape(node.Label)).Append("\", shape=").Append(shape);
        if (style.Length > 0)
        {
            dot.Append(", style=\"").Append(style).Append('"');
        }

        if (fill is not null)
        {
            dot.Append(", fillcolor=\"").Append(fill).Append('"');
        }

        dot.Append("];\n");

        foreach (RenderNode child in node.Children)
        {
            int childId = nextId;
            WriteNode(child, options, dot, ref nextId);
            dot.Append("    ").Append(id).Append(" -> n").Append(childId).Append(";\n");
        }
    }

    /// <summary>The DOT shape and style for a node role, as in the Mermaid shape rule.</summary>
    private static (string Shape, string Style) ShapeFor(OutlineNodeKind kind)
    {
        return kind switch
        {
            OutlineNodeKind.Operator => ("hexagon", string.Empty),
            OutlineNodeKind.Constant => ("circle", string.Empty),
            OutlineNodeKind.Term => ("box", "rounded"),
            _ => ("box", string.Empty),
        };
    }

    private static string? FillFor(RenderState state)
    {
        return state switch
        {
            RenderState.True => "#c8e6c9",
            RenderState.False => "#ffcdd2",
            RenderState.Indeterminate => "#fff9c4",
            RenderState.Skipped => "#eeeeee",
            _ => null,
        };
    }

    private static string RankDirection(MermaidDirection direction)
    {
        return direction switch
        {
            MermaidDirection.TopDown => "TB",
            MermaidDirection.LeftRight => "LR",
            MermaidDirection.BottomTop => "BT",
            MermaidDirection.RightLeft => "RL",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unhandled direction."),
        };
    }

    /// <summary>
    /// Escapes text for a double-quoted DOT string. A backslash and a double quote get a backslash. A line break becomes the
    /// DOT escape <c>\n</c> so it cannot end the statement, and any other control character becomes a space.
    /// </summary>
    private static string Escape(string label)
    {
        StringBuilder builder = new(label.Length + 4);
        foreach (char c in label)
        {
            builder.Append(
                c switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\n' => "\\n",
                    < ' ' or '\u007F' => " ",
                    _ => c.ToString(),
                }
            );
        }

        return builder.ToString();
    }
}
