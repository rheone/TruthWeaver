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
/// <c>CompiledRule.Notes</c>. The compiled rule has no definitions: a use of a definition is already its body. Given the
/// <see cref="RuleDefinitions"/> of the source, the tree marks each node that is the expansion of a use
/// (<see cref="RuleTreeNode.Uses"/>) and lists each definition after the rule, with its parameters and its body, under
/// the paths of the document's own tree (<c>$.definitions.name</c>).
/// </remarks>
public sealed class RuleTree
{
    private const string OperandsSegment = ".operands[";

    private readonly JsonObject root;
    private readonly ToolSettings settings;
    private readonly RuleCompiler<StubAssignment> compiler;

    private RuleTree(CompiledRule<StubAssignment> rule, ToolSettings settings, RuleDefinitions? definitions)
    {
        this.Rule = rule;
        this.settings = settings;
        this.compiler = RuleConverter.CreateCompiler(settings);
        this.root = (JsonObject)JsonNode.Parse(rule.PrintJson(includeNotes: true))!;
        if (definitions is { Succeeded: true, HasDefinitions: true } && definitions.PrintJson() is { } document)
        {
            this.Definitions = definitions;
            this.Document = (JsonObject)JsonNode.Parse(document)!;
        }

        List<RuleTreeNode> nodes = [];
        this.Collect(this.root, "$", 0, nodes, null);
        this.CollectDefinitions(nodes);
        this.Nodes = nodes;
    }

    /// <summary>Gets the rule of the tree.</summary>
    public CompiledRule<StubAssignment> Rule { get; }

    /// <summary>
    /// Gets the nodes in tree order: each node, then its operands in order. The nodes of the rule come first, with the root
    /// at index 0; the definitions and their bodies follow.
    /// </summary>
    public IReadOnlyList<RuleTreeNode> Nodes { get; }

    /// <summary>
    /// Gets the definitions and uses of the source of the rule, or <see langword="null"/> when the tree was made without
    /// them or the source has none.
    /// </summary>
    public RuleDefinitions? Definitions { get; }

    /// <summary>Gets the source document in the JSON tree format with its definitions, or <see langword="null"/> without definitions.</summary>
    internal JsonObject? Document { get; }

    /// <summary>Lists the nodes of a compiled rule.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="settings">The settings (limits and operator style), or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <param name="definitions">
    /// The definitions and uses of the text that <paramref name="rule"/> was compiled from, or <see langword="null"/>
    /// (the default) to list the expanded rule alone. They must belong to the same text, or the marks of the uses are wrong.
    /// </param>
    /// <returns>The tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static RuleTree Of(
        CompiledRule<StubAssignment> rule,
        ToolSettings? settings = null,
        RuleDefinitions? definitions = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new RuleTree(rule, settings ?? ToolSettings.Default, definitions);
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

    /// <summary>
    /// Finds a node of a JSON tree by any path of the tree format: keys (<c>.name</c> or <c>['name']</c>) and items
    /// (<c>[0]</c>), such as <c>$.definitions.both.body.operands[1]</c>.
    /// </summary>
    /// <param name="root">The root of the JSON tree.</param>
    /// <param name="path">The path.</param>
    /// <param name="node">The node, when this method returns <see langword="true"/>.</param>
    /// <param name="put">Puts another node in the place of the node, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the path names an object of the tree.</returns>
    internal static bool TryLocatePath(
        JsonObject root,
        string path,
        [NotNullWhen(true)] out JsonObject? node,
        [NotNullWhen(true)] out Action<JsonObject>? put
    )
    {
        node = null;
        put = null;
        if (!path.StartsWith('$'))
        {
            return false;
        }

        JsonNode current = root;
        Action<JsonObject> place = replacement => ReplaceMembers(root, replacement);
        int position = 1;
        while (position < path.Length)
        {
            if (!TryStep(path, ref position, current, out JsonNode? child, out Action<JsonObject>? childPlace))
            {
                return false;
            }

            current = child;
            place = childPlace;
        }

        if (current is not JsonObject found)
        {
            return false;
        }

        node = found;
        put = place;
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

    /// <summary>Puts the members of one object in place of the members of another, so the target stays the same instance.</summary>
    /// <param name="target">The object to change.</param>
    /// <param name="replacement">The object whose members move into <paramref name="target"/>.</param>
    internal static void ReplaceMembers(JsonObject target, JsonObject replacement)
    {
        target.Clear();
        foreach ((string key, JsonNode? value) in replacement.ToList())
        {
            replacement.Remove(key);
            target[key] = value;
        }
    }

    /// <summary>Gets a copy of the JSON tree of the rule, with its notes, for an edit.</summary>
    /// <returns>The copy.</returns>
    internal JsonObject CopyJson()
    {
        return (JsonObject)this.root.DeepClone();
    }

    /// <summary>Gets a copy of the source document with its definitions, for an edit, or <see langword="null"/> without definitions.</summary>
    /// <returns>The copy.</returns>
    internal JsonObject? CopyDocument()
    {
        return (JsonObject?)this.Document?.DeepClone();
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

    // Reads one step of a path (".name", "['name']" or "[0]") and moves to that child.
    private static bool TryStep(
        string path,
        ref int position,
        JsonNode current,
        [NotNullWhen(true)] out JsonNode? child,
        [NotNullWhen(true)] out Action<JsonObject>? place
    )
    {
        child = null;
        place = null;
        string? key = null;
        if (path[position] == '.')
        {
            int start = ++position;
            while (position < path.Length && (char.IsAsciiLetterOrDigit(path[position]) || path[position] == '_'))
            {
                position++;
            }

            key = path[start..position];
        }
        else if (string.CompareOrdinal(path, position, "['", 0, 2) == 0)
        {
            StringBuilder name = new();
            position += 2;
            while (position < path.Length && string.CompareOrdinal(path, position, "']", 0, 2) != 0)
            {
                // TreePath writes a quote inside a bracketed key as a backslash and the quote.
                bool escaped = path[position] == '\\' && position + 1 < path.Length && path[position + 1] == '\'';
                position += escaped ? 1 : 0;
                name.Append(path[position++]);
            }

            position += 2;
            key = name.ToString();
        }
        else if (path[position] == '[')
        {
            int close = path.IndexOf(']', position);
            if (
                close < 0
                || !int.TryParse(
                    path.AsSpan(position + 1, close - position - 1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int index
                )
                || current is not JsonArray items
                || index >= items.Count
                || items[index] is not { } item
            )
            {
                return false;
            }

            position = close + 1;
            child = item;
            place = replacement => items[index] = replacement;
            return true;
        }

        if (string.IsNullOrEmpty(key) || current is not JsonObject owner || owner[key] is not { } value)
        {
            return false;
        }

        child = value;
        place = replacement => owner[key] = replacement;
        return true;
    }

    private void Collect(JsonObject tree, string path, int depth, List<RuleTreeNode> nodes, IReadOnlySet<string>? parameters)
    {
        string? note = tree["note"] is JsonValue noteValue && noteValue.TryGetValue(out string? text) ? text : null;
        if (parameters is not null && this.CollectSource(tree, path, depth, nodes, parameters, note))
        {
            return;
        }

        if (tree.ContainsKey("const"))
        {
            nodes.Add(
                this.MarkUses(
                    new RuleTreeNode(
                        path,
                        depth,
                        RuleTreeNodeKind.Constant,
                        ConstantLabel(tree["const"]),
                        0,
                        [new("const", tree["const"]?.ToJsonString() ?? "null")],
                        note
                    ),
                    parameters
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

            nodes.Add(
                this.MarkUses(
                    new RuleTreeNode(path, depth, RuleTreeNodeKind.Term, this.TermLabel(tree), 0, arguments, note),
                    parameters
                )
            );
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
            this.MarkUses(
                new RuleTreeNode(path, depth, RuleTreeNodeKind.Operator, OperatorLabel(tree), operands.Count, literals, note),
                parameters
            )
        );
        for (int i = 0; i < operands.Count; i++)
        {
            if (operands[i] is JsonObject operand)
            {
                this.Collect(operand, OperandPath(path, i), depth + 1, nodes, parameters);
            }
        }
    }

    // Lists a use or a parameter inside a definition body; false for any other node, which lists as in the rule.
    private bool CollectSource(
        JsonObject tree,
        string path,
        int depth,
        List<RuleTreeNode> nodes,
        IReadOnlySet<string> parameters,
        string? note
    )
    {
        if (DefinitionsPrinter.IsUse(tree))
        {
            JsonArray arguments = tree["operands"] as JsonArray ?? [];
            nodes.Add(
                new RuleTreeNode(path, depth, RuleTreeNodeKind.Use, "use " + this.UseText(tree), arguments.Count, [], note)
            );
            for (int i = 0; i < arguments.Count; i++)
            {
                if (arguments[i] is JsonObject argument)
                {
                    this.Collect(argument, OperandPath(path, i), depth + 1, nodes, parameters);
                }
            }

            return true;
        }

        // A bare predicate with no arguments that names a parameter is the parameter, as the parser reads it.
        if (
            tree["predicate"] is JsonValue name
            && !tree.ContainsKey("args")
            && name.TryGetValue(out string? parameter)
            && parameters.Contains(parameter)
        )
        {
            nodes.Add(new RuleTreeNode(path, depth, RuleTreeNodeKind.Parameter, $"{parameter} (parameter)", 0, [], note));
            return true;
        }

        return false;
    }

    // Lists each definition after the rule: its name and parameters, then its body.
    private void CollectDefinitions(List<RuleTreeNode> nodes)
    {
        if (this.Definitions is null || this.Document is null)
        {
            return;
        }

        foreach (DefinitionDeclaration definition in this.Definitions.Definitions)
        {
            if (
                !TryLocatePath(this.Document, definition.Path, out JsonObject? entry, out _)
                || entry["body"] is not JsonObject body
            )
            {
                continue;
            }

            string label =
                definition.Parameters.Count == 0
                    ? $"let {definition.Name}"
                    : $"let {definition.Name}({string.Join(", ", definition.Parameters)})";
            nodes.Add(new RuleTreeNode(definition.Path, 0, RuleTreeNodeKind.Definition, label, 1, [], null));
            this.Collect(
                body,
                definition.Path + ".body",
                1,
                nodes,
                new HashSet<string>(definition.Parameters, StringComparer.OrdinalIgnoreCase)
            );
        }
    }

    // Marks a node of the rule that is the expansion of one or more uses: their text goes in front of the label.
    private RuleTreeNode MarkUses(RuleTreeNode node, IReadOnlySet<string>? parameters)
    {
        if (parameters is not null || this.Definitions is null || this.Document is null)
        {
            return node;
        }

        List<string> uses = [];
        foreach (string source in this.Definitions.SourcesOf(node.Path))
        {
            if (TryLocatePath(this.Document, source, out JsonObject? sourceNode, out _) && DefinitionsPrinter.IsUse(sourceNode))
            {
                uses.Add(this.UseText(sourceNode));
            }
        }

        return uses.Count == 0 ? node : node with { Label = $"use {string.Join(" = ", uses)} = {node.Label}", Uses = uses };
    }

    // The rule text of a use with its arguments; its name alone when an argument does not print.
    private string UseText(JsonObject use)
    {
        return DefinitionsPrinter.PrintNode(this.Document!, use, this.settings) ?? use["name"]?.ToString() ?? "?";
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
