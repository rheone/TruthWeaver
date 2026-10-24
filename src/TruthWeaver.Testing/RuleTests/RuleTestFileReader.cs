namespace TruthWeaver.Testing.RuleTests;

using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Testing.Fakes;
using TruthWeaver.Yaml;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Reads the YAML of a rule test file into a <see cref="RuleTestFile"/>. It reports every problem it finds, each with its
/// path and place in the file, instead of stopping at the first one.
/// </summary>
internal sealed class RuleTestFileReader
{
    private static readonly string[] FileKeys = ["name", "rule", "ruleFile", "cases"];
    private static readonly string[] CaseKeys = ["name", "given", "variables", "variableFiles", "expect"];
    private static readonly string[] ExpectKeys = ["result", "faults", "trace"];

    private readonly string baseDirectory;
    private readonly List<RuleTestFileError> errors = [];

    private RuleTestFileReader(string baseDirectory)
    {
        this.baseDirectory = baseDirectory;
    }

    /// <summary>Reads a rule test file.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <param name="baseDirectory">The directory that a <c>ruleFile</c> or <c>variableFiles</c> path is relative to.</param>
    /// <param name="defaultName">The name of the file when it has no <c>name</c> key.</param>
    /// <param name="errors">Every problem; empty when the file is usable.</param>
    /// <returns>The file, or <see langword="null"/> when it has a problem.</returns>
    public static RuleTestFile? Read(
        string yaml,
        string baseDirectory,
        string defaultName,
        out IReadOnlyList<RuleTestFileError> errors
    )
    {
        RuleTestFileReader reader = new(baseDirectory);
        RuleTestFile? file = reader.ReadFile(yaml, defaultName);
        errors = reader.errors;
        return reader.errors.Count == 0 ? file : null;
    }

    /// <summary>Reads a truth value: <c>True</c>, <c>False</c> or <c>Unknown</c>, or <c>T</c>, <c>F</c> or <c>U</c>, with any case.</summary>
    private static TruthValue? ParseTruthValue(string text)
    {
        return text.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "T" => TruthValue.True,
            "FALSE" or "F" => TruthValue.False,
            "UNKNOWN" or "U" => TruthValue.Unknown,
            _ => null,
        };
    }

    /// <summary>Gives the format of a rule file from its extension: JSON, YAML, or otherwise rule text.</summary>
    private static RuleFormat FormatOf(string path)
    {
        string extension = Path.GetExtension(path);
        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            return RuleFormat.Json;
        }

        if (
            string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase)
        )
        {
            return RuleFormat.Yaml;
        }

        return RuleFormat.Text;
    }

    /// <summary>Writes one YAML node as a YAML document, so an inline rule tree compiles with <c>CompileYaml</c>.</summary>
    private static string ToYamlText(YamlNode node)
    {
        YamlStream stream = [new YamlDocument(node)];
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    private static YamlNode? Find(YamlMappingNode mapping, string key)
    {
        return mapping.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? value : null;
    }

    private RuleTestFile? ReadFile(string yaml, string defaultName)
    {
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(yaml);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            // The parser's message can quote the document, so only the position is kept.
            this.errors.Add(
                new RuleTestFileError("The file is not well-formed YAML.", "$", (int)ex.Start.Line, (int)ex.Start.Column)
            );
            return null;
        }

        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            this.errors.Add(new RuleTestFileError("A rule test file is a YAML mapping with a rule and cases.", "$", 0, 0));
            return null;
        }

        this.CheckKeys(root, "$", FileKeys);
        string name = defaultName;
        if (Find(root, "name") is { } nameNode && this.ReadText(nameNode, "$.name") is { } text)
        {
            name = text;
        }

        (string Source, RuleFormat Format, string? File)? rule = this.ReadRule(root);
        List<RuleTestCase> cases = this.ReadCases(root);
        return rule is { } r ? new RuleTestFile(name, r.Source, r.Format, r.File, cases) : null;
    }

    private (string Source, RuleFormat Format, string? File)? ReadRule(YamlMappingNode root)
    {
        YamlNode? inline = Find(root, "rule");
        YamlNode? file = Find(root, "ruleFile");
        if ((inline is null) == (file is null))
        {
            this.Add(root, "$", "Give the rule with exactly one of 'rule' (inline) and 'ruleFile' (a path).");
            return null;
        }

        // Empty rule text is a problem of the file, as the schema states, not a rule that does not compile.
        if (inline is YamlScalarNode { Value: { } ruleText } && !string.IsNullOrWhiteSpace(ruleText))
        {
            return (ruleText, RuleFormat.Text, null);
        }

        if (inline is YamlMappingNode tree)
        {
            return (ToYamlText(tree), RuleFormat.Yaml, null);
        }

        if (inline is not null)
        {
            this.Add(inline, "$.rule", "'rule' is rule text that is not empty (a string) or a rule tree (a mapping).");
            return null;
        }

        if (this.ReadText(file!, "$.ruleFile") is not { } relative)
        {
            return null;
        }

        string rulePath = Path.GetFullPath(relative, this.baseDirectory);
        if (this.ReadFileText(rulePath, file!, "$.ruleFile") is not { } source)
        {
            return null;
        }

        return (source, FormatOf(rulePath), rulePath);
    }

    private List<RuleTestCase> ReadCases(YamlMappingNode root)
    {
        List<RuleTestCase> cases = [];
        YamlNode? node = Find(root, "cases");
        if (node is not YamlSequenceNode { Children.Count: > 0 } sequence)
        {
            this.Add(node ?? root, "$.cases", "'cases' is a list with at least one case.");
            return cases;
        }

        HashSet<string> names = [with(StringComparer.Ordinal)];
        for (int index = 0; index < sequence.Children.Count; index++)
        {
            string path = $"$.cases[{index}]";
            if (sequence.Children[index] is not YamlMappingNode item)
            {
                this.Add(sequence.Children[index], path, "A case is a mapping with 'name', 'given' and 'expect'.");
                continue;
            }

            // The name is checked before the rest of the case, so a second use is reported even when the case has
            // other problems.
            if (Find(item, "name") is YamlScalarNode { Value: { Length: > 0 } name } nameNode && !names.Add(name))
            {
                this.Add(nameNode, path + ".name", $"The case name '{name}' is used more than once.");
            }

            if (this.ReadCase(item, path) is { } read)
            {
                cases.Add(read);
            }
        }

        return cases;
    }

    private RuleTestCase? ReadCase(YamlMappingNode item, string path)
    {
        int before = this.errors.Count;
        this.CheckKeys(item, path, CaseKeys);

        string? name = null;
        if (Find(item, "name") is { } nameNode)
        {
            name = this.ReadText(nameNode, path + ".name");
        }
        else
        {
            this.Add(item, path, "A case needs a 'name'.");
        }

        StubValues given = this.ReadGiven(Find(item, "given"), path + ".given");
        Dictionary<string, IDataSource> variables = [with(StringComparer.Ordinal)];
        this.ReadVariables(Find(item, "variables"), path + ".variables", variables);
        this.ReadVariableFiles(Find(item, "variableFiles"), path + ".variableFiles", variables);

        RuleTestExpectation? expect = null;
        if (Find(item, "expect") is { } expectNode)
        {
            expect = this.ReadExpect(expectNode, path + ".expect");
        }
        else
        {
            this.Add(item, path, "A case needs 'expect' with a 'result'.");
        }

        return this.errors.Count == before && name is not null && expect is not null
            ? new RuleTestCase(name, given, variables, expect)
            : null;
    }

    private StubValues ReadGiven(YamlNode? node, string path)
    {
        if (node is null)
        {
            return StubValues.Empty;
        }

        if (node is not YamlMappingNode mapping)
        {
            this.Add(node, path, "'given' is a mapping from a term to True, False or Unknown.");
            return StubValues.Empty;
        }

        List<KeyValuePair<string, TruthValue>> values = [];
        HashSet<string> terms = [with(StringComparer.Ordinal)];
        foreach ((YamlNode keyNode, YamlNode valueNode) in mapping.Children)
        {
            string key = keyNode is YamlScalarNode { Value: { } k } ? k : string.Empty;
            string entryPath = $"{path}['{key}']";
            if (!StubValues.TryNormalizeTerm(key, out string? term, out string? error))
            {
                this.Add(keyNode, entryPath, error);
            }
            else if (!terms.Add(term))
            {
                this.Add(keyNode, entryPath, $"The term '{key}' names the term '{term}' a second time.");
            }
            else if (valueNode is not YamlScalarNode { Value: { } text } || ParseTruthValue(text) is not { } value)
            {
                this.Add(valueNode, entryPath, $"The value of '{key}' is not a truth value. Use True, False or Unknown.");
            }
            else
            {
                values.Add(new KeyValuePair<string, TruthValue>(term, value));
            }
        }

        // Every key is already one canonical term, seen once, so the creation cannot fail here.
        return StubValues.TryCreate(values, out StubValues? stubs, out _) ? stubs : StubValues.Empty;
    }

    private void ReadVariables(YamlNode? node, string path, Dictionary<string, IDataSource> variables)
    {
        if (node is null)
        {
            return;
        }

        if (node is not YamlMappingNode mapping)
        {
            this.Add(node, path, "'variables' is a mapping from a data source name to its data.");
            return;
        }

        foreach ((YamlNode keyNode, YamlNode valueNode) in mapping.Children)
        {
            string name = keyNode is YamlScalarNode { Value: { } k } ? k : string.Empty;
            try
            {
                variables[name] = YamlDataSource.Create(valueNode);
            }
            catch (YamlException)
            {
                this.Add(valueNode, $"{path}['{name}']", "The data has an alias that refers to its own ancestor.");
            }
        }
    }

    private void ReadVariableFiles(YamlNode? node, string path, Dictionary<string, IDataSource> variables)
    {
        if (node is null)
        {
            return;
        }

        if (node is not YamlMappingNode mapping)
        {
            this.Add(node, path, "'variableFiles' is a mapping from a data source name to a JSON or YAML file.");
            return;
        }

        foreach ((YamlNode keyNode, YamlNode valueNode) in mapping.Children)
        {
            string name = keyNode is YamlScalarNode { Value: { } k } ? k : string.Empty;
            string entryPath = $"{path}['{name}']";
            if (variables.ContainsKey(name))
            {
                this.Add(keyNode, entryPath, $"The data source '{name}' is given more than once.");
                continue;
            }

            if (this.ReadText(valueNode, entryPath) is not { } relative)
            {
                continue;
            }

            string file = Path.GetFullPath(relative, this.baseDirectory);
            if (this.ReadFileText(file, valueNode, entryPath) is not { } text)
            {
                continue;
            }

            // The readers report only the position of a problem, never the content of the document.
            bool parsed;
            IDataSource? source;
            string? error;
            if (FormatOf(file) == RuleFormat.Json)
            {
                parsed = JsonDataSource.TryParse(text, out JsonDataSource? json, out error);
                source = json;
            }
            else
            {
                parsed = YamlDataSource.TryParse(text, out YamlDataSource? yamlSource, out error);
                source = yamlSource;
            }

            if (parsed && source is not null)
            {
                variables[name] = source;
            }
            else
            {
                this.Add(valueNode, entryPath, $"The data file '{relative}' is not usable: {error}");
            }
        }
    }

    private RuleTestExpectation? ReadExpect(YamlNode node, string path)
    {
        if (node is not YamlMappingNode mapping)
        {
            this.Add(node, path, "'expect' is a mapping with a 'result'.");
            return null;
        }

        this.CheckKeys(mapping, path, ExpectKeys);
        TruthValue? result = null;
        if (Find(mapping, "result") is not { } resultNode)
        {
            this.Add(mapping, path, "'expect' needs a 'result': True, False or Unknown.");
        }
        else if (resultNode is not YamlScalarNode { Value: { } text } || (result = ParseTruthValue(text)) is null)
        {
            this.Add(resultNode, path + ".result", "The expected result is not a truth value. Use True, False or Unknown.");
        }

        int? faults = null;
        if (Find(mapping, "faults") is { } faultsNode)
        {
            if (
                faultsNode is YamlScalarNode { Value: { } count }
                && int.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
            )
            {
                faults = parsed;
            }
            else
            {
                this.Add(faultsNode, path + ".faults", "The expected fault count is a whole number from 0.");
            }
        }

        string? trace = null;
        if (Find(mapping, "trace") is { } traceNode)
        {
            trace = this.ReadText(traceNode, path + ".trace");
        }

        return result is { } value ? new RuleTestExpectation(value, faults, trace) : null;
    }

    private string? ReadFileText(string file, YamlNode node, string path)
    {
        try
        {
            return File.ReadAllText(file);
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            this.Add(node, path, $"The file '{file}' cannot be read: {ex.GetType().Name}.");
            return null;
        }
    }

    private string? ReadText(YamlNode node, string path)
    {
        if (node is YamlScalarNode { Value: { Length: > 0 } text })
        {
            return text;
        }

        this.Add(node, path, "The value is a text that is not empty.");
        return null;
    }

    private void CheckKeys(YamlMappingNode mapping, string path, string[] known)
    {
        foreach (YamlNode key in mapping.Children.Keys)
        {
            string text = key is YamlScalarNode { Value: { } k } ? k : string.Empty;
            if (!known.Contains(text, StringComparer.Ordinal))
            {
                this.Add(key, path, $"'{text}' is not a key here. The keys are {string.Join(", ", known)}.");
            }
        }
    }

    private void Add(YamlNode node, string path, string message)
    {
        this.errors.Add(new RuleTestFileError(message, path, (int)node.Start.Line, (int)node.Start.Column));
    }
}
