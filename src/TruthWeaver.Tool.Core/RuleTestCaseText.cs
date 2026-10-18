namespace TruthWeaver.Tool.Core;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Edits the text of a rule test file (YAML) one case at a time: it adds, replaces and removes a case, reads the cases back
/// and swaps the rule of the file. The rest of the file stays as it is, with its comments, its other keys and the other
/// cases. The format is the one that <c>truthweaver test</c> and <c>RuleTestFile</c> in <c>TruthWeaver.Testing</c> read
/// (see <c>docs/rule-tests.md</c>); this class does not depend on that package.
/// </summary>
/// <remarks>
/// The cases must be a block list (<c>- name: ...</c>), not a flow list (<c>[...]</c>). A case that is replaced loses the
/// keys this class does not write: its <c>trace</c> expectation, its <c>variableFiles</c> and its comments.
/// </remarks>
public static class RuleTestCaseText
{
    private const int StepIndent = 2;

    /// <summary>Makes the text of a new rule test file with one case.</summary>
    /// <param name="ruleText">The rule, as rule text, written to the file as an inline <c>rule</c>.</param>
    /// <param name="firstCase">The first case.</param>
    /// <returns>The text, ending with a line feed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ruleText"/> or <paramref name="firstCase"/> is <see langword="null"/>.</exception>
    public static string NewFile(string ruleText, RuleTestCaseSpec firstCase)
    {
        ArgumentNullException.ThrowIfNull(ruleText);
        ArgumentNullException.ThrowIfNull(firstCase);

        List<string> lines = [.. RuleBlock(ruleText), "cases:", .. Emit(firstCase, StepIndent)];
        return string.Join('\n', lines) + "\n";
    }

    /// <summary>Reads the cases of a file.</summary>
    /// <param name="fileText">The text of the file.</param>
    /// <param name="baseDirectory">The folder that a <c>variableFiles</c> path is relative to, or <see langword="null"/> for the current folder.</param>
    /// <returns>The cases, or the <see cref="ToolDiagnosticCodes.InvalidRuleTestFile"/> errors. The <c>trace</c> expectation is not read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileText"/> is <see langword="null"/>.</exception>
    public static ToolResult<IReadOnlyList<RuleTestCaseSpec>> ReadCases(string fileText, string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(fileText);

        if (!Parse(fileText).TryGetValue(out Layout? layout, out EquatableArray<Diagnostic> parseDiagnostics))
        {
            return ToolResults.Failure<IReadOnlyList<RuleTestCaseSpec>>(parseDiagnostics);
        }

        List<RuleTestCaseSpec> cases = [];
        List<Diagnostic> diagnostics = [];
        for (int i = 0; i < layout.Items.Count; i++)
        {
            string path = $"$.cases[{i}]";
            RuleTestCaseSpec? spec = ReadCase(layout.Items[i], path, baseDirectory, diagnostics);
            if (spec is not null)
            {
                cases.Add(spec);
            }
        }

        return diagnostics.Count > 0
            ? ToolResults.Failure<IReadOnlyList<RuleTestCaseSpec>>(diagnostics)
            : ToolResults.Success<IReadOnlyList<RuleTestCaseSpec>>(cases);
    }

    /// <summary>Adds a case at the end of the cases, or replaces the case that has the same name.</summary>
    /// <param name="fileText">The text of the file.</param>
    /// <param name="spec">The case.</param>
    /// <returns>The new text, or the errors when the file is not usable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileText"/> or <paramref name="spec"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> AddOrReplaceCase(string fileText, RuleTestCaseSpec spec)
    {
        ArgumentNullException.ThrowIfNull(fileText);
        ArgumentNullException.ThrowIfNull(spec);

        if (!Parse(fileText).TryGetValue(out Layout? layout, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<string>(diagnostics);
        }

        List<string> lines = layout.Lines;
        List<string> block = Emit(spec, layout.DashColumn);
        Chunk? existing = layout.Chunks.FirstOrDefault(c => c.Name == spec.Name);
        if (existing is not null)
        {
            lines.RemoveRange(existing.Start, existing.End - existing.Start);
            lines.InsertRange(existing.Start, block);
        }
        else
        {
            lines.InsertRange(layout.Chunks[^1].End, block);
        }

        return Checked(lines);
    }

    /// <summary>Removes the case that has a name.</summary>
    /// <param name="fileText">The text of the file.</param>
    /// <param name="name">The name of the case.</param>
    /// <returns>The new text, or the errors when the file is not usable, has no such case, or has no other case.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileText"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> RemoveCase(string fileText, string name)
    {
        ArgumentNullException.ThrowIfNull(fileText);
        ArgumentNullException.ThrowIfNull(name);

        if (!Parse(fileText).TryGetValue(out Layout? layout, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<string>(diagnostics);
        }

        Chunk? existing = layout.Chunks.FirstOrDefault(c => c.Name == name);
        if (existing is null)
        {
            return Invalid<string>($"The file has no case named '{name}'.", "$.cases");
        }

        if (layout.Chunks.Count == 1)
        {
            return Invalid<string>("A rule test file needs at least one case, so the last case stays.", "$.cases");
        }

        layout.Lines.RemoveRange(existing.Start, existing.End - existing.Start);
        return Checked(layout.Lines);
    }

    /// <summary>
    /// Replaces the rule of the file (<c>rule</c> or <c>ruleFile</c>) with an inline rule, to run the cases of the file on
    /// another rule. The result is for running only: do not write it back.
    /// </summary>
    /// <param name="fileText">The text of the file.</param>
    /// <param name="ruleText">The rule, as rule text.</param>
    /// <returns>The new text, or the errors when the file is not usable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileText"/> or <paramref name="ruleText"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> WithRule(string fileText, string ruleText)
    {
        ArgumentNullException.ThrowIfNull(fileText);
        ArgumentNullException.ThrowIfNull(ruleText);

        if (!Parse(fileText).TryGetValue(out Layout? layout, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<string>(diagnostics);
        }

        // Remove from the last key to the first, so the line numbers of the keys not yet removed stay right.
        List<string> lines = layout.Lines;
        List<KeyValuePair<YamlNode, YamlNode>> keys = [.. layout.Root.Children];
        for (int i = keys.Count - 1; i >= 0; i--)
        {
            if (keys[i].Key is YamlScalarNode { Value: "rule" or "ruleFile" } key)
            {
                int start = LineIndex(key.Start);
                int end = i + 1 < keys.Count ? LineIndex(keys[i + 1].Key.Start) : lines.Count;
                lines.RemoveRange(start, end - start);
            }
        }

        lines.InsertRange(0, RuleBlock(ruleText));
        return Checked(lines);
    }

    private static List<string> RuleBlock(string ruleText)
    {
        return ["rule: |", .. ruleText.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Select(line => "  " + line)];
    }

    // One case as block YAML lines. The dash sits at `dash` spaces; its keys sit two further in.
    private static List<string> Emit(RuleTestCaseSpec spec, int dash)
    {
        string pad = new(' ', dash);
        string key = new(' ', dash + StepIndent);
        string entry = new(' ', dash + (2 * StepIndent));
        List<string> lines = [$"{pad}- name: {Quote(spec.Name)}"];
        if (spec.Given.Count > 0)
        {
            lines.Add($"{key}given:");
            lines.AddRange(
                spec.Given.OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{entry}{Key(g.Key)}: {g.Value}")
            );
        }

        if (spec.Variables.Count > 0)
        {
            lines.Add($"{key}variables:");
            lines.AddRange(AssignmentVariables.ToYamlBlock(spec.Variables, dash + (2 * StepIndent)).Split('\n'));
        }

        lines.Add($"{key}expect:");
        lines.Add($"{entry}result: {spec.ExpectedResult}");
        if (spec.ExpectedFaults is { } faults)
        {
            lines.Add($"{entry}faults: {faults}");
        }

        return lines;
    }

    // A double-quoted scalar: JSON strings are valid YAML double-quoted strings.
    private static string Quote(string text)
    {
        return JsonValue.Create(text).ToJsonString();
    }

    // A term as a mapping key: plain when it is a simple name, else single-quoted.
    private static string Key(string term)
    {
        bool simple =
            term.Length > 0
            && (char.IsAsciiLetter(term[0]) || term[0] == '_')
            && term.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
        return simple ? term : "'" + term.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static ToolResult<string> Checked(List<string> lines)
    {
        string text = string.Join('\n', lines).TrimEnd('\n') + "\n";

        // The edit must leave a file that still has the cases; a mistake in the line arithmetic fails here, not on disk.
        return Parse(text).Succeeded
            ? ToolResults.Success(text)
            : Invalid<string>("The edit would leave a rule test file that cannot be read.", "$");
    }

    private static ToolResult<T> Invalid<T>(string message, string path)
        where T : class
    {
        return ToolResults.Failure<T>(ToolDiagnosticCodes.InvalidRuleTestFile, message, path);
    }

    private static ToolResult<Layout> Parse(string fileText)
    {
        string normalized = fileText.ReplaceLineEndings("\n");
        List<string> lines = [.. normalized.Split('\n')];
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(normalized);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            return Invalid<Layout>($"The YAML is not well-formed (line {ex.Start.Line}, column {ex.Start.Column}).", "$");
        }

        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            return Invalid<Layout>("A rule test file is a YAML mapping with a rule and cases.", "$");
        }

        List<KeyValuePair<YamlNode, YamlNode>> keys = [.. root.Children];
        int casesIndex = keys.FindIndex(k => k.Key is YamlScalarNode { Value: "cases" });
        if (casesIndex < 0 || keys[casesIndex].Value is not YamlSequenceNode { Style: SequenceStyle.Block } cases)
        {
            return Invalid<Layout>("The file needs a 'cases' key with a block list (- name: ...).", "$.cases");
        }

        List<YamlMappingNode> items = [];
        foreach (YamlNode item in cases.Children)
        {
            if (
                item is not YamlMappingNode { Style: MappingStyle.Block } mapping
                || !lines[LineIndex(mapping.Start)].TrimStart().StartsWith('-')
            )
            {
                return Invalid<Layout>("Each case must be a block mapping that starts on the line of its dash.", "$.cases");
            }

            items.Add(mapping);
        }

        if (items.Count == 0)
        {
            return Invalid<Layout>("The file needs at least one case.", "$.cases");
        }

        int sectionEnd = casesIndex + 1 < keys.Count ? LineIndex(keys[casesIndex + 1].Key.Start) : lines.Count;
        List<Chunk> chunks = [];
        for (int i = 0; i < items.Count; i++)
        {
            int start = LineIndex(items[i].Start);
            int end = i + 1 < items.Count ? LineIndex(items[i + 1].Start) : sectionEnd;
            while (end - 1 > start && string.IsNullOrWhiteSpace(lines[end - 1]))
            {
                end--;
            }

            string name = items[i].Children.FirstOrDefault(c => c.Key is YamlScalarNode { Value: "name" }).Value
                is YamlScalarNode scalar
                ? scalar.Value ?? string.Empty
                : string.Empty;
            chunks.Add(new Chunk(name, start, end));
        }

        int dash = lines[chunks[0].Start].IndexOf('-', StringComparison.Ordinal);
        return ToolResults.Success(new Layout(lines, root, items, chunks, dash));
    }

    private static RuleTestCaseSpec? ReadCase(
        YamlMappingNode item,
        string path,
        string? baseDirectory,
        List<Diagnostic> diagnostics
    )
    {
        string? name = (Child(item, "name") as YamlScalarNode)?.Value;
        if (string.IsNullOrEmpty(name))
        {
            diagnostics.Add(ToolResults.Error(ToolDiagnosticCodes.InvalidRuleTestFile, "A case needs a name.", path));
            return null;
        }

        JsonObject given = [];
        if (Child(item, "given") is YamlMappingNode givenNode)
        {
            foreach ((YamlNode term, YamlNode value) in givenNode.Children)
            {
                given[(term as YamlScalarNode)?.Value ?? string.Empty] = (value as YamlScalarNode)?.Value;
            }
        }

        if (
            !StubAssignment
                .Parse(given.ToJsonString(), RuleFormat.Json)
                .TryGetValue(out StubAssignment? stubs, out EquatableArray<Diagnostic> stubDiagnostics)
        )
        {
            diagnostics.AddRange(stubDiagnostics.Select(d => d with { Path = $"{path}.given" }));
            return null;
        }

        JsonObject variables = [];
        if (!ReadVariables(item, path, baseDirectory, variables, diagnostics))
        {
            return null;
        }

        if (
            Child(item, "expect") is not YamlMappingNode expect
            || !TryResult((Child(expect, "result") as YamlScalarNode)?.Value, out TruthValue result)
        )
        {
            diagnostics.Add(
                ToolResults.Error(
                    ToolDiagnosticCodes.InvalidRuleTestFile,
                    $"The case '{name}' needs expect: result: True, False or Unknown.",
                    path
                )
            );
            return null;
        }

        int? faults = int.TryParse((Child(expect, "faults") as YamlScalarNode)?.Value, out int count) ? count : null;
        return new RuleTestCaseSpec(name, stubs.Values, variables, result, faults);
    }

    private static bool ReadVariables(
        YamlMappingNode item,
        string path,
        string? baseDirectory,
        JsonObject variables,
        List<Diagnostic> diagnostics
    )
    {
        try
        {
            if (Child(item, "variables") is YamlMappingNode inline)
            {
                foreach ((YamlNode source, YamlNode data) in inline.Children)
                {
                    variables[(source as YamlScalarNode)?.Value ?? string.Empty] = AssignmentVariables.FromYaml(data);
                }
            }

            if (Child(item, "variableFiles") is YamlMappingNode files)
            {
                foreach ((YamlNode source, YamlNode file) in files.Children)
                {
                    string full = Path.GetFullPath(
                        (file as YamlScalarNode)?.Value ?? string.Empty,
                        baseDirectory ?? Environment.CurrentDirectory
                    );
                    if (
                        !File.Exists(full) || !AssignmentVariables.TryReadAny(File.ReadAllText(full), out JsonNode? data, out _)
                    )
                    {
                        diagnostics.Add(
                            ToolResults.Error(
                                ToolDiagnosticCodes.InvalidRuleTestFile,
                                $"The data file '{full}' cannot be read.",
                                path
                            )
                        );
                        return false;
                    }

                    variables[(source as YamlScalarNode)?.Value ?? string.Empty] = data;
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(
                ToolResults.Error(ToolDiagnosticCodes.InvalidRuleTestFile, "The variables of the case cannot be read.", path)
            );
            return false;
        }

        return true;
    }

    private static bool TryResult(string? text, out TruthValue value)
    {
        switch (text?.ToUpperInvariant())
        {
            case "TRUE" or "T":
                value = TruthValue.True;
                return true;
            case "FALSE" or "F":
                value = TruthValue.False;
                return true;
            case "UNKNOWN" or "U":
                value = TruthValue.Unknown;
                return true;
            default:
                value = TruthValue.Unknown;
                return false;
        }
    }

    // YamlDotNet counts lines from 1 and as a long; the lines of a file are indexed from 0.
    private static int LineIndex(Mark mark)
    {
        return checked((int)mark.Line) - 1;
    }

    private static YamlNode? Child(YamlMappingNode node, string key)
    {
        return node.Children.FirstOrDefault(c => c.Key is YamlScalarNode scalar && scalar.Value == key).Value;
    }

    /// <summary>One case in the lines: its name and its lines, from <see cref="Start"/> to before <see cref="End"/> (0-based).</summary>
    private sealed record Chunk(string Name, int Start, int End);

    /// <summary>The parsed file: its lines, its root mapping, its case nodes, where each case sits and the column of the dashes.</summary>
    private sealed record Layout(
        List<string> Lines,
        YamlMappingNode Root,
        List<YamlMappingNode> Items,
        List<Chunk> Chunks,
        int DashColumn
    );
}
