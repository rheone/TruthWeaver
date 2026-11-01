namespace TruthWeaver.Yaml;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Reads the <c>let</c> definitions and uses of a YAML rule document, and prints a document with its definitions as YAML:
/// the YAML half of <see cref="RuleDefinitions"/>, which reads rule text and JSON.
/// </summary>
public static class YamlRuleDefinitions
{
    /// <summary>Reads the definitions and uses of a rule in the YAML tree format.</summary>
    /// <param name="yaml">The YAML tree text.</param>
    /// <param name="options">
    /// The resource limits to read by, matching <see cref="RuleDefinitions.ReadText"/>: <see cref="CompilerOptions.MaxDepth"/>
    /// bounds the parse, and <see cref="CompilerOptions.MaxDefinitionDepth"/> and <see cref="CompilerOptions.MaxNodeCount"/>
    /// bound the <see cref="RuleDefinitions.SourcesOf"/> walk. Defaults to <see cref="CompilerOptions.Default"/>, so a
    /// caller that compiles with a different <see cref="CompilerOptions.MaxDefinitionDepth"/> can pass the same
    /// options here and get a source map that agrees with that compile (ticket 23), instead of one truncated at the
    /// default depth.
    /// </param>
    /// <returns>The definitions and uses, or the diagnostics when the YAML cannot be read as a rule tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml"/> is <see langword="null"/>.</exception>
    public static RuleDefinitions ReadYaml(string yaml, CompilerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        CompilerOptions effective = options ?? CompilerOptions.Default;
        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = YamlTreeParser.Parse(
            yaml,
            effective.MaxDepth,
            effective.MaxNodeCount
        );
        return RuleDefinitions.FromParsed(root, diagnostics, null, effective);
    }

    /// <summary>
    /// Reads the definitions and uses of a rule in any of the three formats: <see cref="RuleDefinitions.ReadText"/>,
    /// <see cref="RuleDefinitions.ReadJson"/> or <see cref="ReadYaml"/>.
    /// </summary>
    /// <param name="source">The rule in <paramref name="format"/>.</param>
    /// <param name="format">The format of <paramref name="source"/>.</param>
    /// <param name="options">The resource limits to read by; see <see cref="ReadYaml"/>. Defaults to <see cref="CompilerOptions.Default"/>.</param>
    /// <returns>The definitions and uses, or the diagnostics when the source cannot be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static RuleDefinitions ReadAs(string source, RuleFormat format, CompilerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        return format switch
        {
            RuleFormat.Text => RuleDefinitions.ReadText(source, options),
            RuleFormat.Json => RuleDefinitions.ReadJson(source, options),
            RuleFormat.Yaml => ReadYaml(source, options),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a defined rule format."),
        };
    }

    /// <summary>
    /// Prints a document with its definitions in the YAML tree format: the tree of <see cref="RuleDefinitions.PrintJson"/>,
    /// written as YAML in the layout of <c>PrintYaml</c>. The YAML tree format has no shape for a library rule reference
    /// (<c>rule("name")</c>) yet, so a document that holds one at any depth is not printed.
    /// </summary>
    /// <param name="definitions">The document, read with one of the read methods.</param>
    /// <returns>
    /// The YAML text, or <see langword="null"/> when the document was not read or holds a library rule reference.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="definitions"/> is <see langword="null"/>.</exception>
    public static string? PrintYaml(this RuleDefinitions definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        YamlNodeWriter nodeWriter = new();
        if (!definitions.WriteTree(nodeWriter))
        {
            return null;
        }

        YamlStream stream = [with(new YamlDocument(nodeWriter.Result!))];
        using StringWriter writer = new();
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }
}
