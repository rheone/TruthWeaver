namespace TruthWeaver.Tool.Core;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Yaml;

/// <summary>
/// Prints a rule document that has <c>let</c> definitions, given as the JSON tree of <see cref="RuleDefinitions.PrintJson"/>,
/// in rule text, JSON or YAML, and keeps its definitions and uses. The library prints JSON and YAML; rule text is put
/// together here: each definition body and the rule are printed by the rule text printer with each use replaced by a
/// placeholder term, and the placeholder is then replaced by the text of the use.
/// </summary>
internal static class DefinitionsPrinter
{
    private const string PlaceholderStem = "__twuse";

    /// <summary>Prints a document in a format.</summary>
    /// <param name="document">The document in the JSON tree format, with its definitions section.</param>
    /// <param name="format">The format to print.</param>
    /// <param name="settings">The settings: the operator style of rule text and the limits of the compiler.</param>
    /// <param name="pretty"><see langword="true"/> for the pretty form (indented JSON, rule text broken at <see cref="ArtifactCatalog.PrettyWidth"/>).</param>
    /// <returns>The text, or <see langword="null"/> when a part of the document does not compile on its own.</returns>
    public static string? Print(JsonObject document, RuleFormat format, ToolSettings settings, bool pretty)
    {
        return format switch
        {
            RuleFormat.Json => pretty ? RuleDocument.IndentJson(document.ToJsonString()) : document.ToJsonString(),

            // The YAML printer may write CRLF on Windows; the document keeps line feeds.
            RuleFormat.Yaml => RuleDefinitions.ReadJson(document.ToJsonString()).PrintYaml()?.ReplaceLineEndings("\n"),
            _ => PrintText(document, settings, pretty ? ArtifactCatalog.PrettyWidth : null),
        };
    }

    /// <summary>Prints the <c>let</c> declarations of a document as rule text, one per line, each ending in a line feed.</summary>
    /// <param name="document">The document in the JSON tree format.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The declarations (an empty string when there are none), or <see langword="null"/> when a body does not compile on its own.</returns>
    public static string? PrintDeclarations(JsonObject document, ToolSettings settings)
    {
        Printer printer = new(document, settings, null);
        return printer.Declarations();
    }

    /// <summary>Prints one node of a document as rule text: a use prints as its name with its arguments, such as <c>both(a, c)</c>.</summary>
    /// <param name="document">The document the node belongs to, which the placeholder names must not occur in.</param>
    /// <param name="node">The node.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The text, or <see langword="null"/> when the node does not compile on its own.</returns>
    public static string? PrintNode(JsonObject document, JsonObject node, ToolSettings settings)
    {
        return new Printer(document, settings, null).Node(node);
    }

    /// <summary>Gets a value indicating whether a node of the JSON tree format is a use of a definition.</summary>
    /// <param name="node">The node.</param>
    /// <returns><see langword="true"/> for a <c>{"op": "use"}</c> node.</returns>
    internal static bool IsUse(JsonObject node)
    {
        return node["op"] is JsonValue op
            && op.TryGetValue(out string? name)
            && string.Equals(name, "use", StringComparison.OrdinalIgnoreCase);
    }

    private static string? PrintText(JsonObject document, ToolSettings settings, int? width)
    {
        Printer printer = new(document, settings, width);
        if (printer.Declarations() is not { } declarations)
        {
            return null;
        }

        JsonObject body = (JsonObject)document.DeepClone();
        body.Remove("definitions");
        return printer.Node(body) is { } rule ? declarations + rule : null;
    }

    /// <summary>Prints the parts of one document, with placeholder names that the document does not contain.</summary>
    private sealed class Printer
    {
        private readonly JsonObject document;
        private readonly ToolSettings settings;
        private readonly int? width;
        private readonly RuleCompiler<StubAssignment> compiler;
        private readonly string stem;

        public Printer(JsonObject document, ToolSettings settings, int? width)
        {
            this.document = document;
            this.settings = settings;
            this.width = width;
            this.compiler = RuleConverter.CreateCompiler(settings);

            // A placeholder must not match any name in the document, or the replacement would also change that name.
            string text = document.ToJsonString();
            int underscores = 0;
            while (text.Contains(PlaceholderStem + new string('_', underscores), StringComparison.Ordinal))
            {
                underscores++;
            }

            this.stem = PlaceholderStem + new string('_', underscores);
        }

        public string? Declarations()
        {
            if (this.document["definitions"] is not JsonObject definitions)
            {
                return string.Empty;
            }

            StringBuilder text = new();
            foreach ((string name, JsonNode? entry) in definitions)
            {
                if (entry?["body"] is not JsonObject body || this.Node(body) is not { } printed)
                {
                    return null;
                }

                string[] parameters = entry["parameters"] is JsonArray list
                    ? [.. list.Select(p => p?.ToString() ?? string.Empty)]
                    : [];
                text.Append("let ").Append(name);
                if (parameters.Length > 0)
                {
                    text.Append('(').AppendJoin(", ", parameters).Append(')');
                }

                text.Append(" = ").Append(printed).Append(";\n");
            }

            return text.ToString();
        }

        public string? Node(JsonObject node)
        {
            List<string?> uses = [];
            JsonObject piece = (JsonObject)node.DeepClone();
            if (this.Replace(piece, uses) is { } replaced)
            {
                piece = replaced;
            }

            if (uses.Exists(u => u is null))
            {
                return null;
            }

            CompiledRule<StubAssignment>? rule = this.compiler.CompileAs(piece.ToJsonString(), RuleFormat.Json).Rule;
            if (rule is null)
            {
                return null;
            }

            RuleTextOptions options = new()
            {
                OperatorStyle = this.settings.OperatorStyle,
                IncludeNotes = true,
                Width = this.width,
            };

            // The C-style operators have no rule text that reads back; the word style always does.
            string text = rule.TryPrintRuleText(options, out string? styled)
                ? styled
                : rule.PrintRuleText(options with { OperatorStyle = OperatorStyle.Word });
            return Regex.Replace(
                text,
                Regex.Escape(this.stem) + "([0-9]+)__",
                m => uses[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)]!,
                RegexOptions.None,
                TimeSpan.FromSeconds(1)
            );
        }

        // Replaces each use under a node by a placeholder term and records the text of the use. It returns the placeholder
        // when the node itself is a use, so the caller can put it in the node's place.
        private JsonObject? Replace(JsonObject node, List<string?> uses)
        {
            if (IsUse(node))
            {
                string name = node["name"]?.ToString() ?? string.Empty;
                string? text = name;
                if (node["operands"] is JsonArray { Count: > 0 } arguments)
                {
                    List<string> printed = [];
                    foreach (JsonNode? argument in arguments)
                    {
                        if (argument is not JsonObject value || this.Node(value) is not { } argumentText)
                        {
                            text = null;
                            break;
                        }

                        printed.Add(argumentText);
                    }

                    text = text is null ? null : $"{name}({string.Join(", ", printed)})";
                }

                JsonObject placeholder = new()
                {
                    ["predicate"] = string.Create(CultureInfo.InvariantCulture, $"{this.stem}{uses.Count}__"),
                };
                if (node["note"] is { } note)
                {
                    placeholder["note"] = note.DeepClone();
                }

                uses.Add(text);
                return placeholder;
            }

            if (node["operands"] is JsonArray operands)
            {
                for (int i = 0; i < operands.Count; i++)
                {
                    if (operands[i] is JsonObject operand && this.Replace(operand, uses) is { } replaced)
                    {
                        operands[i] = replaced;
                    }
                }
            }

            return null;
        }
    }
}
