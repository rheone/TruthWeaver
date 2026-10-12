namespace TruthWeaver.Evaluation;

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

/// <summary>Writes a <see cref="RuleCoverage"/> as a Markdown report or as JSON, with line feeds on every platform.</summary>
internal static class RuleCoveragePrinter
{
    /// <summary>Writes the Markdown report.</summary>
    /// <param name="coverage">The coverage.</param>
    /// <param name="writer">The writer.</param>
    public static void WriteMarkdown(RuleCoverage coverage, TextWriter writer)
    {
        StringBuilder text = new();
        text.Append("# Rule coverage\n\n");
        text.Append(CultureInfo.InvariantCulture, $"Rule: {Code(coverage.Rule)}\n\n");
        text.Append(
            CultureInfo.InvariantCulture,
            $"Decisions: {coverage.DecisionCount} (True {coverage.TrueCount}, False {coverage.FalseCount}, Unknown {coverage.UnknownCount})\n\n"
        );

        text.Append("## Terms\n\n");
        text.Append("| Term | Evaluated | True | False | Unknown | Faults | Decided |\n");
        text.Append("| --- | ---: | ---: | ---: | ---: | ---: | ---: |\n");
        foreach (TermCoverage term in coverage.Terms)
        {
            text.Append(
                CultureInfo.InvariantCulture,
                $"| {Code(term.Term)} | {term.EvaluatedCount} | {term.TrueCount} | {term.FalseCount} | {term.UnknownCount} | {term.FaultCount} | {term.DecidedCount} |\n"
            );
        }

        text.Append('\n');
        text.Append("Never decided: ").Append(List(coverage.Terms.Where(term => term.NeverDecided))).Append("\n\n");
        text.Append("Always Unknown: ").Append(List(coverage.Terms.Where(term => term.AlwaysUnknown))).Append("\n\n");
        text.Append("Faulted: ").Append(List(coverage.Terms.Where(term => term.FaultCount > 0))).Append("\n\n");

        text.Append("## Branches that never decided\n\n");
        IReadOnlyList<NodeCoverage> operators = coverage.OperatorsWithUndecidedOperands;
        if (operators.Count == 0)
        {
            text.Append("None.\n");
        }
        else
        {
            text.Append("| Operator | Operator path | Branch | Branch path |\n");
            text.Append("| --- | --- | --- | --- |\n");
            foreach (NodeCoverage node in operators)
            {
                foreach (NodeCoverage branch in node.UndecidedOperands)
                {
                    text.Append(
                        CultureInfo.InvariantCulture,
                        $"| {Code(node.Text)} | {Code(node.Path)} | {Code(branch.Text)} | {Code(branch.Path)} |\n"
                    );
                }
            }
        }

        writer.Write(text.ToString());
    }

    /// <summary>Writes the JSON document.</summary>
    /// <param name="coverage">The coverage.</param>
    /// <param name="writer">The writer.</param>
    public static void WriteJson(RuleCoverage coverage, TextWriter writer)
    {
        using MemoryStream stream = new();
        using (
            Utf8JsonWriter json = new(
                stream,
                new JsonWriterOptions
                {
                    Indented = true,
                    NewLine = "\n",
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                }
            )
        )
        {
            json.WriteStartObject();
            json.WriteString("rule", coverage.Rule);
            json.WriteNumber("decisions", coverage.DecisionCount);
            json.WriteStartObject("outcomes");
            json.WriteNumber("true", coverage.TrueCount);
            json.WriteNumber("false", coverage.FalseCount);
            json.WriteNumber("unknown", coverage.UnknownCount);
            json.WriteEndObject();

            json.WriteStartArray("terms");
            foreach (TermCoverage term in coverage.Terms)
            {
                json.WriteStartObject();
                json.WriteString("term", term.Term);
                json.WriteNumber("evaluated", term.EvaluatedCount);
                json.WriteNumber("true", term.TrueCount);
                json.WriteNumber("false", term.FalseCount);
                json.WriteNumber("unknown", term.UnknownCount);
                json.WriteNumber("faults", term.FaultCount);
                json.WriteNumber("decided", term.DecidedCount);
                json.WriteBoolean("neverDecided", term.NeverDecided);
                json.WriteBoolean("alwaysUnknown", term.AlwaysUnknown);
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("nodes");
            foreach (NodeCoverage node in coverage.Nodes)
            {
                json.WriteStartObject();
                json.WriteString("path", node.Path);
                json.WriteString("text", node.Text);
                json.WriteNumber("evaluated", node.EvaluatedCount);
                json.WriteNumber("notEvaluated", node.NotEvaluatedCount);
                json.WriteNumber("true", node.TrueCount);
                json.WriteNumber("false", node.FalseCount);
                json.WriteNumber("unknown", node.UnknownCount);
                json.WriteNumber("decided", node.DecidedCount);
                WritePaths(json, "operands", node.Operands);
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("undecidedBranches");
            foreach (NodeCoverage node in coverage.OperatorsWithUndecidedOperands)
            {
                json.WriteStartObject();
                json.WriteString("path", node.Path);
                json.WriteString("text", node.Text);
                WritePaths(json, "branches", node.UndecidedOperands);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        writer.Write(Encoding.UTF8.GetString(stream.ToArray()));
        writer.Write('\n');
    }

    private static void WritePaths(Utf8JsonWriter json, string name, IEnumerable<NodeCoverage> nodes)
    {
        json.WriteStartArray(name);
        foreach (NodeCoverage node in nodes)
        {
            json.WriteStringValue(node.Path);
        }

        json.WriteEndArray();
    }

    private static string List(IEnumerable<TermCoverage> terms)
    {
        string[] names = [.. terms.Select(term => Code(term.Term))];
        return names.Length == 0 ? "none" : string.Join(", ", names);
    }

    // A backtick inside the text would end the code span early, so the fence grows to one more backtick than the text holds.
    private static string Code(string text)
    {
        int longest = 0;
        int run = 0;
        foreach (char c in text)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        string fence = new('`', longest + 1);
        string padding = longest > 0 ? " " : string.Empty;
        return $"{fence}{padding}{text.Replace("|", "\\|", StringComparison.Ordinal)}{padding}{fence}";
    }
}
