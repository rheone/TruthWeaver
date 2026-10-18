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
    /// <returns>The definitions and uses, or the diagnostics when the YAML cannot be read as a rule tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml"/> is <see langword="null"/>.</exception>
    public static RuleDefinitions ReadYaml(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = YamlTreeParser.Parse(yaml);
        return RuleDefinitions.FromParsed(root, diagnostics, null);
    }

    /// <summary>
    /// Reads the definitions and uses of a rule in any of the three formats: <see cref="RuleDefinitions.ReadText"/>,
    /// <see cref="RuleDefinitions.ReadJson"/> or <see cref="ReadYaml"/>.
    /// </summary>
    /// <param name="source">The rule in <paramref name="format"/>.</param>
    /// <param name="format">The format of <paramref name="source"/>.</param>
    /// <returns>The definitions and uses, or the diagnostics when the source cannot be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static RuleDefinitions ReadAs(string source, RuleFormat format)
    {
        ArgumentNullException.ThrowIfNull(source);

        return format switch
        {
            RuleFormat.Text => RuleDefinitions.ReadText(source),
            RuleFormat.Json => RuleDefinitions.ReadJson(source),
            RuleFormat.Yaml => ReadYaml(source),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a defined rule format."),
        };
    }

    /// <summary>
    /// Prints a document with its definitions in the YAML tree format: the tree of <see cref="RuleDefinitions.PrintJson"/>,
    /// written as YAML in the layout of <c>PrintYaml</c>.
    /// </summary>
    /// <param name="definitions">The document, read with one of the read methods.</param>
    /// <returns>The YAML text, or <see langword="null"/> when the document was not read.</returns>
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
