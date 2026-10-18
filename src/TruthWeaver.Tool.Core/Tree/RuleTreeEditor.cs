namespace TruthWeaver.Tool.Core.Tree;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Yaml;

/// <summary>
/// The structural edits of a rule: replace an operator, wrap a node in <c>NOT</c>, negate it, insert an operand, delete a
/// node, swap two operands, replace a node with an expression (a palette entry or pasted text) and change a literal. Each
/// edit names a node by its <see cref="RuleTreeNode.Path"/>, changes a copy of the JSON tree of the rule, and compiles the
/// result. An edit whose result does not compile is refused with the diagnostics of the compiler; the rule never changes.
/// </summary>
/// <remarks>
/// <para>
/// The editor compiles permissively, as <see cref="RuleDocument"/> does, with the limits of the settings. The notes of the
/// nodes that an edit keeps stay on them. It has no state beyond the rule: each edit gives a new rule, and
/// <see cref="RuleDocument.ApplyEdit"/> puts it into the document as one undo step.
/// </para>
/// <para>
/// Given the <see cref="RuleDefinitions"/> of a document with <c>let</c> definitions, the editor edits the source of the
/// expanded rule instead, so the definitions stay. An edit of a node changes the node it comes from
/// (<see cref="RuleDefinitions.SourcesOf"/>): a node inside an expanded use changes the definition body, so every use
/// sees the change, and a node in the place of a parameter changes the argument of that use. Replace, wrap and negate act
/// on the outermost source, so on the root of an expanded use they change the use itself; a change of the operator, a
/// literal or the operands acts on the node that holds them. The edited document must compile, so an edit that would break
/// a definition (such as an operand added to a use) is refused. The nodes of a definition (paths that start with
/// <c>$.definitions</c>) can be edited directly, except a parameter, which only a replace can change.
/// </para>
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
    /// <param name="definitions">
    /// The definitions and uses of the text that <paramref name="rule"/> was compiled from (<see cref="RuleDocument.Definitions"/>),
    /// or <see langword="null"/> (the default) to edit the expanded rule. With definitions, each edit keeps them and gives the
    /// edited document in <see cref="RuleTreeEdit.Document"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public RuleTreeEditor(CompiledRule<StubAssignment> rule, ToolSettings? settings = null, RuleDefinitions? definitions = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        this.settings = settings ?? ToolSettings.Default;
        this.compiler = RuleConverter.CreateCompiler(this.settings);
        this.Tree = RuleTree.Of(rule, this.settings, definitions);
    }

    /// <summary>The part of the source of a node that an edit acts on, when the rule has definitions.</summary>
    private enum Part
    {
        /// <summary>The outermost source: the use, when the node is the root of an expanded use. Replace, wrap and negate act here.</summary>
        Source,

        /// <summary>The innermost source, which holds the operator, the literals and the operands.</summary>
        Content,
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
            Part.Content,
            (_, node, _) =>
            {
                if (DefinitionsPrinter.IsUse(node))
                {
                    return "the node is a use of a definition. Edit the body of the definition, or replace the use.";
                }

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
            Part.Source,
            (_, node, put) =>
            {
                put(Not(node));
                return null;
            },
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
            Part.Source,
            (_, node, put) =>
            {
                string? op = node["op"]?.GetValue<string>();
                if (string.Equals(op, "not", StringComparison.OrdinalIgnoreCase))
                {
                    if (node["operands"] is not JsonArray { Count: 1 } operands || operands[0] is not JsonObject operand)
                    {
                        return "the NOT has no single operand.";
                    }

                    put((JsonObject)operand.DeepClone());
                    return null;
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

                put(Not(node));
                return null;
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
            Part.Content,
            (_, node, _) =>
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
            Part.Source,
            (workspace, _, _) =>
            {
                if (parentPath is null)
                {
                    return "the root cannot be deleted. Replace it instead.";
                }

                // The operands of the parent hold the node at the same index in the source as in the expanded rule.
                if (
                    !workspace.TryFind(parentPath, Part.Content, out JsonObject? owner, out Action<JsonObject>? putOwner)
                    || owner["operands"] is not JsonArray operands
                    || OperandIndex(path!) is not int index
                    || index >= operands.Count
                )
                {
                    return "the node has no parent that holds it as an operand.";
                }

                operands.RemoveAt(index);
                if (operands.Count == 1 && owner["op"]?.GetValue<string>() is { } op && MultiOperand.Contains(op))
                {
                    putOwner((JsonObject)operands[0]!.DeepClone());
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
            Part.Source,
            (workspace, _, _) =>
            {
                JsonArray? parent =
                    parentPath is not null && workspace.TryFind(parentPath, Part.Content, out JsonObject? owner, out _)
                        ? owner["operands"] as JsonArray
                        : null;
                int index = OperandIndex(path!) ?? -1;
                int other = index + Math.Sign(direction);
                if (
                    parent is null
                    || parentPath is null
                    || direction == 0
                    || index >= parent.Count
                    || other < 0
                    || other >= parent.Count
                )
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

        return this.Edit(
            path,
            Part.Source,
            (_, _, put) =>
            {
                put(replacement);
                return null;
            },
            path,
            "Replaced the node."
        );
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
            Part.Content,
            (workspace, node, _) =>
            {
                if (workspace.IsParameter(path, node))
                {
                    return "the node is a parameter of the definition: a literal would make it a term. Replace it instead.";
                }

                if (DefinitionsPrinter.IsUse(node))
                {
                    return "the node is a use of a definition, which has no literals. Edit its arguments instead.";
                }

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

    // The index of a node among the operands of its parent, read from the end of its path; null for the root.
    private static int? OperandIndex(string path)
    {
        int open = path.LastIndexOf('[');
        return
            path.EndsWith(']')
            && open >= 0
            && int.TryParse(
                path.AsSpan(open + 1, path.Length - open - 2),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int index
            )
            ? index
            : null;
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
        if (this.Tree.Document is { } document)
        {
            return this.TryParseSource(expression, format, document, out tree, out refused);
        }

        CompilationResult<StubAssignment> compiled = this.compiler.CompileAs(expression, format);
        if (compiled.Rule is null)
        {
            refused = RuleTreeEdit.Refused($"Not a valid expression: {FirstError(compiled.Diagnostics)}", compiled.Diagnostics);
            return false;
        }

        tree = (JsonObject)JsonNode.Parse(compiled.Rule.PrintJson(includeNotes: true))!;
        return true;
    }

    // Reads an expression for a document with definitions: rule text is read after the declarations of the document, so it
    // can use them, and the source of the expression (with its uses) is what the edit puts in.
    private bool TryParseSource(
        string expression,
        RuleFormat format,
        JsonObject document,
        [NotNullWhen(true)] out JsonObject? tree,
        [NotNullWhen(false)] out RuleTreeEdit? refused
    )
    {
        tree = null;
        refused = null;
        string declarations =
            format == RuleFormat.Text
                ? DefinitionsPrinter.PrintDeclarations(document, this.settings) ?? string.Empty
                : string.Empty;
        RuleDefinitions read = YamlRuleDefinitions.ReadAs(declarations + expression, format);
        if (read.PrintJson() is not { } json)
        {
            refused = RuleTreeEdit.Refused($"Not a valid expression: {FirstError(read.Diagnostics)}", read.Diagnostics);
            return false;
        }

        JsonObject source = (JsonObject)JsonNode.Parse(json)!;
        source.Remove("definitions");

        // The expression must compile with the definitions of the document, on its own.
        JsonObject probe = [];
        if (document["definitions"] is { } definitions)
        {
            probe["definitions"] = definitions.DeepClone();
        }

        foreach ((string key, JsonNode? value) in source)
        {
            probe[key] = value?.DeepClone();
        }

        CompilationResult<StubAssignment> compiled = this.compiler.CompileAs(probe.ToJsonString(), RuleFormat.Json);
        if (compiled.Rule is null)
        {
            refused = RuleTreeEdit.Refused($"Not a valid expression: {FirstError(compiled.Diagnostics)}", compiled.Diagnostics);
            return false;
        }

        tree = source;
        return true;
    }

    // Applies a change to a copy of the tree (or of the source document, when the rule has definitions) and compiles the
    // result. The change gets the node of the part it acts on and a way to put another node in its place; it returns null
    // when it applied, or the reason it cannot apply.
    private RuleTreeEdit Edit(
        string path,
        Part part,
        Func<Workspace, JsonObject, Action<JsonObject>, string?> change,
        string? selected,
        string done
    )
    {
        ArgumentNullException.ThrowIfNull(path);

        Workspace workspace = new(this.Tree.CopyDocument() ?? this.Tree.CopyJson(), this.Tree.Definitions);
        if (!workspace.TryFind(path, part, out JsonObject? node, out Action<JsonObject>? put))
        {
            return RuleTreeEdit.Refused($"Not applied: the path '{path}' names no node of the rule.");
        }

        if (workspace.IsDeclaration(path))
        {
            return RuleTreeEdit.Refused("Not applied: select the body of the definition, or a node in it, to edit it.");
        }

        if (change(workspace, node, put) is { } problem)
        {
            return RuleTreeEdit.Refused("Not applied: " + problem);
        }

        string edited = workspace.Root.ToJsonString();
        CompilationResult<StubAssignment> compiled = this.compiler.CompileAs(edited, RuleFormat.Json);
        if (compiled.Rule is null)
        {
            string what = workspace.HasDefinitions ? "an invalid rule or definition" : "an invalid rule";
            return RuleTreeEdit.Refused(
                $"Not applied: the edit would make {what}. {FirstError(compiled.Diagnostics)}",
                compiled.Diagnostics
            );
        }

        RuleDefinitions? definitions = workspace.HasDefinitions ? RuleDefinitions.ReadJson(edited) : null;
        string? target =
            selected is not null && RuleTree.Of(compiled.Rule, this.settings, definitions).Find(selected) is not null
                ? selected
                : "$";
        return new RuleTreeEdit(compiled.Rule, target, done, compiled.Diagnostics)
        {
            Document = workspace.HasDefinitions ? edited : null,
        };
    }

    /// <summary>
    /// The JSON that an edit changes: the tree of the rule, or the source document with its definitions. It finds the node
    /// that a path of the tree names, in the part an edit acts on.
    /// </summary>
    /// <param name="Root">The JSON being edited.</param>
    /// <param name="Definitions">The definitions of the source, or <see langword="null"/> when the tree of the rule is edited.</param>
    private sealed record Workspace(JsonObject Root, RuleDefinitions? Definitions)
    {
        private const string DefinitionsPrefix = "$.definitions";

        [MemberNotNullWhen(true, nameof(Definitions))]
        public bool HasDefinitions => this.Definitions is not null;

        public bool TryFind(
            string path,
            Part part,
            [NotNullWhen(true)] out JsonObject? node,
            [NotNullWhen(true)] out Action<JsonObject>? put
        )
        {
            node = null;
            put = null;
            if (!this.HasDefinitions)
            {
                if (!RuleTree.TryLocate(this.Root, path, out node, out JsonArray? parent, out int index))
                {
                    return false;
                }

                JsonObject root = this.Root;
                put = parent is null
                    ? replacement => RuleTree.ReplaceMembers(root, replacement)
                    : replacement => parent[index] = replacement;
                return true;
            }

            // A node of a definition is named by its own path; a node of the rule by the sources of its expansion.
            EquatableArray<string> sources = path.StartsWith(DefinitionsPrefix, StringComparison.Ordinal)
                ? new EquatableArray<string>([path])
                : this.Definitions.SourcesOf(path);
            if (sources.Count == 0)
            {
                return false;
            }

            return RuleTree.TryLocatePath(this.Root, part == Part.Source ? sources[0] : sources[^1], out node, out put);
        }

        // The entry of a definition itself, such as $.definitions.both, which is not a node of the rule.
        public bool IsDeclaration(string path)
        {
            return this.HasDefinitions
                && this.Definitions.Definitions.Any(d => string.Equals(d.Path, path, StringComparison.Ordinal));
        }

        // A parameter inside the body of a definition: a bare predicate, with no arguments, that names a parameter.
        public bool IsParameter(string path, JsonObject node)
        {
            if (!this.HasDefinitions || node.ContainsKey("args") || node["predicate"]?.ToString() is not { } name)
            {
                return false;
            }

            return this.Definitions.Definitions.Any(d =>
                path.StartsWith(d.Path + ".", StringComparison.Ordinal)
                && d.Parameters.Contains(name, StringComparer.OrdinalIgnoreCase)
            );
        }
    }
}
