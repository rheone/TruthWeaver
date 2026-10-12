namespace TruthWeaver.Tool.Core;

using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;

/// <summary>
/// Writes the Markdown report of a rule: the source, the rule text, the equation as MathJax, a Mermaid diagram, the truth
/// table (within the term cap, otherwise a note), the statistics and the analysis findings. GitHub renders the equation
/// and the diagram. The report is deterministic: it names the input file but not its directory, and holds no time.
/// </summary>
public static class RuleReport
{
    /// <summary>Writes the report of a loaded rule.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="options">
    /// The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>.
    /// <see cref="ExportOptions.AllowLargerTruthTable"/> puts a truth table over the cap in the report.
    /// </param>
    /// <returns>The Markdown text with line feeds and no final line feed, or no text and the compile diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> Format(LoadedRule rule, ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return Format(new ArtifactSource(rule, options ?? ExportOptions.Default));
    }

    /// <summary>Writes the report from a compiled source.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The Markdown text, or the compile diagnostics.</returns>
    internal static ToolResult<string> Format(ArtifactSource source)
    {
        if (source.Compiled is not { } compiled || source.Statistics is not { } statistics)
        {
            return ToolResults.Failure<string>(source.CompileDiagnostics);
        }

        List<Diagnostic> diagnostics = [];
        StringBuilder report = new();
        report.Append("# Rule report\n\n");
        report.Append(Source(source.Rule)).Append("\n\n");

        report.Append("## Rule\n\n");
        report.Append(Fenced("text", compiled.PrintRuleText(new RuleTextOptions { Width = ArtifactCatalog.PrettyWidth })));

        report.Append("\n\n## Equation\n\n");
        report.Append(
            compiled.PrintEquation(
                new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.MathJaxSafe }
            )
        );

        report.Append("\n\n## Diagram\n\n");
        report.Append(Fenced("mermaid", compiled.PrintMermaid()));

        report.Append("\n\n## Truth table\n\n");
        TruthTableOptions table = new() { AllowLargerTable = source.Options.AllowLargerTruthTable };
        if (compiled.TryPrintTruthTable(table, out string? text, out IReadOnlyList<Diagnostic> tableDiagnostics))
        {
            report.Append("The Strong K3 truth table: every combination of `F`, `U` and `T` for the terms.\n\n");
            report.Append(text);
            diagnostics.AddRange(tableDiagnostics);
        }
        else
        {
            report.Append(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The truth table is left out. The rule has {statistics.DistinctTermCount} distinct terms and the table would have {statistics.TruthTableRows} rows, "
                )
            );
            report.Append(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"more than the cap of {TruthTableOptions.DefaultTermCap} terms allows."
                )
            );
        }

        report.Append("\n\n## Statistics\n\n");
        report.Append(StatisticsTable(statistics));

        report.Append("\n\n## Analysis\n\n");
        report.Append(Analysis(source.Validation[ValidationLevels.Analysis]));
        return ToolResults.Success(report.ToString(), diagnostics);
    }

    private static string Source(LoadedRule rule)
    {
        string origin = rule.Source.Origin == "-" ? "standard input" : $"`{Path.GetFileName(rule.Source.Origin)}`";
        string path = rule.Path is null ? string.Empty : $", path `{rule.Path}`";
        return $"Source: {origin}{path}, {rule.Format.ToString().ToLowerInvariant()}, `{ArtifactExporter.HashOf(rule)}`.";
    }

    private static string StatisticsTable(RuleStatistics statistics)
    {
        List<(string Name, string Value)> rows =
        [
            ("Nodes", Number(statistics.NodeCount)),
            ("Depth", Number(statistics.Depth)),
            ("Terms", $"{Number(statistics.TermCount)} ({Number(statistics.DistinctTermCount)} distinct)"),
            ("Predicates", Codes(statistics.Predicates)),
            ("Operators", List(statistics.OperatorHistogram.Select(o => $"`{o.Key}` {Number(o.Value)}"))),
            ("Variables", List(statistics.Variables.Select(v => $"`from(\"{v.Source}\", \"{v.Query}\")`"))),
            ("Data sources", Codes(statistics.DataSources)),
            ("BDD nodes", statistics.BddNodeCount is { } bdd ? Number(bdd) : "not computed"),
            ("Constancy", statistics.Constancy.ToString()),
            (
                "Truth table",
                $"{statistics.TruthTableRows.ToString(CultureInfo.InvariantCulture)} rows, "
                    + $"{statistics.ClassicalTruthTableRows.ToString(CultureInfo.InvariantCulture)} classical rows, "
                    + (statistics.TruthTableWithinCap ? "within the cap" : "over the cap")
            ),
        ];
        return "| Statistic | Value |\n| --- | --- |\n"
            + string.Join("\n", rows.Select(row => $"| {row.Name} | {EscapeCell(row.Value)} |"));
    }

    private static string Analysis(ValidationLevelReport level)
    {
        if (level.Diagnostics.Count == 0)
        {
            return "The analysis found nothing to report.";
        }

        return string.Join(
            "\n",
            level.Diagnostics.Select(d =>
                $"- {d.Severity} `{d.Code}`" + (d.Path is null ? string.Empty : $" at `{d.Path}`") + ": " + d.Message
            )
        );
    }

    // A code fence longer than any run of backticks in the text, so the text cannot close it.
    private static string Fenced(string language, string text)
    {
        int longest = 0;
        int run = 0;
        foreach (char c in text)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        string fence = new('`', Math.Max(3, longest + 1));
        return $"{fence}{language}\n{text.ReplaceLineEndings("\n").TrimEnd('\n')}\n{fence}";
    }

    private static string Number(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Codes(IEnumerable<string> values)
    {
        return List(values.Select(value => $"`{value}`"));
    }

    private static string List(IEnumerable<string> values)
    {
        string joined = string.Join(", ", values);
        return joined.Length == 0 ? "none" : joined;
    }

    // A pipe in a cell would end the cell.
    private static string EscapeCell(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
