namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Yaml;

/// <summary>
/// What the Insert menu offers: the operators in groups, the constants, a variable, a definition and a predicate call. The
/// text of an entry is rule text with the placeholder terms <c>a</c>, <c>b</c> and <c>c</c>. The description of an operator or
/// a constant is not written here: it is the description of the library, read from the outline of the compiled text
/// (<c>OperatorDefinitions</c> and <c>OperatorInfo</c>), so there is one source for it. The description of a predicate is the
/// description of its catalog entry.
/// </summary>
internal static class InsertItems
{
    private static readonly Lazy<IReadOnlyList<InsertGroup>> OperatorGroups = new(CreateOperators);
    private static readonly Lazy<IReadOnlyList<InsertItem>> ConstantItems = new(CreateConstants);

    /// <summary>Gets the operators, in the groups of the submenu: connectives, cardinality, inspection and other.</summary>
    public static IReadOnlyList<InsertGroup> Operators => OperatorGroups.Value;

    /// <summary>Gets the constants: <c>True</c>, <c>False</c> and <c>Unknown</c>, then the letters <c>T</c>, <c>F</c> and <c>U</c>.</summary>
    public static IReadOnlyList<InsertItem> Constants => ConstantItems.Value;

    /// <summary>Gets the entry that inserts a new <c>let</c> definition.</summary>
    public static InsertItem NewDefinition { get; } =
        new(
            "New definition",
            "let both(first, second) = first AND second; ",
            "A named sub-expression with parameters; each use is replaced by its body when the rule compiles.",
            "{0}both(a, b)"
        );

    /// <summary>Gets every operator and constant entry, for a check of the descriptions and of the text.</summary>
    public static IEnumerable<InsertItem> AllFixed => Operators.SelectMany(g => g.Items).Concat(Constants);

    /// <summary>Makes the entry for a <c>from(...)</c> variable.</summary>
    /// <param name="source">The name of the data source.</param>
    /// <param name="query">The query that reads the value from the source.</param>
    /// <returns>The entry. Its text is an argument value, so it compiles inside a predicate call.</returns>
    public static InsertItem Variable(string source, string query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        return new InsertItem(
            "Variable",
            $"from({Quote(source)}, {Quote(query)})",
            "A value that the rule reads from a named data source when it runs.",
            "isSet(value: {0})"
        );
    }

    /// <summary>
    /// Makes the rule text of a predicate or a definition from a palette entry, with a placeholder for each argument: the
    /// default of the argument when it has one, otherwise a value of its kind. An optional argument without a default is left out.
    /// </summary>
    /// <param name="entry">The palette entry.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The text, or the diagnostics when the call does not compile.</returns>
    public static ToolResult<string> PredicateText(PaletteEntry entry, ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(settings);

        // A definition parameter is rule text, so its own name is the placeholder term.
        string[] values = [.. entry.Arguments.Select(a => ValueOf(entry, a))];
        return RulePalette.Fill(entry, values, settings);
    }

    private static string ValueOf(PaletteEntry entry, PaletteArgument argument)
    {
        if (entry.Source == PaletteSource.Definition)
        {
            return argument.Name;
        }

        if (argument.Default is { } given)
        {
            return given;
        }

        return argument.Required ? Placeholder(argument.Kind) : string.Empty;
    }

    private static string Quote(string text)
    {
        return "\""
            + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)
            + "\"";
    }

    private static string Placeholder(LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String => "text",
            LiteralKind.Boolean => "true",
            LiteralKind.DateTimeOffset => "2026-01-01T00:00:00Z",
            LiteralKind.Guid => "00000000-0000-0000-0000-000000000000",
            LiteralKind.StringArray => "a, b",
            LiteralKind.Int64Array or LiteralKind.DecimalArray => "[0]",
            LiteralKind.BooleanArray => "[true]",
            LiteralKind.DateTimeOffsetArray => "[\"2026-01-01T00:00:00Z\"]",
            LiteralKind.GuidArray => "[\"00000000-0000-0000-0000-000000000000\"]",
            _ => "0",
        };
    }

    private static List<InsertGroup> CreateOperators()
    {
        // Infix operators are in parentheses, so the text keeps its meaning next to the text around the cursor.
        return
        [
            new InsertGroup(
                "Connectives",
                Described(
                    ("NOT", "NOT a"),
                    ("AND", "(a AND b)"),
                    ("OR", "(a OR b)"),
                    ("XOR", "(a XOR b)"),
                    ("EQUIVALENT", "(a EQUIVALENT b)"),
                    ("IMPLIES", "(a IMPLIES b)"),
                    ("NAND", "(a NAND b)"),
                    ("NOR", "(a NOR b)"),
                    ("PARITY", "PARITY(a, b, c)")
                )
            ),
            new InsertGroup(
                "Cardinality",
                Described(
                    ("ANY", "ANY(a, b)"),
                    ("ALL", "ALL(a, b)"),
                    ("NONE", "NONE(a, b)"),
                    ("ExactlyOne", "ExactlyOne(a, b)"),
                    ("AtLeast", "AtLeast(1, a, b)"),
                    ("AtMost", "AtMost(1, a, b)"),
                    ("Exactly", "Exactly(1, a, b)"),
                    ("GreaterThan", "GreaterThan(1, a, b)"),
                    ("LessThan", "LessThan(1, a, b)"),
                    ("BETWEEN", "BETWEEN(1, 2, a, b, c)")
                )
            ),
            new InsertGroup(
                "Inspection",
                Described(
                    ("IsTrue", "IsTrue(a)"),
                    ("IsFalse", "IsFalse(a)"),
                    ("IsUnknown", "IsUnknown(a)"),
                    ("IsKnown", "IsKnown(a)")
                )
            ),
            new InsertGroup("Other", Described(("If", "If(a, b, c)"), ("COALESCE", "COALESCE(a, b)"))),
        ];
    }

    private static List<InsertItem> CreateConstants()
    {
        List<InsertItem> words = [.. Described(("True", "True"), ("False", "False"), ("Unknown", "Unknown"))];
        List<InsertItem> letters =
        [
            .. Described(("T", "T"), ("F", "F"), ("U", "U"))
                .Select(
                    (item, index) => item with { Description = item.Description + $" The letter form of {words[index].Name}." }
                ),
        ];
        return [.. words, .. letters];
    }

    // Each entry gets the description of the library for the root of its compiled text.
    private static List<InsertItem> Described(params (string Name, string Text)[] entries)
    {
        return [.. entries.Select(e => new InsertItem(e.Name, e.Text, DescriptionOf(e.Text)))];
    }

    private static string DescriptionOf(string text)
    {
        CompilationResult<TruthWeaver.Tool.Core.Assignment.StubAssignment> result = RuleConverter
            .CreateCompiler(ToolSettings.Default)
            .CompileAs(text, RuleFormat.Text);
        return result.Rule?.Outline().Description ?? string.Empty;
    }
}
