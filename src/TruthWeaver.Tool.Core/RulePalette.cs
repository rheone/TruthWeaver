namespace TruthWeaver.Tool.Core;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;

/// <summary>
/// The palette of a structural editor: the operators and constants, which are always there, then the predicates that the
/// predicate schema file declares, then the predicates that the open rule uses, then the <c>let</c> definitions of the
/// open rule. A predicate is a template: its name and its arguments with their kinds; a definition is a template with its
/// parameters. <see cref="Fill"/> makes an expression of a template and the values of a form.
/// </summary>
public sealed class RulePalette
{
    // The operators that a palette offers, by their name in the JSON tree format. The quantifiers are not here: they need
    // a variable and a collection, which the rule text names.
    private static readonly (string Name, string Description)[] Operators =
    [
        ("and", "True when every operand is True."),
        ("or", "True when at least one operand is True."),
        ("not", "Negation. Unknown stays Unknown."),
        ("xor", "True when exactly one of the two operands is True."),
        ("equivalent", "True when the two operands agree."),
        ("implies", "NOT antecedent OR consequent."),
        ("nand", "NOT (left AND right)."),
        ("nor", "NOT (left OR right)."),
        ("parity", "True when an odd number of operands are True."),
        ("any", "True when at least one operand is True."),
        ("all", "True when every operand is True."),
        ("none", "True when no operand is True."),
        ("exactlyOne", "True when exactly one operand is True."),
        ("atLeast", "True when at least k operands are True."),
        ("atMost", "True when at most k operands are True."),
        ("greaterThan", "True when more than k operands are True."),
        ("lessThan", "True when fewer than k operands are True."),
        ("exactly", "True when exactly k operands are True."),
        ("between", "True when min to max operands are True."),
        ("coalesce", "The first operand that is not Unknown."),
        ("if", "The second operand when the condition is True, the third when it is False."),
        ("isTrue", "True when the operand is True; never Unknown."),
        ("isFalse", "True when the operand is False; never Unknown."),
        ("isUnknown", "True when the operand is Unknown; never Unknown."),
        ("isKnown", "True when the operand is True or False; never Unknown."),
    ];

    private RulePalette(IReadOnlyList<PaletteEntry> entries)
    {
        this.Entries = entries;
    }

    /// <summary>
    /// Gets the entries: the operators, the constants, the predicates of the schema file and those of the rule, then the
    /// definitions of the rule.
    /// </summary>
    public IReadOnlyList<PaletteEntry> Entries { get; }

    /// <summary>Makes the palette for a rule and a schema file.</summary>
    /// <param name="rule">The open rule, or <see langword="null"/> when no text compiled yet.</param>
    /// <param name="schema">The predicate schema file, or <see langword="null"/> when there is none.</param>
    /// <param name="definitions">
    /// The definitions and uses of the open rule (<see cref="RuleDocument.Definitions"/>), or <see langword="null"/> (the
    /// default) for none. Each definition is an entry of <see cref="PaletteSource.Definition"/>.
    /// </param>
    /// <returns>The palette.</returns>
    public static RulePalette Create(
        CompiledRule<StubAssignment>? rule,
        PredicateSchemaFile? schema,
        RuleDefinitions? definitions = null
    )
    {
        List<PaletteEntry> entries =
        [
            .. Operators.Select(o => new PaletteEntry(PaletteSource.Operator, o.Name, o.Description, [])),
            new(PaletteSource.Constant, "True", "The constant True.", []),
            new(PaletteSource.Constant, "False", "The constant False.", []),
            new(PaletteSource.Constant, "Unknown", "The constant Unknown.", []),
        ];

        foreach (PredicateSchema predicate in schema?.Predicates ?? [])
        {
            entries.Add(
                new PaletteEntry(
                    PaletteSource.SchemaFile,
                    predicate.Name,
                    predicate.Description,
                    [.. predicate.Arguments.Select(a => new PaletteArgument(a.Name, a.Type, a.Required, null, a.Description))]
                )
            );
        }

        if (rule is not null)
        {
            HashSet<string> seen = [with(StringComparer.Ordinal)];
            CollectTerms(JsonNode.Parse(rule.PrintJson(includeNotes: false)), entries, seen);
        }

        foreach (DefinitionDeclaration definition in definitions?.Definitions ?? EquatableArray<DefinitionDeclaration>.Empty)
        {
            entries.Add(
                new PaletteEntry(
                    PaletteSource.Definition,
                    definition.Name,
                    "A definition of the open rule.",
                    [
                        .. definition.Parameters.Select(p => new PaletteArgument(
                            p,
                            LiteralKind.String,
                            true,
                            null,
                            "An expression: rule text such as a term, a constant or a sub-expression."
                        )),
                    ]
                )
            );
        }

        return new RulePalette(entries);
    }

    /// <summary>
    /// Makes an expression of an entry: the constant itself, a call of the predicate with the values of a form, or a use of
    /// a definition. A value of a predicate argument is JSON (<c>18</c>, <c>true</c>, <c>["a", "b"]</c>, or a <c>from</c>
    /// reference as a JSON object); for a string, GUID or date and time argument, plain text is the string, and for an array
    /// of strings, plain text is a list separated by commas. An empty value leaves out an argument that is not required. A
    /// value of a definition parameter is rule text; the use (<c>name(a, b)</c>, or the bare name without parameters) is
    /// not compiled here, because it compiles only in the document that has the definition.
    /// </summary>
    /// <param name="entry">A constant or a predicate entry.</param>
    /// <param name="values">The values, one for each argument of the entry, in order.</param>
    /// <param name="settings">The settings (limits and operator style), or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The expression as rule text, or the diagnostics when the call does not compile.</returns>
    /// <exception cref="ArgumentException"><paramref name="entry"/> is an operator, or the number of values is not the number of arguments.</exception>
    public static ToolResult<string> Fill(PaletteEntry entry, IReadOnlyList<string> values, ToolSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(values);
        if (entry.Source == PaletteSource.Operator)
        {
            throw new ArgumentException(
                "An operator is not an expression; replace the operator of a node with it.",
                nameof(entry)
            );
        }

        if (values.Count != entry.Arguments.Count)
        {
            throw new ArgumentException(
                $"The entry has {entry.Arguments.Count} arguments, not {values.Count}.",
                nameof(values)
            );
        }

        if (entry.Source == PaletteSource.Constant)
        {
            return ToolResults.Success(entry.Name);
        }

        if (entry.Source == PaletteSource.Definition)
        {
            return ToolResults.Success(
                values.Count == 0 ? entry.Name : $"{entry.Name}({string.Join(", ", values.Select(v => v.Trim()))})"
            );
        }

        JsonObject args = [];
        for (int i = 0; i < values.Count; i++)
        {
            PaletteArgument argument = entry.Arguments[i];
            if (values[i].Length == 0 && !argument.Required)
            {
                continue;
            }

            args[argument.Name] = ValueOf(argument.Kind, values[i]);
        }

        JsonObject call = new() { ["predicate"] = entry.Name, ["args"] = args };
        ToolSettings resolved = settings ?? ToolSettings.Default;
        CompilationResult<StubAssignment> compiled = RuleConverter
            .CreateCompiler(resolved)
            .CompileAs(call.ToJsonString(), RuleFormat.Json);
        return compiled.Rule is { } rule
            ? ToolResults.Success(RuleTree.PrintText(rule, resolved))
            : ToolResults.Failure<string>(compiled.Diagnostics);
    }

    private static JsonNode? ValueOf(LiteralKind kind, string text)
    {
        string trimmed = text.Trim();
        bool json = trimmed.StartsWith('{') || trimmed.StartsWith('"') || trimmed.StartsWith('[');
        return kind switch
        {
            LiteralKind.String or LiteralKind.Guid or LiteralKind.DateTimeOffset when !json => JsonValue.Create(text),
            LiteralKind.StringArray or LiteralKind.GuidArray or LiteralKind.DateTimeOffsetArray when !json => new JsonArray([
                .. text.Split(',').Select(part => (JsonNode?)JsonValue.Create(part.Trim())),
            ]),
            _ => RuleTreeEditor.ParseValue(text),
        };
    }

    // The kind of an argument value as the rule writes it. A from(...) reference is a JSON object; its kind is not known, so
    // the template offers it as a string.
    private static LiteralKind KindOf(JsonNode? value)
    {
        return value switch
        {
            JsonValue v when v.TryGetValue(out bool _) => LiteralKind.Boolean,
            JsonValue v when v.TryGetValue(out long _) => LiteralKind.Int64,
            JsonValue v when v.TryGetValue(out decimal _) => LiteralKind.Decimal,
            JsonArray { Count: > 0 } array => LiteralValue.ToArrayKind(KindOf(array[0])),
            JsonArray => LiteralKind.StringArray,
            _ => LiteralKind.String,
        };
    }

    // The first use of each predicate, in tree order, as a template whose values start at the values of that use.
    private static void CollectTerms(JsonNode? node, List<PaletteEntry> entries, HashSet<string> seen)
    {
        switch (node)
        {
            case JsonObject tree when tree["predicate"] is JsonValue name:
                if (seen.Add(name.ToString()))
                {
                    List<PaletteArgument> arguments = [];
                    if (tree["args"] is JsonObject args)
                    {
                        arguments.AddRange(
                            args.Select(a => new PaletteArgument(
                                a.Key,
                                KindOf(a.Value),
                                true,
                                a.Value is JsonValue text && text.TryGetValue(out string? s) ? s : a.Value?.ToJsonString(),
                                string.Empty
                            ))
                        );
                    }

                    entries.Add(new PaletteEntry(PaletteSource.Document, name.ToString(), "Used in the open rule.", arguments));
                }

                break;
            case JsonObject tree:
                foreach ((string _, JsonNode? child) in tree)
                {
                    CollectTerms(child, entries, seen);
                }

                break;
            case JsonArray list:
                foreach (JsonNode? child in list)
                {
                    CollectTerms(child, entries, seen);
                }

                break;
        }
    }
}
