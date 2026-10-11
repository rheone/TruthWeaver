namespace TruthWeaver.Tool.Core;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using YamlDotNet.Core;

/// <summary>
/// The fallback half of <see cref="RulePatcher"/>: writes the whole span of one rule again, in the document's format and
/// fitted to its place (indentation, flow or block style, string style), and leaves the text around the rule as it is.
/// </summary>
internal static class RuleRewrite
{
    /// <summary>Writes the rule at the loaded rule's path again.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="format">The document format.</param>
    /// <param name="loaded">The rule as loaded from <paramref name="document"/>.</param>
    /// <param name="edited">The edited rule.</param>
    /// <param name="problem">Why the rule cannot be written, when this method returns <see langword="null"/>.</param>
    /// <returns>The new document, or <see langword="null"/>.</returns>
    public static string? TryApply(
        string document,
        RuleFormat format,
        LoadedRule loaded,
        PrintedRule edited,
        out string? problem
    )
    {
        problem = null;
        if (format == RuleFormat.Text)
        {
            return ReplaceText(document, edited.Text);
        }

        if (SourceNodeIndex.Create(document, format).Find(loaded.Path ?? "$") is not { IsShared: false } node)
        {
            problem = "the rule is shared through a YAML anchor or alias, so a change to it would change other values too.";
            return null;
        }

        string newLine = PatchText.NewLineOf(document);
        (int end, string text) = (loaded.Format, format, node.IsFlow) switch
        {
            (RuleFormat.Text, RuleFormat.Json, _) => (node.End, PatchText.Quote(edited.Text)),
            (RuleFormat.Text, _, _) => YamlRuleText(document, node, edited.Text, newLine),
            (_, RuleFormat.Json, _) => (node.End, JsonRule(document, node, JsonNode.Parse(edited.Json)!, newLine)),
            (_, _, true) => (node.End, PatchText.Flow(JsonNode.Parse(edited.Json))),
            _ => YamlBlockRule(document, node, edited.Yaml, newLine),
        };
        return PatchText.Replace(document, [(node.Start, end, text)]);
    }

    /// <summary>Replaces the text of a whole rule text document, between its leading and trailing white space.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="ruleText">The new rule text.</param>
    /// <returns>The new document.</returns>
    public static string ReplaceText(string document, string ruleText)
    {
        int start = 0;
        while (start < document.Length && char.IsWhiteSpace(document[start]))
        {
            start++;
        }

        int end = document.Length;
        while (end > start && char.IsWhiteSpace(document[end - 1]))
        {
            end--;
        }

        return PatchText.Replace(document, [(start, end, ruleText)]);
    }

    // A JSON object: on one line when it was on one line, otherwise indented with the document's indent step and
    // continued at the indentation of the line it starts on.
    private static string JsonRule(string document, SourceNode node, JsonNode rule, string newLine)
    {
        if (document.IndexOf('\n', node.Start, node.End - node.Start) < 0)
        {
            return PatchText.Flow(rule);
        }

        string indent = PatchText.LineIndent(document, node.Start);
        int step = 2;
        int nextLine = document.IndexOf('\n', node.Start) + 1;
        if (nextLine > 0 && nextLine < node.End)
        {
            int inner = PatchText.LineIndent(document, nextLine).Length - indent.Length;
            step = inner > 0 ? inner : step;
        }

        return PatchText.Reindent(PatchText.Indented(rule, step), indent, newLine);
    }

    // A YAML block mapping: the printed YAML, each line after the first indented to the column the mapping starts at. A
    // comment at the end of the rule's last line belongs to that line, so it goes with the rule.
    private static (int End, string Text) YamlBlockRule(string document, SourceNode node, string yaml, string newLine)
    {
        string printed = yaml.TrimEnd();
        if (printed.EndsWith("\n...", StringComparison.Ordinal))
        {
            printed = printed[..^4].TrimEnd();
        }

        string indent = new(' ', node.Start - PatchText.LineStart(document, node.Start));
        int end = node.End;
        int scan = end;
        while (scan < document.Length && document[scan] is ' ' or '\t')
        {
            scan++;
        }

        if (scan < document.Length && document[scan] == '#')
        {
            end = scan;
            while (end < document.Length && document[end] is not '\r' and not '\n')
            {
                end++;
            }
        }

        return (end, PatchText.Reindent(printed, indent, newLine));
    }

    // A YAML scalar of rule text: quoted in the style it had, or, for a block scalar, the same block header with the new
    // lines at the old indentation. A folded block becomes a literal one, as folding would join the lines.
    private static (int End, string Text) YamlRuleText(string document, SourceNode node, string ruleText, string newLine)
    {
        int headerEnd = document.IndexOf('\n', node.Start);
        if (node.Style is not (ScalarStyle.Literal or ScalarStyle.Folded) || headerEnd < 0 || headerEnd >= node.End)
        {
            return (node.End, PatchText.YamlString(ruleText, node.Style));
        }

        string header = document[node.Start..headerEnd].TrimEnd('\r');
        if (header.StartsWith('>'))
        {
            header = "|" + header[1..];
        }

        // The indentation of the first line with content; an empty block takes two spaces more than its key.
        string indent = PatchText.LineIndent(document, node.Start) + "  ";
        for (int line = headerEnd + 1; line < node.End; line = document.IndexOf('\n', line) + 1)
        {
            string lineIndent = PatchText.LineIndent(document, line);
            int content = line + lineIndent.Length;
            if (content < document.Length && document[content] is not '\r' and not '\n')
            {
                indent = lineIndent;
                break;
            }

            if (document.IndexOf('\n', line) < 0)
            {
                break;
            }
        }

        int end = node.End;
        while (end > headerEnd && char.IsWhiteSpace(document[end - 1]))
        {
            end--;
        }

        return (
            end,
            header
                + newLine
                + indent
                + PatchText.Reindent(ruleText.Replace("\r\n", "\n", StringComparison.Ordinal), indent, newLine)
        );
    }
}
