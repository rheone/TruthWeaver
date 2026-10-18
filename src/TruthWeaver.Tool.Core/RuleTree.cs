namespace TruthWeaver.Tool.Core;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Yaml;

/// <summary>
/// The nodes of a compiled rule as a list in tree order (each node before its operands), with the path, the kind and a
/// short label of each node. A front end lists the tree with it and names a node by its path for
/// <see cref="RuleTreeEditor"/>.
/// </summary>
/// <remarks>
/// The tree is read from the JSON tree format of the rule (<c>PrintJson</c>), so a path is the path of the node in that
/// format: <c>$</c> is the root and <c>$.operands[1]</c> is its second operand, the same form as the keys of
/// <c>CompiledRule.Notes</c>. The JSON tree has no definitions: a <c>use</c> of a definition is already its body.
/// </remarks>
public sealed class RuleTree
{
    private const string OperandsSegment = ".operands[";

    private readonly JsonObject root;
    private readonly ToolSettings settings;
    private readonly RuleCompiler<StubAssignment> compiler;

    private RuleTree(CompiledRule<StubAssignment> rule, ToolSettings settings)
    {
        this.Rule = rule;
        this.settings = settings;
        this.compiler = RuleConverter.CreateCompiler(settings);
        this.root = (JsonObject)JsonNode.Parse(rule.PrintJson(includeNotes: true))!;
        List<RuleTreeNode> nodes = [];
        this.Collect(this.root, "$", 0, nodes);
        this.Nodes = nodes;
    }

    /// <summary>Gets the rule of the tree.</summary>
    public CompiledRule<StubAssignment> Rule { get; }

    /// <summary>Gets the nodes in tree order: each node, then its operands in order.</summary>
    public IReadOnlyList<RuleTreeNode> Nodes { get; }

    /// <summary>Lists the nodes of a compiled rule.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="settings">The settings (limits and operator style), or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static RuleTree Of(CompiledRule<StubAssignment> rule, ToolSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new RuleTree(rule, settings ?? ToolSettings.Default);
    }

    /// <summary>Finds the node at a path.</summary>
    /// <param name="path">The path, such as <c>$.operands[0]</c>.</param>
    /// <returns>The node, or <see langword="null"/> when the path names no node.</returns>
    public RuleTreeNode? Find(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return this.Nodes.FirstOrDefault(n => string.Equals(n.Path, path, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the rule text of the node at a path, with its notes, in the operator style of the settings: the text to copy.
    /// The text compiles back to the same subtree.
    /// </summary>
    /// <param name="path">The path of the node; <c>$</c> for the whole rule.</param>
    /// <returns>The text, or the diagnostics when the path names no node or the node does not compile on its own.</returns>
    public ToolResult<string> TextOf(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!TryLocate(this.root, path, out JsonObject? node, out _, out _))
        {
            return ToolResults.Failure<string>(
                ToolDiagnosticCodes.PathNotFound,
                $"The path '{path}' names no node of the rule."
            );
        }

        if (path == "$")
        {
            return ToolResults.Success(PrintText(this.Rule, this.settings));
        }

        CompilationResult<StubAssignment> compiled = this.compiler.CompileAs(node.ToJsonString(), RuleFormat.Json);
        return compiled.Rule is { } rule
            ? ToolResults.Success(PrintText(rule, this.settings))
            : ToolResults.Failure<string>(compiled.Diagnostics);
    }

    /// <summary>Finds a node of a JSON tree by its path.</summary>
    /// <param name="root">The root of the JSON tree.</param>
    /// <param name="path">The path, such as <c>$.operands[0].operands[1]</c>.</param>
    /// <param name="node">The node, when this method returns <see langword="true"/>.</param>
    /// <param name="parent">The <c>operands</c> array that holds the node, or <see langword="null"/> for the root.</param>
    /// <param name="index">The index of the node in <paramref name="parent"/>, or -1 for the root.</param>
    /// <returns><see langword="true"/> when the path names a node.</returns>
    internal static bool TryLocate(
        JsonObject root,
        string path,
        [NotNullWhen(true)] out JsonObject? node,
        out JsonArray? parent,
        out int index
    )
    {
        node = null;
        parent = null;
        index = -1;
        if (!path.StartsWith('$'))
        {
            return false;
        }

        JsonObject current = root;
        int position = 1;
        while (position < path.Length)
        {
            if (string.CompareOrdinal(path, position, OperandsSegment, 0, OperandsSegment.Length) != 0)
            {
                return false;
            }

            position += OperandsSegment.Length;
            int close = path.IndexOf(']', position);
            if (
                close < 0
                || !int.TryParse(
                    path.AsSpan(position, close - position),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int operand
                )
                || current["operands"] is not JsonArray operands
                || operand >= operands.Count
                || operands[operand] is not JsonObject child
            )
            {
                return false;
            }

            parent = operands;
            index = operand;
            current = child;
            position = close + 1;
        }

        node = current;
        return true;
    }

    /// <summary>Gets the path of an operand of a node.</summary>
    /// <param name="path">The path of the node.</param>
    /// <param name="index">The index of the operand.</param>
    /// <returns>The path of the operand.</returns>
    internal static string OperandPath(string path, int index)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{path}{OperandsSegment}{index}]");
    }

    /// <summary>Gets the path of the parent of a node, or <see langword="null"/> for the root.</summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The path of the parent.</returns>
    internal static string? ParentPath(string path)
    {
        int last = path.LastIndexOf(OperandsSegment, StringComparison.Ordinal);
        return last < 0 ? null : path[..last];
    }

    /// <summary>Prints a rule as rule text with its notes, in the operator style of the settings when that style reads back.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The text.</returns>
    internal static string PrintText(CompiledRule<StubAssignment> rule, ToolSettings settings)
    {
        // The C-style operators have no rule text that reads back; the word style always does.
        RuleTextOptions options = new() { OperatorStyle = settings.OperatorStyle, IncludeNotes = true };
        return rule.TryPrintRuleText(options, out string? text)
            ? text
            : rule.PrintRuleText(options with { OperatorStyle = OperatorStyle.Word });
    }

    /// <summary>Gets a copy of the JSON tree of the rule, with its notes, for an edit.</summary>
    /// <returns>The copy.</returns>
    internal JsonObject CopyJson()
    {
        return (JsonObject)this.root.DeepClone();
    }

    // The label of an operator: its name, then the members that are not operands, such as "atLeast k=2".
    private static string OperatorLabel(JsonObject tree)
    {
        StringBuilder label = new(tree["op"]?.ToString() ?? "?");
        foreach (string member in (string[])["k", "min", "max"])
        {
            if (tree[member] is { } value)
            {
                label.Append(' ').Append(member).Append('=').Append(value.ToJsonString());
            }
        }

        if (tree["var"] is { } variable && tree["in"] is { } collection)
        {
            label.Append(' ').Append(variable).Append(" in ").Append(collection);
        }

        return label.ToString();
    }

    private static string ConstantLabel(JsonNode? value)
    {
        if (value is not JsonValue v || !v.TryGetValue(out bool flag))
        {
            return "Unknown";
        }

        return flag ? "True" : "False";
    }

    private void Collect(JsonObject tree, string path, int depth, List<RuleTreeNode> nodes)
    {
        string? note = tree["note"] is JsonValue noteValue && noteValue.TryGetValue(out string? text) ? text : null;
        if (tree.ContainsKey("const"))
        {
            nodes.Add(
                new RuleTreeNode(
                    path,
                    depth,
                    RuleTreeNodeKind.Constant,
                    ConstantLabel(tree["const"]),
                    0,
                    [new("const", tree["const"]?.ToJsonString() ?? "null")],
                    note
                )
            );
            return;
        }

        if (tree.ContainsKey("predicate"))
        {
            List<KeyValuePair<string, string>> arguments = [];
            if (tree["args"] is JsonObject args)
            {
                arguments.AddRange(
                    args.Select(a => new KeyValuePair<string, string>(a.Key, a.Value?.ToJsonString() ?? "null"))
                );
            }

            nodes.Add(new RuleTreeNode(path, depth, RuleTreeNodeKind.Term, this.TermLabel(tree), 0, arguments, note));
            return;
        }

        JsonArray operands = tree["operands"] as JsonArray ?? [];
        List<KeyValuePair<string, string>> literals =
        [
            .. ((string[])["k", "min", "max"])
                .Where(tree.ContainsKey)
                .Select(m => new KeyValuePair<string, string>(m, tree[m]?.ToJsonString() ?? "null")),
        ];
        nodes.Add(
            new RuleTreeNode(path, depth, RuleTreeNodeKind.Operator, OperatorLabel(tree), operands.Count, literals, note)
        );
        for (int i = 0; i < operands.Count; i++)
        {
            if (operands[i] is JsonObject operand)
            {
                this.Collect(operand, OperandPath(path, i), depth + 1, nodes);
            }
        }
    }

    // The canonical text of the term alone, as the term list of the assignment shows it; the bare predicate name when the
    // term does not compile on its own.
    private string TermLabel(JsonObject tree)
    {
        JsonObject term = new()
        {
            ["predicate"] = tree["predicate"]?.DeepClone(),
            ["args"] = tree["args"]?.DeepClone() ?? new JsonObject(),
        };
        return this.compiler.CompileAs(term.ToJsonString(), RuleFormat.Json).Rule is { } compiled
            ? compiled.CanonicalText
            : tree["predicate"]?.ToString() ?? "?";
    }
}
