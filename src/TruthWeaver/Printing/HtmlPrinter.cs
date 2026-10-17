namespace TruthWeaver.Printing;

using System.Text;
using TruthWeaver.Evaluation;

/// <summary>
/// Renders a rule as an HTML fragment: one infix formula in nested <c>span</c> elements, with a class for each node role
/// (<c>tw-operator</c>, <c>tw-term</c>, <c>tw-constant</c>) and, for a rule that was evaluated, a class for each state
/// (<c>tw-true</c>, <c>tw-false</c>, <c>tw-unknown</c>, <c>tw-skipped</c>). The fragment has no <c>script</c> element and no
/// <c>style</c> attribute, so the host page decides every color. It prints from the shared <see cref="RuleRenderTree"/>.
/// </summary>
internal static class HtmlPrinter
{
    /// <summary>The letters of simple-variable mode, as in the equation printer.</summary>
    private const string VariableLetters = "pqrstuvwxyz";

    /// <summary>Prints a rule's structure only.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="options">The HTML options.</param>
    /// <returns>The HTML fragment.</returns>
    public static string Print(OutlineNode root, HtmlOptions options)
    {
        return Print(RuleRenderTree.Build(root, options.OperatorStyle, options.ShowArgumentValues, options.Constants), options);
    }

    /// <summary>Prints a rule's structure, with a state class on each node of one evaluation.</summary>
    /// <param name="root">The rule's outline.</param>
    /// <param name="traceTree">The matching <see cref="Decision.TraceTree"/>.</param>
    /// <param name="options">The HTML options.</param>
    /// <returns>The HTML fragment.</returns>
    public static string Print(OutlineNode root, TraceNode traceTree, HtmlOptions options)
    {
        return Print(
            RuleRenderTree.Build(root, traceTree, options.OperatorStyle, options.ShowArgumentValues, options.Constants),
            options
        );
    }

    private static string Print(RenderNode root, HtmlOptions options)
    {
        Dictionary<string, string> variables = [];
        if (options.SimpleVariables)
        {
            AssignVariables(root, variables);
        }

        StringBuilder html = new("<span class=\"tw-rule\">");
        WriteNode(root, nested: false, variables, html);
        return html.Append("</span>").ToString();
    }

    /// <summary>Gives each distinct term a letter in first-occurrence order, depth first and left to right.</summary>
    private static void AssignVariables(RenderNode node, Dictionary<string, string> variables)
    {
        if (node.Children.Count == 0)
        {
            if (node.Kind == OutlineNodeKind.Term && node.Source is { } source && !variables.ContainsKey(TermKey(source)))
            {
                int index = variables.Count;
                char letter = VariableLetters[index % VariableLetters.Length];
                int round = index / VariableLetters.Length;
                variables[TermKey(source)] = round == 0 ? letter.ToString() : $"{letter}<sub>{round}</sub>";
            }

            return;
        }

        foreach (RenderNode child in node.Children)
        {
            AssignVariables(child, variables);
        }
    }

    private static string TermKey(OutlineNode source)
    {
        // The NUL separator cannot appear in a label, so two different terms never share a key.
        return source.Label + (char)0 + source.ArgumentText;
    }

    /// <summary>Writes one node. A <paramref name="nested"/> infix connective gets parentheses, as in the equation printer.</summary>
    private static void WriteNode(RenderNode node, bool nested, Dictionary<string, string> variables, StringBuilder html)
    {
        string stateClass = StateClass(node.State);
        if (node.Children.Count == 0)
        {
            WriteLeaf(node, stateClass, variables, html);
            return;
        }

        string canonical = node.Source?.Label ?? node.Label;
        string heading = node.Heading ?? node.Label;
        string symbol = $"<span class=\"tw-operator{stateClass}\">{HtmlText.Escape(heading)}</span>";
        bool infix = canonical is "AND" or "OR" or "XOR" or "EQUIVALENT" or "IMPLIES" or "NAND" or "NOR";
        if (infix && nested)
        {
            html.Append('(');
        }

        if (canonical == "NOT" && node.Children.Count == 1)
        {
            // A word operator needs a space before its operand; a symbol does not.
            html.Append(symbol).Append(char.IsLetter(heading[^1]) ? " " : string.Empty);
            WriteNode(node.Children[0], nested: true, variables, html);
        }
        else if (infix)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                html.Append(i == 0 ? string.Empty : $" {symbol} ");
                WriteNode(node.Children[i], nested: true, variables, html);
            }
        }
        else
        {
            // Most operators' Heading equals Source.Label (canonical); a quantifier's Heading additionally carries its
            // "x IN items" head (ADR-0010), which WriteCall must show, so it is passed the styled heading, not canonical.
            WriteCall(node, heading, stateClass, variables, html);
        }

        if (infix && nested)
        {
            html.Append(')');
        }
    }

    /// <summary>
    /// Writes an operator with no infix symbol as <c>Name(operands...)</c>. A label that already ends with its parameters, such
    /// as <c>AtLeast(3)</c>, takes the operands after them: <c>AtLeast(3, a, b)</c>. <paramref name="heading"/> is the
    /// styled head text (e.g. <c>FORALL x IN items</c> for a quantifier, ADR-0010), not the bare canonical op-name.
    /// </summary>
    private static void WriteCall(
        RenderNode node,
        string heading,
        string stateClass,
        Dictionary<string, string> variables,
        StringBuilder html
    )
    {
        bool hasParameters = heading.EndsWith(')');
        string name = hasParameters ? heading[..^1] : heading;
        html.Append("<span class=\"tw-operator").Append(stateClass).Append("\">").Append(HtmlText.Escape(name));
        html.Append(hasParameters ? ", " : "(").Append("</span>");
        for (int i = 0; i < node.Children.Count; i++)
        {
            html.Append(i == 0 ? string.Empty : ", ");
            WriteNode(node.Children[i], nested: false, variables, html);
        }

        html.Append(')');
    }

    private static void WriteLeaf(RenderNode node, string stateClass, Dictionary<string, string> variables, StringBuilder html)
    {
        if (node.Kind == OutlineNodeKind.Constant)
        {
            html.Append("<span class=\"tw-constant")
                .Append(stateClass)
                .Append("\">")
                .Append(HtmlText.Escape(node.Label))
                .Append("</span>");
            return;
        }

        if (node.Source is { } source && variables.TryGetValue(TermKey(source), out string? variable))
        {
            // The letter holds only generated markup, so it is not escaped. The title keeps the full term for a reader to hover.
            string full = source.ArgumentText is { } arguments ? $"{source.Label} ({arguments})" : source.Label;
            html.Append("<span class=\"tw-term")
                .Append(stateClass)
                .Append("\" title=\"")
                .Append(HtmlText.Escape(full))
                .Append("\">")
                .Append(variable)
                .Append("</span>");
            return;
        }

        html.Append("<span class=\"tw-term")
            .Append(stateClass)
            .Append("\">")
            .Append(HtmlText.Escape(node.Label))
            .Append("</span>");
    }

    private static string StateClass(RenderState state)
    {
        return state switch
        {
            RenderState.True => " tw-true",
            RenderState.False => " tw-false",
            RenderState.Indeterminate => " tw-unknown",
            RenderState.Skipped => " tw-skipped",
            _ => string.Empty,
        };
    }
}
