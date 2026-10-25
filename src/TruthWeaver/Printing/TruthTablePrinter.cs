namespace TruthWeaver.Printing;

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;

/// <summary>
/// Prints the truth table of a tree: one row for each assignment of <c>False</c>, <c>Unknown</c> and <c>True</c> (or only
/// <c>False</c> and <c>True</c>) to its distinct terms, and the tree's value for the row. The values come from the
/// analyzer's dual-rail diagram (<see cref="TermValuation"/>), so the table and the compiler's analysis share one
/// definition of every operator. A term is a free variable here: the table never calls a predicate or reads a data source.
/// </summary>
/// <remarks>
/// The rows are in a stable order: the first term changes slowest, and the values run <c>False</c>, <c>Unknown</c>,
/// <c>True</c>. That order is a layout choice and not a number. The columns follow the first occurrence of each term,
/// depth first and left to right, as the letters of <see cref="EquationPrinter"/> do.
/// </remarks>
internal static class TruthTablePrinter
{
    private const string ResultHeader = "Result";

    private static readonly TruthValue[] ClassicalStates = [TruthValue.False, TruthValue.True];
    private static readonly TruthValue[] K3States = [TruthValue.False, TruthValue.Unknown, TruthValue.True];

    /// <summary>Checks the size of the table, and writes it unless the size is refused.</summary>
    /// <param name="root">The tree.</param>
    /// <param name="options">The table options.</param>
    /// <param name="writer">The writer. Nothing is written when this method returns <see langword="false"/>.</param>
    /// <param name="diagnostics">A <c>TRE0034</c> warning for a raised cap, a <c>TRE0035</c> error for a refusal, or empty.</param>
    /// <returns><see langword="true"/> when the table was written.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value.</exception>
    public static bool TryPrint(
        Expression root,
        TruthTableOptions options,
        TextWriter writer,
        out IReadOnlyList<Diagnostic> diagnostics
    )
    {
        if (!Enum.IsDefined(options.Format))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Format, "Unhandled truth table format.");
        }

        int termCount = Analyzer.DistinctTerms(root).Count;
        if (termCount > options.TermLimit)
        {
            diagnostics =
            [
                Diagnostic.Error(
                    DiagnosticCodes.TruthTableTooLarge,
                    $"The truth table was not printed: the rule has {termCount} distinct terms, more than the limit of {options.TermLimit}.",
                    SourceSpan.None
                ),
            ];
            return false;
        }

        TruthValue[] states = options.Classical ? ClassicalStates : K3States;
        long rowCount = 1;
        for (int i = 0; i < termCount; i++)
        {
            // Saturate: the count is only a message and a JSON field, and a caller may set a very large limit.
            rowCount = rowCount > long.MaxValue / states.Length ? long.MaxValue : rowCount * states.Length;
        }

        string rows = rowCount.ToString("N0", CultureInfo.InvariantCulture);
        if (termCount > TruthTableOptions.DefaultTermCap)
        {
            if (!options.AllowLargerTable)
            {
                diagnostics =
                [
                    Diagnostic.Error(
                        DiagnosticCodes.TruthTableTooLarge,
                        $"The truth table was not printed: the rule has {termCount} distinct terms, more than the cap of {TruthTableOptions.DefaultTermCap} ({rows} rows). Set AllowLargerTable to print it, up to {options.TermLimit} terms.",
                        SourceSpan.None
                    ),
                ];
                return false;
            }

            diagnostics =
            [
                Diagnostic.Warning(
                    DiagnosticCodes.TruthTableCapRaised,
                    $"The truth table has {termCount} distinct terms, more than the default cap of {TruthTableOptions.DefaultTermCap}: it has {rows} rows.",
                    SourceSpan.None
                ),
            ];
        }
        else
        {
            diagnostics = [];
        }

        TermValuation valuation;
        try
        {
            valuation = Analyzer.CreateValuation(root);
        }
        catch (BddNodeBudgetExceededException)
        {
            // Within the term limit, the shared BDD for this term ordering and rule shape still grew past the
            // analyzer's internal node budget (ticket 24): refuse the table the same way an over-cap term count does,
            // rather than hanging or allocating unboundedly.
            const string message =
                "The truth table was not printed: building its value table's decision diagram for this rule would exceed the analyzer's internal node budget.";
            diagnostics = [Diagnostic.Error(DiagnosticCodes.TruthTableTooLarge, message, SourceSpan.None)];
            return false;
        }

        LineWriter lines = new(writer);
        switch (options.Format)
        {
            case TruthTableFormat.Markdown:
                WriteMarkdown(lines, options, valuation, states);
                break;
            case TruthTableFormat.Csv:
                WriteCsv(lines, options, valuation, states);
                break;
            case TruthTableFormat.Json:
                WriteJson(lines, options, valuation, states, rowCount);
                break;
            case TruthTableFormat.Html:
                WriteHtml(lines, options, valuation, states);
                break;
            case TruthTableFormat.Latex:
                WriteLatex(lines, options, valuation, states);
                break;
            default:
                throw new InvalidOperationException($"Unhandled truth table format '{options.Format}'.");
        }

        return true;
    }

    private static void WriteMarkdown(LineWriter lines, TruthTableOptions options, TermValuation valuation, TruthValue[] states)
    {
        string[] headers = [.. Headers(valuation, options, EquationTokens.Unicode).Select(MarkdownEscape), ResultHeader];
        lines.Line("| " + string.Join(" | ", headers) + " |");
        lines.Line("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");

        string[] cells = CellTexts(value => MarkdownEscape(options.Constants.Text(value)));
        foreach ((TruthValue[] assignment, TruthValue result) in Rows(valuation, states))
        {
            lines.Line("| " + string.Join(" | ", RowCells(cells, assignment, result)) + " |");
        }
    }

    private static void WriteCsv(LineWriter lines, TruthTableOptions options, TermValuation valuation, TruthValue[] states)
    {
        lines.Line(string.Join(',', Headers(valuation, options, EquationTokens.Unicode).Append(ResultHeader).Select(CsvField)));

        string[] cells = CellTexts(value => CsvField(options.Constants.Text(value)));
        foreach ((TruthValue[] assignment, TruthValue result) in Rows(valuation, states))
        {
            lines.Line(string.Join(',', RowCells(cells, assignment, result)));
        }
    }

    private static void WriteJson(
        LineWriter lines,
        TruthTableOptions options,
        TermValuation valuation,
        TruthValue[] states,
        long rowCount
    )
    {
        lines.Line("{");
        lines.Line($"  \"logic\": {JsonString(options.Classical ? "classical" : "K3")},");

        string[] headers = [.. Headers(valuation, options, EquationTokens.Unicode)];
        if (headers.Length == 0)
        {
            lines.Line("  \"terms\": [],");
        }
        else
        {
            lines.Line("  \"terms\": [");
            for (int i = 0; i < headers.Length; i++)
            {
                string term = EquationTokens.Unicode.FormatTerm(valuation.Terms[i], true);
                string comma = i < headers.Length - 1 ? "," : string.Empty;
                lines.Line($"    {{ \"name\": {JsonString(headers[i])}, \"term\": {JsonString(term)} }}{comma}");
            }

            lines.Line("  ],");
        }

        lines.Line("  \"rows\": [");
        string[] cells = CellTexts(value => JsonString(options.Constants.Text(value)));
        long written = 0;
        foreach ((TruthValue[] assignment, TruthValue result) in Rows(valuation, states))
        {
            written++;
            string comma = written < rowCount ? "," : string.Empty;
            string values = string.Join(", ", assignment.Select(value => cells[CellIndex(value)]));
            lines.Line($"    {{ \"values\": [{values}], \"result\": {cells[CellIndex(result)]} }}{comma}");
        }

        lines.Line("  ]");
        lines.Line("}");
    }

    private static void WriteHtml(LineWriter lines, TruthTableOptions options, TermValuation valuation, TruthValue[] states)
    {
        lines.Line("<table class=\"tw-truth-table\">");
        lines.Line("<thead>");

        StringBuilder header = new("<tr>");
        string[] names = [.. Headers(valuation, options, EquationTokens.Unicode)];
        for (int i = 0; i < names.Length; i++)
        {
            // A letter hides the term, so the full call goes in the title attribute (as PrintHtml does).
            string title = options.SimpleVariables
                ? $" title=\"{HtmlText.Escape(EquationTokens.Unicode.FormatTerm(valuation.Terms[i], true))}\""
                : string.Empty;
            header.Append($"<th{title}>{HtmlText.Escape(names[i])}</th>");
        }

        header.Append($"<th>{ResultHeader}</th></tr>");
        lines.Line(header.ToString());
        lines.Line("</thead>");
        lines.Line("<tbody>");

        string[] cells = CellTexts(value => HtmlText.Escape(options.Constants.Text(value)));
        foreach ((TruthValue[] assignment, TruthValue result) in Rows(valuation, states))
        {
            StringBuilder row = new("<tr>");
            foreach (TruthValue value in assignment)
            {
                row.Append($"<td class=\"{HtmlClass(value)}\">{cells[CellIndex(value)]}</td>");
            }

            row.Append($"<td class=\"{HtmlClass(result)} tw-result\">{cells[CellIndex(result)]}</td></tr>");
            lines.Line(row.ToString());
        }

        lines.Line("</tbody>");
        lines.Line("</table>");
    }

    private static void WriteLatex(LineWriter lines, TruthTableOptions options, TermValuation valuation, TruthValue[] states)
    {
        EquationTokens tokens = EquationTokens.Latex(LatexWrapMode.None);
        int termCount = valuation.Terms.Count;

        // A rule with no term has only the result column, so the rule between the term columns and it is left out.
        string columns = new string('c', termCount) + (termCount > 0 ? "|c" : "c");
        lines.Line($"\\begin{{tabular}}{{{columns}}}");
        lines.Line(
            string.Join(" & ", Headers(valuation, options, tokens).Select(header => $"${header}$").Append(ResultHeader))
                + " \\\\"
        );
        lines.Line("\\hline");

        string[] cells = CellTexts(value => $"${tokens.FormatConstant(value, options.Constants)}$");
        foreach ((TruthValue[] assignment, TruthValue result) in Rows(valuation, states))
        {
            lines.Line(string.Join(" & ", RowCells(cells, assignment, result)) + " \\\\");
        }

        lines.Line("\\end{tabular}");
    }

    /// <summary>The column headers of the term columns: a letter in simple-variable mode, otherwise the term.</summary>
    private static IEnumerable<string> Headers(TermValuation valuation, TruthTableOptions options, EquationTokens tokens)
    {
        for (int i = 0; i < valuation.Terms.Count; i++)
        {
            if (options.SimpleVariables)
            {
                yield return tokens.FormatVariable(
                    EquationPrinter.VariableLetters[i % EquationPrinter.VariableLetters.Length],
                    i / EquationPrinter.VariableLetters.Length
                );
            }
            else
            {
                yield return tokens.FormatTerm(valuation.Terms[i], options.ShowArgumentValues);
            }
        }
    }

    /// <summary>
    /// Enumerates the rows. The first term changes slowest, as in <c>K3Oracle.Assignments</c>. The assignment array is the
    /// same instance for every row, so a caller must use it before it asks for the next row.
    /// </summary>
    private static IEnumerable<(TruthValue[] Assignment, TruthValue Result)> Rows(TermValuation valuation, TruthValue[] states)
    {
        int termCount = valuation.Terms.Count;
        int[] digits = new int[termCount];
        TruthValue[] assignment = new TruthValue[termCount];
        for (int i = 0; i < termCount; i++)
        {
            assignment[i] = states[0];
        }

        while (true)
        {
            yield return (assignment, valuation.ValueAt(assignment));

            // Count up like an odometer; a carry out of the first digit ends the table.
            int position = termCount - 1;
            while (position >= 0)
            {
                digits[position]++;
                if (digits[position] < states.Length)
                {
                    assignment[position] = states[digits[position]];
                    break;
                }

                digits[position] = 0;
                assignment[position] = states[0];
                position--;
            }

            if (position < 0)
            {
                yield break;
            }
        }
    }

    /// <summary>The finished text of the three values in a cell, indexed by <see cref="CellIndex"/>.</summary>
    private static string[] CellTexts(Func<TruthValue, string> format)
    {
        return [format(TruthValue.False), format(TruthValue.Unknown), format(TruthValue.True)];
    }

    private static IEnumerable<string> RowCells(string[] cells, TruthValue[] assignment, TruthValue result)
    {
        return assignment.Append(result).Select(value => cells[CellIndex(value)]);
    }

    private static int CellIndex(TruthValue value)
    {
        return value switch
        {
            TruthValue.False => 0,
            TruthValue.Unknown => 1,
            TruthValue.True => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unhandled truth value."),
        };
    }

    private static string HtmlClass(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => "tw-true",
            TruthValue.False => "tw-false",
            _ => "tw-unknown",
        };
    }

    /// <summary>Escapes the characters that Markdown reads as markup, and turns a line break into a space (a table row is one line).</summary>
    private static string MarkdownEscape(string value)
    {
        StringBuilder builder = new(value.Length + 4);
        foreach (char c in value)
        {
            switch (c)
            {
                case '\r' or '\n':
                    builder.Append(' ');
                    break;
                case '\\' or '|' or '`' or '*' or '_' or '<' or '>' or '[' or ']':
                    builder.Append('\\').Append(c);
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>Quotes a CSV field when it has a comma, a quote or a line break (RFC 4180).</summary>
    private static string CsvField(string value)
    {
        return value.AsSpan().IndexOfAny(",\"\r\n") >= 0
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static string JsonString(string value)
    {
        return $"\"{JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(value)}\"";
    }

    /// <summary>Writes lines separated by a line feed, with none after the last, as the other printers do.</summary>
    private sealed class LineWriter(TextWriter writer)
    {
        private bool first = true;

        public void Line(string text)
        {
            if (!this.first)
            {
                writer.Write('\n');
            }

            this.first = false;
            writer.Write(text);
        }
    }
}
