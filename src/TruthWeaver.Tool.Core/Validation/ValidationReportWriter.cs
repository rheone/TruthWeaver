namespace TruthWeaver.Tool.Core.Validation;

using System.Text.Json;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Reports;

/// <summary>
/// Writes a <see cref="ValidationReport"/>: the outcome of each level, the statistics and the diagnostics, as text or
/// JSON, or the diagnostics alone as SARIF 2.1.0 (SARIF has no place for statistics). The output is deterministic.
/// </summary>
public static class ValidationReportWriter
{
    /// <summary>Writes a validation report to a string.</summary>
    /// <param name="report">The report.</param>
    /// <param name="format">The output format.</param>
    /// <returns>The output, with line feeds and no final line feed. Write it with <see cref="OutputWriter"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined value.</exception>
    public static string Format(ValidationReport report, DiagnosticOutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        DiagnosticDocument document = DiagnosticDocument.For(report.Rule, report.Diagnostics);
        return format switch
        {
            DiagnosticOutputFormat.Text => FormatText(report, document),
            DiagnosticOutputFormat.Json => DiagnosticWriter.WriteJson(json => WriteJson(json, report, document)),
            DiagnosticOutputFormat.Sarif => DiagnosticWriter.Format([document], DiagnosticOutputFormat.Sarif),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "The value is not a defined output format."),
        };
    }

    /// <summary>
    /// Writes the statistics of a rule alone, as the statistics section of a report: text lines (<c>nodes: 9</c>), or one
    /// JSON object with the members of the <c>statistics</c> object of the JSON report.
    /// </summary>
    /// <param name="statistics">The statistics.</param>
    /// <param name="format"><see cref="DiagnosticOutputFormat.Text"/> or <see cref="DiagnosticOutputFormat.Json"/>.</param>
    /// <returns>The output, with line feeds and no final line feed. Write it with <see cref="OutputWriter"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="statistics"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="format"/> is <see cref="DiagnosticOutputFormat.Sarif"/>, which has no place for statistics, or is not
    /// a defined value.
    /// </exception>
    public static string FormatStatistics(RuleStatistics statistics, DiagnosticOutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        return format switch
        {
            DiagnosticOutputFormat.Text => string.Join("\n", StatisticsLines(statistics)),
            DiagnosticOutputFormat.Json => DiagnosticWriter.WriteJson(json => WriteStatisticsObject(json, statistics)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Statistics are written as text or JSON only; SARIF has no place for them."
            ),
        };
    }

    /// <summary>Gets the name of a level as the JSON output writes it, such as <c>treeSchema</c>.</summary>
    /// <param name="level">One flag of <see cref="ValidationLevels"/>.</param>
    /// <returns>The name.</returns>
    internal static string LevelName(ValidationLevels level)
    {
        return CamelCase(level.ToString());
    }

    private static string FormatText(ValidationReport report, DiagnosticDocument document)
    {
        List<string> lines = [report.Rule.Path is null ? document.Origin : $"{document.Origin} {report.Rule.Path}"];
        foreach (ValidationLevelReport level in report.Levels)
        {
            string status = level.Status switch
            {
                ValidationStatus.Passed => "passed",
                ValidationStatus.PassedWithWarnings => "passed with warnings",
                ValidationStatus.Failed => "failed",
                ValidationStatus.Skipped => "skipped: " + level.Reason,
                _ => "off",
            };
            lines.Add($"  {LevelName(level.Level)}: {status}");
        }

        if (report.Statistics is { } statistics)
        {
            lines.Add("statistics");
            lines.AddRange(StatisticsLines(statistics).Select(line => "  " + line));
        }

        string diagnostics = DiagnosticWriter.FormatText([document]);
        if (diagnostics.Length > 0)
        {
            lines.Add(diagnostics);
        }

        return string.Join("\n", lines);
    }

    private static void WriteJson(Utf8JsonWriter json, ValidationReport report, DiagnosticDocument document)
    {
        json.WriteStartObject();
        json.WriteString("origin", document.Origin);
        if (report.Rule.Path is { } path)
        {
            json.WriteString("rulePath", path);
        }

        json.WriteString("format", CamelCase(report.Rule.Format.ToString()));
        json.WriteStartArray("levels");
        foreach (ValidationLevelReport level in report.Levels)
        {
            json.WriteStartObject();
            json.WriteString("level", LevelName(level.Level));
            json.WriteString("status", CamelCase(level.Status.ToString()));
            if (level.Reason is not null)
            {
                json.WriteString("reason", level.Reason);
            }

            json.WriteNumber("diagnostics", level.Diagnostics.Count);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        if (report.Statistics is { } statistics)
        {
            WriteStatistics(json, statistics);
        }

        json.WriteStartArray("diagnostics");
        foreach (TruthWeaver.Diagnostics.Diagnostic diagnostic in document.Diagnostics)
        {
            DiagnosticWriter.WriteDiagnosticObject(json, document, diagnostic);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteStatistics(Utf8JsonWriter json, RuleStatistics statistics)
    {
        json.WritePropertyName("statistics");
        WriteStatisticsObject(json, statistics);
    }

    private static void WriteStatisticsObject(Utf8JsonWriter json, RuleStatistics statistics)
    {
        json.WriteStartObject();
        json.WriteNumber("nodeCount", statistics.NodeCount);
        json.WriteNumber("depth", statistics.Depth);
        json.WriteNumber("termCount", statistics.TermCount);
        json.WriteNumber("distinctTermCount", statistics.DistinctTermCount);
        WriteStrings(json, "predicates", statistics.Predicates);
        json.WriteStartObject("operators");
        foreach (KeyValuePair<string, int> entry in statistics.OperatorHistogram)
        {
            json.WriteNumber(entry.Key, entry.Value);
        }

        json.WriteEndObject();
        json.WriteStartArray("variables");
        foreach (VariableUse variable in statistics.Variables)
        {
            json.WriteStartObject();
            json.WriteString("from", variable.Source);
            json.WriteString("query", variable.Query);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        WriteStrings(json, "dataSources", statistics.DataSources);
        if (statistics.BddNodeCount is { } bdd)
        {
            json.WriteNumber("bddNodeCount", bdd);
        }
        else
        {
            json.WriteNull("bddNodeCount");
        }

        json.WriteString("constancy", CamelCase(statistics.Constancy.ToString()));

        // The writer has no BigInteger overload, so the exact digits of a row count are written as a raw number.
        json.WritePropertyName("truthTableRows");
        json.WriteRawValue(statistics.TruthTableRows.ToString(CultureInfo.InvariantCulture));
        json.WritePropertyName("classicalTruthTableRows");
        json.WriteRawValue(statistics.ClassicalTruthTableRows.ToString(CultureInfo.InvariantCulture));
        json.WriteBoolean("truthTableWithinCap", statistics.TruthTableWithinCap);
        json.WriteEndObject();
    }

    // The statistics as text lines, without indentation: the statistics section of the text report.
    private static IEnumerable<string> StatisticsLines(RuleStatistics statistics)
    {
        yield return $"nodes: {statistics.NodeCount}";
        yield return $"depth: {statistics.Depth}";
        yield return $"terms: {statistics.TermCount} ({statistics.DistinctTermCount} distinct)";
        yield return $"predicates: {List(statistics.Predicates)}";
        yield return $"operators: {List(statistics.OperatorHistogram.Select(o => $"{o.Key} {o.Value.ToString(CultureInfo.InvariantCulture)}"))}";
        yield return $"variables: {List(statistics.Variables.Select(v => $"from(\"{v.Source}\", \"{v.Query}\")"))}";
        yield return $"data sources: {List(statistics.DataSources)}";
        yield return $"BDD nodes: {statistics.BddNodeCount?.ToString(CultureInfo.InvariantCulture) ?? "not computed"}";
        yield return $"constancy: {CamelCase(statistics.Constancy.ToString())}";
        yield return $"truth table: {statistics.TruthTableRows} rows, {statistics.ClassicalTruthTableRows} classical rows, "
            + (statistics.TruthTableWithinCap ? "within the cap" : "over the cap");
    }

    private static void WriteStrings(Utf8JsonWriter json, string name, IEnumerable<string> values)
    {
        json.WriteStartArray(name);
        foreach (string value in values)
        {
            json.WriteStringValue(value);
        }

        json.WriteEndArray();
    }

    private static string List(IEnumerable<string> values)
    {
        string joined = string.Join(", ", values);
        return joined.Length == 0 ? "none" : joined;
    }

    private static string CamelCase(string name)
    {
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
