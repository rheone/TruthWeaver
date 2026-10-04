namespace TruthWeaver.Yaml;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Parses the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in YAML,
/// into the same raw <see cref="RuleNode"/> tree the DSL and JSON front ends produce (ticket 08).
/// Never throws for malformed YAML — it reports a <see cref="DiagnosticCodes.MalformedTree"/>
/// diagnostic instead. Each diagnostic is located by its path from the document root (<c>$.operands[1].op</c>, the same
/// syntax JSON uses) and, because YamlDotNet keeps node positions, by the span of the offending node as well.
/// </summary>
internal static class YamlTreeParser
{
    /// <summary>Parses YAML tree text into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="yaml">The YAML tree text.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the YAML itself was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(string yaml)
    {
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(yaml);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            List<Diagnostic> diagnostics = [SyntaxError(yaml, ex)];
            return (null, diagnostics);
        }

        if (stream.Documents.Count == 0)
        {
            List<Diagnostic> diagnostics =
            [
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "The YAML document is empty.",
                    SourceSpan.None,
                    expected: "a YAML mapping",
                    found: "an empty document",
                    path: TreePath.Root
                ),
            ];
            return (null, diagnostics);
        }

        return Parse(stream.Documents[0].RootNode);
    }

    /// <summary>Parses an already-materialized YAML tree node (e.g. a subtree of a larger document) into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="node">The YAML tree node.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the node was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(YamlNode node)
    {
        return TreeFormatReader.Read(new YamlNodeCursor(node), YamlNodeCursor.Vocabulary);
    }

    /// <summary>Builds the diagnostic for text that is not YAML: the parser's message, its position, and the nearest valid ancestor.</summary>
    private static Diagnostic SyntaxError(string yaml, YamlException ex)
    {
        // YamlDotNet prefixes its messages with "(Line: .., Col: .., Idx: ..) - (..): "; the position is carried by the span.
        string reason = ex.Message;
        int detail = reason.IndexOf("): ", StringComparison.Ordinal);
        if (detail >= 0)
        {
            reason = reason[(detail + 3)..];
        }

        int start = Math.Clamp((int)ex.Start.Index, 0, yaml.Length);
        int length = Math.Max(1, (int)ex.End.Index - (int)ex.Start.Index);
        return Diagnostic.Error(
            DiagnosticCodes.MalformedTree,
            $"Malformed YAML: {ex.Message}",
            new SourceSpan(start, length),
            expected: "well-formed YAML",
            found: reason,
            path: ContainerPathAt(yaml)
        );
    }

    /// <summary>Reads the text event by event until the parser gives up, and returns the path of the innermost container still open.</summary>
    private static string ContainerPathAt(string yaml)
    {
        TreePathTracker tracker = new();
        try
        {
            using StringReader reader = new(yaml);
            Parser parser = new(reader);
            while (parser.MoveNext())
            {
                switch (parser.Current)
                {
                    case MappingStart:
                        tracker.Enter(isArray: false);
                        break;
                    case SequenceStart:
                        tracker.Enter(isArray: true);
                        break;
                    case MappingEnd or SequenceEnd:
                        tracker.YamlExit();
                        break;
                    case YamlDotNet.Core.Events.Scalar scalar:
                        tracker.YamlScalar(scalar.Value);
                        break;
                    case AnchorAlias:
                        tracker.YamlScalar(string.Empty);
                        break;
                }
            }
        }
        catch (YamlException)
        {
            // The parser stopping is the point: the tracker now holds the last good position.
        }

        return tracker.ContainerPath;
    }
}
