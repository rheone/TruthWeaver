namespace TruthWeaver.Tool.Core;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Yaml;

/// <summary>
/// Every artifact that the tools can make from a rule: the rule text, JSON and YAML; the equation in every dialect and
/// constant style, with the LaTeX envelopes; the plain text tree, Mermaid, DOT and HTML; the truth table in every format;
/// the statistics; the diagnostics; and the Markdown report. The order of <see cref="All"/> is stable.
/// </summary>
/// <remarks>
/// Each artifact calls one library printer with fixed options, so its text is exactly what that printer gives. A
/// decision (an evaluation result) is not an artifact: it needs stub values and a serializable decision record.
/// </remarks>
public static class ArtifactCatalog
{
    /// <summary>The page width of the <c>rule-text-pretty</c> artifact, in characters.</summary>
    public const int PrettyWidth = 80;

    private static readonly (EquationDialect Dialect, string Name, string Extension, string Format)[] Dialects =
    [
        (EquationDialect.Unicode, "unicode", ".txt", "text"),
        (EquationDialect.LaTeX, "latex", ".tex", "latex"),
        (EquationDialect.AsciiMath, "asciimath", ".txt", "asciimath"),
        (EquationDialect.CStyle, "cstyle", ".txt", "text"),
        (EquationDialect.MathML, "mathml", ".mml", "mathml"),
        (EquationDialect.Typst, "typst", ".typ", "typst"),
        (EquationDialect.Ascii, "ascii", ".txt", "text"),
    ];

    private static readonly (TruthTableFormat Format, string Name, string Extension)[] TableFormats =
    [
        (TruthTableFormat.Markdown, "markdown", ".md"),
        (TruthTableFormat.Csv, "csv", ".csv"),
        (TruthTableFormat.Json, "json", ".json"),
        (TruthTableFormat.Html, "html", ".html"),
        (TruthTableFormat.Latex, "latex", ".tex"),
    ];

    private static readonly ConstantStyle[] ConstantStyles =
    [
        ConstantStyle.Letters,
        ConstantStyle.Word,
        ConstantStyle.LogicSymbols,
    ];

    private static readonly ImmutableArray<ArtifactDefinition> Definitions = [.. Build()];

    // Declared after Definitions, so that it is initialized after it.
    private static readonly Dictionary<string, ArtifactDefinition> ById = Definitions.ToDictionary(
        definition => definition.Id,
        StringComparer.Ordinal
    );

    /// <summary>Gets every artifact, in the stable catalog order.</summary>
    public static IReadOnlyList<ArtifactDefinition> All => Definitions;

    /// <summary>Finds an artifact by its id. The id is matched exactly (ids are lower case).</summary>
    /// <param name="id">The artifact id.</param>
    /// <param name="artifact">The artifact, or <see langword="null"/> when no artifact has the id.</param>
    /// <returns><see langword="true"/> when an artifact has the id.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public static bool TryFind(string id, [NotNullWhen(true)] out ArtifactDefinition? artifact)
    {
        ArgumentNullException.ThrowIfNull(id);

        return ById.TryGetValue(id, out artifact);
    }

    /// <summary>
    /// Gets the equation artifact for a dialect, a constant style and a LaTeX envelope, so that a front end can map its
    /// options to an id: <c>equation-&lt;dialect&gt;[-doubledollar|-mathjax]-&lt;constants&gt;</c>.
    /// </summary>
    /// <param name="dialect">The dialect.</param>
    /// <param name="constants">The constant style.</param>
    /// <param name="latexWrap">The LaTeX envelope. Only the <see cref="EquationDialect.LaTeX"/> dialect has one; the other dialects need <see cref="LatexWrapMode.None"/>.</param>
    /// <returns>The artifact.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A value is not defined, or <paramref name="latexWrap"/> is set for a dialect other than LaTeX.</exception>
    public static ArtifactDefinition Equation(
        EquationDialect dialect,
        ConstantStyle constants,
        LatexWrapMode latexWrap = LatexWrapMode.None
    )
    {
        if (latexWrap != LatexWrapMode.None && dialect != EquationDialect.LaTeX)
        {
            throw new ArgumentOutOfRangeException(nameof(latexWrap), latexWrap, "Only the LaTeX dialect has an envelope.");
        }

        string id = EquationId(DialectName(dialect), WrapName(latexWrap), ConstantName(constants));
        return ById[id];
    }

    /// <summary>Gets the truth-table artifact for a format: <c>truth-table-&lt;format&gt;</c>.</summary>
    /// <param name="format">The table format.</param>
    /// <returns>The artifact.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined value.</exception>
    public static ArtifactDefinition TruthTable(TruthTableFormat format)
    {
        foreach ((TruthTableFormat value, string name, _) in TableFormats)
        {
            if (value == format)
            {
                return ById["truth-table-" + name];
            }
        }

        throw new ArgumentOutOfRangeException(nameof(format), format, "The value is not a truth-table format.");
    }

    private static IEnumerable<ArtifactDefinition> Build()
    {
        yield return Artifact(
            "rule-text",
            ".rule",
            "rule-text",
            "The rule as rule text, in the word style with parentheses and the notes.",
            [],
            source => source.Print(rule => rule.PrintRuleText())
        );
        yield return Artifact(
            "rule-text-symbolic",
            ".rule",
            "rule-text",
            "The rule as rule text with the symbolic operators.",
            [Option("operatorStyle", "symbolic")],
            source => source.Print(rule => rule.PrintRuleText(GroupingStyle.Parentheses, OperatorStyle.Symbolic))
        );
        yield return Artifact(
            "rule-text-pretty",
            ".rule",
            "rule-text",
            $"The rule as rule text, broken across lines to a width of {PrettyWidth} characters.",
            [Option("width", PrettyWidth.ToString(CultureInfo.InvariantCulture)), Option("indentSize", "2")],
            source => source.Print(rule => rule.PrintRuleText(new RuleTextOptions { Width = PrettyWidth }))
        );
        yield return Artifact(
            "json",
            ".json",
            "json",
            "The rule in the JSON tree format, with the notes.",
            [],
            source => source.Print(rule => rule.PrintJson())
        );
        yield return Artifact(
            "yaml",
            ".yaml",
            "yaml",
            "The rule in the YAML tree format, with the notes.",
            [],
            source => source.Print(rule => rule.PrintYaml())
        );

        foreach ((EquationDialect dialect, string dialectName, string extension, string format) in Dialects)
        {
            LatexWrapMode[] wraps =
                dialect == EquationDialect.LaTeX
                    ? [LatexWrapMode.None, LatexWrapMode.DoubleDollar, LatexWrapMode.MathJaxSafe]
                    : [LatexWrapMode.None];
            foreach (LatexWrapMode wrap in wraps)
            {
                foreach (ConstantStyle constants in ConstantStyles)
                {
                    yield return EquationArtifact(dialect, dialectName, extension, format, wrap, constants);
                }
            }
        }

        yield return Artifact(
            "tree",
            ".txt",
            "text",
            "The rule as an indented plain text tree.",
            [],
            source => source.Print(rule => rule.PrintPlainText())
        );
        yield return Artifact(
            "mermaid",
            ".mmd",
            "mermaid",
            "The rule as a Mermaid flowchart.",
            [],
            source => source.Print(rule => rule.PrintMermaid())
        );
        yield return Artifact(
            "dot",
            ".dot",
            "dot",
            "The rule as a Graphviz DOT graph.",
            [],
            source => source.Print(rule => rule.PrintDot())
        );
        yield return Artifact(
            "html",
            ".html",
            "html",
            "The rule as an HTML tree.",
            [],
            source => source.Print(rule => rule.PrintHtml())
        );

        foreach ((TruthTableFormat format, string name, string extension) in TableFormats)
        {
            yield return Artifact(
                "truth-table-" + name,
                extension,
                name,
                $"The Strong K3 truth table in {name}.",
                [Option("format", name)],
                source => source.PrintOrRefuse(rule => PrintTruthTable(rule, format, source.Options.AllowLargerTruthTable))
            );
        }

        yield return Artifact(
            "statistics",
            ".json",
            "json",
            "The statistics of the rule as JSON.",
            [],
            source => Statistics(source, DiagnosticOutputFormat.Json)
        );
        yield return Artifact(
            "statistics-text",
            ".txt",
            "text",
            "The statistics of the rule as text.",
            [],
            source => Statistics(source, DiagnosticOutputFormat.Text)
        );
        yield return Artifact(
            "diagnostics",
            ".txt",
            "text",
            "The diagnostics of every validation level as text.",
            [],
            source => Diagnostics(source, DiagnosticOutputFormat.Text)
        );
        yield return Artifact(
            "diagnostics-json",
            ".json",
            "json",
            "The diagnostics of every validation level as JSON.",
            [],
            source => Diagnostics(source, DiagnosticOutputFormat.Json)
        );
        yield return Artifact(
            "diagnostics-sarif",
            ".sarif",
            "sarif",
            "The diagnostics of every validation level as SARIF 2.1.0.",
            [],
            source => Diagnostics(source, DiagnosticOutputFormat.Sarif)
        );
        yield return Artifact(
            "report",
            ".md",
            "markdown",
            "The Markdown report: the rule, the equation, a diagram, the truth table, the statistics and the analysis.",
            [],
            RuleReport.Format
        );
    }

    private static ArtifactDefinition EquationArtifact(
        EquationDialect dialect,
        string dialectName,
        string extension,
        string format,
        LatexWrapMode wrap,
        ConstantStyle constants
    )
    {
        string wrapName = WrapName(wrap);
        EquationOptions options = new()
        {
            Dialect = dialect,
            LatexWrap = wrap,
            Constants = new ConstantFormat { Style = constants },
        };

        // The MathJax envelope is for Markdown, so its file is Markdown.
        bool markdown = wrap == LatexWrapMode.MathJaxSafe;
        string description =
            $"The equation in the {dialectName} dialect"
            + (wrap == LatexWrapMode.None ? string.Empty : $" with the {wrapName} envelope")
            + $", with the constants as {ConstantName(constants)}.";
        return Artifact(
            EquationId(dialectName, wrapName, ConstantName(constants)),
            markdown ? ".md" : extension,
            markdown ? "markdown" : format,
            description,
            [
                Option("dialect", dialectName),
                Option("constants", ConstantName(constants)),
                .. wrap == LatexWrapMode.None ? Array.Empty<KeyValuePair<string, string>>() : [Option("latexWrap", wrapName)],
            ],
            source => source.Print(rule => rule.PrintEquation(options))
        );
    }

    // A truth table, or the TRE0035 error when the rule has too many terms.
    private static ToolResult<string> PrintTruthTable(
        CompiledRule<StubAssignment> rule,
        TruthTableFormat format,
        bool allowLargerTable
    )
    {
        TruthTableOptions options = new() { Format = format, AllowLargerTable = allowLargerTable };
        return rule.TryPrintTruthTable(
            options,
            out string? text,
            out IReadOnlyList<TruthWeaver.Diagnostics.Diagnostic> diagnostics
        )
            ? ToolResults.Success(text, diagnostics)
            : ToolResults.Failure<string>(diagnostics);
    }

    private static ToolResult<string> Statistics(ArtifactSource source, DiagnosticOutputFormat format)
    {
        return source.Statistics is { } statistics
            ? ToolResults.Success(ValidationReportWriter.FormatStatistics(statistics, format))
            : ToolResults.Failure<string>(source.CompileDiagnostics);
    }

    // The diagnostics artifacts are made for a rule that does not compile too: they are the way to see why.
    private static ToolResult<string> Diagnostics(ArtifactSource source, DiagnosticOutputFormat format)
    {
        DiagnosticDocument document = DiagnosticDocument.For(source.Rule, source.Validation.Diagnostics);
        return ToolResults.Success(DiagnosticWriter.Format([document], format));
    }

    private static ArtifactDefinition Artifact(
        string id,
        string extension,
        string format,
        string description,
        KeyValuePair<string, string>[] options,
        Func<ArtifactSource, ToolResult<string>> render
    )
    {
        return new ArtifactDefinition(id, extension, format, description, options, render);
    }

    private static KeyValuePair<string, string> Option(string name, string value)
    {
        return new KeyValuePair<string, string>(name, value);
    }

    private static string EquationId(string dialect, string wrap, string constants)
    {
        return wrap == "none" ? $"equation-{dialect}-{constants}" : $"equation-{dialect}-{wrap}-{constants}";
    }

    private static string DialectName(EquationDialect dialect)
    {
        foreach ((EquationDialect value, string name, _, _) in Dialects)
        {
            if (value == dialect)
            {
                return name;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(dialect), dialect, "The value is not an equation dialect.");
    }

    private static string WrapName(LatexWrapMode wrap)
    {
        return wrap switch
        {
            LatexWrapMode.None => "none",
            LatexWrapMode.DoubleDollar => "doubledollar",
            LatexWrapMode.MathJaxSafe => "mathjax",
            _ => throw new ArgumentOutOfRangeException(nameof(wrap), wrap, "The value is not a LaTeX envelope."),
        };
    }

    private static string ConstantName(ConstantStyle constants)
    {
        return constants switch
        {
            ConstantStyle.Letters => "letters",
            ConstantStyle.Word => "word",
            ConstantStyle.LogicSymbols => "logicsymbols",
            _ => throw new ArgumentOutOfRangeException(nameof(constants), constants, "The value is not a constant style."),
        };
    }
}
