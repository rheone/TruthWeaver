namespace TruthWeaver.Tool.Core;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;

/// <summary>
/// The structural edits of a rule: replace an operator, wrap a node in <c>NOT</c>, negate it, insert an operand, delete a
/// node, swap two operands, replace a node with an expression (a palette entry or pasted text) and change a literal. Each
/// edit names a node by its <see cref="RuleTreeNode.Path"/>, changes a copy of the JSON tree of the rule, and compiles the
/// result. An edit whose result does not compile is refused with the diagnostics of the compiler; the rule never changes.
/// </summary>
/// <remarks>
/// The editor compiles permissively, as <see cref="RuleDocument"/> does, with the limits of the settings. The notes of the
/// nodes that an edit keeps stay on them. It has no state beyond the rule: each edit gives a new rule, and
/// <see cref="RuleDocument.ReplaceRule"/> puts it into the document as one undo step.
/// </remarks>
public sealed class RuleTreeEditor
{
    // The operators whose operands are counted against a threshold "k", or a range "min" to "max".
    private static readonly HashSet<string> Thresholds =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "atLeast",
        "atMost",
        "greaterThan",
        "lessThan",
        "exactly",
    ];

    private static readonly HashSet<string> Quantifiers = [with(StringComparer.OrdinalIgnoreCase), "forall", "exists"];

    // The operators that need two or more operands. When a delete leaves one of them with one operand, the operand takes
    // its place, so deleting one side of "a AND b" leaves the other side.
    private static readonly HashSet<string> MultiOperand =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "and",
        "or",
        "xor",
        "equivalent",
        "implies",
        "nand",
        "nor",
        "parity",
        "any",
        "all",
        "none",
        "exactlyOne",
        "between",
        "coalesce",
    ];

    // The operators whose negation is another operator under Strong K3: NAND is NOT AND, NOR is NOT OR, and EQUIVALENT
    // is NOT XOR, for every value of the operands.
    private static readonly Dictionary<string, string> Duals = new(StringComparer.OrdinalIgnoreCase)
    {
        ["and"] = "nand",
        ["nand"] = "and",
        ["or"] = "nor",
        ["nor"] = "or",
        ["xor"] = "equivalent",
        ["equivalent"] = "xor",
    };

    private readonly ToolSettings settings;
    private readonly RuleCompiler<StubAssignment> compiler;

    /// <summary>Initializes a new instance of the <see cref="RuleTreeEditor"/> class.</summary>
    /// <param name="rule">The rule to edit.</param>
    /// <param name="settings">The settings (limits and operator style), or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public RuleTreeEditor(CompiledRule<StubAssignment> rule, ToolSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        this.settings = settings ?? ToolSettings.Default;
        this.compiler = RuleConverter.CreateCompiler(this.settings);
        this.Tree = RuleTree.Of(rule, this.settings);
    }

    /// <summary>Gets the tree of the rule that the edits start from.</summary>
    public RuleTree Tree { get; }

    /// <summary>
    /// Replaces the operator of an operator node and keeps its operands. A count (<c>k</c>) or a range (<c>min</c> and
    /// <c>max</c>) that the new operator needs and the node does not have starts at 1 (and <c>max</c> at the number of
    /// operands); a member that the new operator does not take is removed.
    /// </summary>
    /// <param name="path">The path of the operator node.</param>
    /// <param name="op">The new operator, by its name in the JSON tree format, such as <c>or</c> or <c>atLeast</c>.</param>
    /// <returns>The edited rule, or the reason it was refused (for example <c>NOT</c> with two operands).</returns>
    public RuleTreeEdit ReplaceOperator(string path, string op)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(op);

        return this.Edit(
            path,
            (_, node, _, _) =>
            {
                if (!node.ContainsKey("op"))
                {
                    return "the node is not an operator. Replace it or wrap it instead.";
                }

                int operands = (node["operands"] as JsonArray)?.Count ?? 0;
                node["op"] = op;
                Keep(node, "k", Thresholds.Contains(op), 1);
                Keep(node, "min", op.Equals("between", StringComparison.OrdinalIgnoreCase), 1);
                Keep(node, "max", op.Equals("between", StringComparison.OrdinalIgnoreCase), operands);
                if (!Quantifiers.Contains(op))
                {
                    node.Remove("var");
                    node.Remove("in");
                }

                return null;
            },
            path,
            $"Replaced the operator with {op}."
        );
    }

    /// <summary>Wraps a node in <c>NOT</c>. The note of the node stays on the node.</summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The edited rule; the path selects the new <c>NOT</c>.</returns>
    public RuleTreeEdit WrapInNot(string path)
    {
        return this.Edit(
            path,
            (root, node, parent, index) => Put(root, parent, index, Not(node)),
            path,
            "Wrapped the node in NOT."
        );
    }

    /// <summary>
    /// Negates a node in the simplest exact way: a <c>NOT</c> gives its operand, <c>True</c> and <c>False</c> swap,
    /// <c>AND</c> and <c>NAND</c>, <c>OR</c> and <c>NOR</c>, and <c>XOR</c> and <c>EQUIVALENT</c> swap (each pair are
    /// negations of each other under Strong K3), and any other node is wrapped in <c>NOT</c>. <c>Unknown</c> is its own
    /// negation, so negating it is refused.
    /// </summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The edited rule, or the reason it was refused.</returns>
    public RuleTreeEdit Negate(string path)
    {
        return this.Edit(
            path,
            (root, node, parent, index) =>
            {
                string? op = node["op"]?.GetValue<string>();
                if (string.Equals(op, "not", StringComparison.OrdinalIgnoreCase))
                {
                    return node["operands"] is JsonArray { Count: 1 } operands && operands[0] is JsonObject operand
                        ? Put(root, parent, index, (JsonObject)operand.DeepClone())
                        : "the NOT has no single operand.";
                }

                if (op is not null && Duals.TryGetValue(op, out string? dual))
                {
                    node["op"] = dual;
                    return null;
                }

                if (node.ContainsKey("const"))
                {
                    if (node["const"] is not JsonValue value || !value.TryGetValue(out bool flag))
                    {
                        return "NOT Unknown is Unknown, so there is nothing to change.";
                    }

                    node["const"] = !flag;
                    return null;
                }

                return Put(root, parent, index, Not(node));
            },
            path,
            "Negated the node."
        );
    }

    /// <summary>Inserts an operand into an operator node.</summary>
    /// <param name="path">The path of the operator node.</param>
    /// <param name="expression">The operand as rule text, JSON or YAML (the content selects the format).</param>
    /// <param name="index">The index of the new operand, or <see langword="null"/> to add it after the last operand.</param>
    /// <returns>The edited rule; the path selects the new operand. An expression that does not compile gives its diagnostics.</returns>
    public RuleTreeEdit InsertOperand(string path, string expression, int? index = null)
    {
        if (!this.TryParse(expression, out JsonObject? operand, out RuleTreeEdit? refused))
        {
            return refused;
        }

        string selected = path;
        RuleTreeEdit edit = this.Edit(
            path,
            (_, node, _, _) =>
            {
                if (node["operands"] is not JsonArray operands)
                {
                    return "the node is not an operator, so it has no operands. Select an operator.";
                }

                int position = Math.Clamp(index ?? operands.Count, 0, operands.Count);
                operands.Insert(position, operand);
                selected = RuleTree.OperandPath(path, position);
                return null;
            },
            path,
            "Inserted an operand."
        );
        return edit.Succeeded ? edit with { Path = selected } : edit;
    }

    /// <summary>
    /// Deletes a node from the operands of its parent. When that leaves an operator that needs two operands (such as
    /// <c>AND</c>) with one, the remaining operand takes the place of the operator. The root cannot be deleted.
    /// </summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The edited rule; the path selects the parent, or the operand that took its place.</returns>
    public RuleTreeEdit Delete(string path)
    {
        string? parentPath = RuleTree.ParentPath(path ?? string.Empty);
        return this.Edit(
            path!,
            (root, _, parent, index) =>
            {
                if (parent is null || parentPath is null)
                {
                    return "the root cannot be deleted. Replace it instead.";
                }

                parent.RemoveAt(index);
                if (
                    parent.Count == 1
                    && RuleTree.TryLocate(root, parentPath, out JsonObject? owner, out JsonArray? above, out int at)
                    && owner["op"]?.GetValue<string>() is { } op
                    && MultiOperand.Contains(op)
                )
                {
                    JsonObject remaining = (JsonObject)parent[0]!.DeepClone();
                    return Put(root, above, at, remaining);
                }

                return null;
            },
            parentPath,
            "Deleted the node."
        );
    }

    /// <summary>Swaps a node with its neighbor in the operands of its parent.</summary>
    /// <param name="path">The path of the node.</param>
    /// <param name="direction">1 to swap with the next operand; -1 to swap with the previous one.</param>
    /// <returns>The edited rule; the path follows the node to its new place.</returns>
    public RuleTreeEdit Swap(string path, int direction)
    {
        string? parentPath = RuleTree.ParentPath(path ?? string.Empty);
        string? selected = null;
        RuleTreeEdit edit = this.Edit(
            path!,
            (_, _, parent, index) =>
            {
                int other = index + Math.Sign(direction);
                if (parent is null || parentPath is null || direction == 0 || other < 0 || other >= parent.Count)
                {
                    return "there is no operand on that side to swap with.";
                }

                JsonNode? first = parent[index]!.DeepClone();
                JsonNode? second = parent[other]!.DeepClone();
                parent[index] = second;
                parent[other] = first;
                selected = RuleTree.OperandPath(parentPath, other);
                return null;
            },
            path,
            "Swapped the operands."
        );
        return edit.Succeeded ? edit with { Path = selected } : edit;
    }

    /// <summary>Replaces a node with an expression: a palette entry, a constant, or pasted text.</summary>
    /// <param name="path">The path of the node; <c>$</c> replaces the whole rule.</param>
    /// <param name="expression">The expression as rule text, JSON or YAML (the content selects the format).</param>
    /// <returns>The edited rule. An expression that does not compile gives its diagnostics and changes nothing.</returns>
    public RuleTreeEdit Replace(string path, string expression)
    {
        if (!this.TryParse(expression, out JsonObject? replacement, out RuleTreeEdit? refused))
        {
            return refused;
        }

        return this.Edit(path, (root, _, parent, index) => Put(root, parent, index, replacement), path, "Replaced the node.");
    }

    /// <summary>
    /// Changes a literal value of a node (see <see cref="RuleTreeNode.Literals"/>): <c>const</c> of a constant
    /// (<c>true</c>, <c>false</c>, <c>unknown</c> or a letter <c>T</c>, <c>F</c>, <c>U</c>), an argument of a term, or the
    /// <c>k</c>, <c>min</c> or <c>max</c> of an operator. The value is JSON, such as <c>"EU"</c>, <c>18</c> or
    /// <c>["a", "b"]</c>; text that is not JSON is taken as a string. For a term, a new name adds the argument and an
    /// empty value removes it.
    /// </summary>
    /// <param name="path">The path of the node.</param>
    /// <param name="name">The name of the literal.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The edited rule, or the reason it was refused (for example a value of the wrong kind).</returns>
    public RuleTreeEdit ChangeLiteral(string path, string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        return this.Edit(
            path,
            (_, node, _, _) =>
            {
                if (node.ContainsKey("const"))
                {
                    if (name != "const")
                    {
                        return "a constant has only the literal 'const'.";
                    }

                    node["const"] = value.Trim().ToUpperInvariant() switch
                    {
                        "TRUE" or "T" => true,
                        "FALSE" or "F" => false,
                        _ => ParseValue(value),
                    };
                    return null;
                }

                if (node.ContainsKey("predicate"))
                {
                    JsonObject args = node["args"] as JsonObject ?? [];
                    node["args"] = args;
                    if (value.Length == 0)
                    {
                        return args.Remove(name) ? null : $"the term has no argument '{name}'.";
                    }

                    args[name] = ParseValue(value);
                    return null;
                }

                if (!node.ContainsKey(name) || name is not ("k" or "min" or "max"))
                {
                    return $"the operator has no literal '{name}'.";
                }

                node[name] = ParseValue(value);
                return null;
            },
            path,
            $"Changed {name}."
        );
    }

    /// <summary>Reads a value as JSON when it reads as JSON; otherwise the text is a string.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The JSON value.</returns>
    internal static JsonNode? ParseValue(string value)
    {
        string trimmed = value.Trim();
        if (
            trimmed.Length > 0
            && (trimmed[0] is '"' or '[' or '{' or '-' || char.IsAsciiDigit(trimmed[0]) || trimmed is "true" or "false")
        )
        {
            try
            {
                return JsonNode.Parse(trimmed);
            }
            catch (JsonException)
            {
                // Not JSON after all: the text is the value.
            }
        }

        return JsonValue.Create(value);
    }

    private static JsonObject Not(JsonObject node)
    {
        return new JsonObject { ["op"] = "not", ["operands"] = new JsonArray(node.DeepClone()) };
    }

    // Puts a node in place of the node at a position: in its parent's operands, or as the root.
    private static string? Put(JsonObject root, JsonArray? parent, int index, JsonObject replacement)
    {
        if (parent is not null)
        {
            parent[index] = replacement;
            return null;
        }

        // The root object stays the same instance, so its members are replaced in place.
        root.Clear();
        foreach ((string key, JsonNode? value) in replacement.ToList())
        {
            replacement.Remove(key);
            root[key] = value;
        }

        return null;
    }

    // Keeps a member that an operator needs (adding it with a start value) and removes it when the operator does not take it.
    private static void Keep(JsonObject node, string member, bool needed, int start)
    {
        if (!needed)
        {
            node.Remove(member);
        }
        else if (!node.ContainsKey(member))
        {
            node[member] = start;
        }
    }

    private static string FirstError(EquatableArray<Diagnostic> diagnostics)
    {
        Diagnostic? first =
            diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error)
            ?? (diagnostics.Count > 0 ? diagnostics[0] : null);
        return first is null ? "it does not compile." : $"{first.Code}: {first.Message}";
    }

    // Compiles an expression in the format its content shows and gives its JSON tree with the notes.
    private bool TryParse(
        string expression,
        [NotNullWhen(true)] out JsonObject? tree,
        [NotNullWhen(false)] out RuleTreeEdit? refused
    )
    {
        ArgumentNullException.ThrowIfNull(expression);

        tree = null;
        refused = null;
        RuleFormat format = FormatDetector.Detect(expression).Format;
        CompilationResult<StubAssignment> compiled = this.compiler.CompileAs(expression, format);
        if (compiled.Rule is null)
        {
            refused = RuleTreeEdit.Refused($"Not a valid expression: {FirstError(compiled.Diagnostics)}", compiled.Diagnostics);
            return false;
        }

        tree = (JsonObject)JsonNode.Parse(compiled.Rule.PrintJson(includeNotes: true))!;
        return true;
    }

    // Applies a change to a copy of the tree and compiles the result. The change returns null when it applied, or the reason
    // it cannot apply.
    private RuleTreeEdit Edit(
        string path,
        Func<JsonObject, JsonObject, JsonArray?, int, string?> change,
        string? selected,
        string done
    )
    {
        ArgumentNullException.ThrowIfNull(path);

        JsonObject root = this.Tree.CopyJson();
        if (!RuleTree.TryLocate(root, path, out JsonObject? node, out JsonArray? parent, out int index))
        {
            return RuleTreeEdit.Refused($"Not applied: the path '{path}' names no node of the rule.");
        }

        if (change(root, node, parent, index) is { } problem)
        {
            return RuleTreeEdit.Refused("Not applied: " + problem);
        }

        CompilationResult<StubAssignment> compiled = this.compiler.CompileAs(root.ToJsonString(), RuleFormat.Json);
        if (compiled.Rule is null)
        {
            return RuleTreeEdit.Refused(
                $"Not applied: the edit would make an invalid rule. {FirstError(compiled.Diagnostics)}",
                compiled.Diagnostics
            );
        }

        string? target =
            selected is not null && RuleTree.Of(compiled.Rule, this.settings).Find(selected) is not null ? selected : "$";
        return new RuleTreeEdit(compiled.Rule, target, done, compiled.Diagnostics);
    }
}
