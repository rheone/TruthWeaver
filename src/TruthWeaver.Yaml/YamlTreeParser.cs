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
            // YamlDotNet rejects a repeated mapping key while loading. A repeated argument is its own mistake, with one
            // code on every surface, so it is found again here and reported as such.
            List<Diagnostic> diagnostics = [FindDuplicateArgument(yaml) ?? SyntaxError(yaml, ex)];
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

    /// <summary>
    /// Reads the text event by event and reports the first key that repeats inside an <c>args</c> mapping as a
    /// <see cref="DiagnosticCodes.DuplicateArgument"/> error at that key.
    /// </summary>
    /// <returns>The diagnostic, or <see langword="null"/> when no <c>args</c> mapping repeats a key before the text stops being YAML.</returns>
    private static Diagnostic? FindDuplicateArgument(string yaml)
    {
        List<ScanFrame> frames = [];
        try
        {
            using StringReader reader = new(yaml);
            Parser parser = new(reader);
            while (parser.MoveNext())
            {
                switch (parser.Current)
                {
                    case MappingStart:
                        frames.Add(
                            new ScanFrame(frames.Count == 0 ? TreePath.Root : frames[^1].ChildPath(), isSequence: false)
                        );
                        break;
                    case SequenceStart:
                        frames.Add(new ScanFrame(frames.Count == 0 ? TreePath.Root : frames[^1].ChildPath(), isSequence: true));
                        break;
                    case MappingEnd or SequenceEnd:
                        frames.RemoveAt(frames.Count - 1);
                        frames.LastOrDefault()?.ValueDone();
                        break;
                    case YamlDotNet.Core.Events.Scalar scalar when frames.Count > 0:
                        ScanFrame top = frames[^1];
                        if (top.ExpectsKey && !top.IsSequence)
                        {
                            if (!top.Keys.Add(scalar.Value) && top.Path.EndsWith(".args", StringComparison.Ordinal))
                            {
                                return DuplicateArgument(scalar, TreePath.Property(top.Path, scalar.Value));
                            }

                            top.CurrentKey = scalar.Value;
                        }

                        top.ValueDone();
                        break;
                    case AnchorAlias when frames.Count > 0:
                        frames[^1].ValueDone();
                        break;
                }
            }
        }
        catch (YamlException)
        {
            // The text stopped being YAML before any key repeated, so this is a syntax error, not a repeated argument.
        }

        return null;
    }

    private static Diagnostic DuplicateArgument(YamlDotNet.Core.Events.Scalar key, string path)
    {
        int start = Math.Max(0, (int)key.Start.Index);
        return Diagnostic.Error(
            DiagnosticCodes.DuplicateArgument,
            $"Argument '{key.Value}' is given more than once.",
            new SourceSpan(start, Math.Max(1, (int)key.End.Index - start)),
            expected: "each argument once",
            found: $"'{key.Value}' repeated",
            suggestion: new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, $"Remove one '{key.Value}' argument."),
            path: path
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

    /// <summary>One open mapping or sequence while <see cref="FindDuplicateArgument"/> scans the events.</summary>
    private sealed class ScanFrame(string path, bool isSequence)
    {
        private int index = -1;

        public string Path { get; } = path;

        public bool IsSequence { get; } = isSequence;

        public HashSet<string> Keys { get; } = [with(StringComparer.Ordinal)];

        public string? CurrentKey { get; set; }

        // In a mapping the scalars alternate key, value, key, value.
        public bool ExpectsKey { get; private set; } = true;

        // The path of the child that the next event opens: the current key's value, or the next item.
        public string ChildPath()
        {
            if (this.IsSequence)
            {
                return TreePath.Index(this.Path, this.index + 1);
            }

            return this.CurrentKey is null ? this.Path : TreePath.Property(this.Path, this.CurrentKey);
        }

        // Records that a key, a value or a whole child container has been read.
        public void ValueDone()
        {
            if (this.IsSequence)
            {
                this.index++;
            }
            else
            {
                this.ExpectsKey = !this.ExpectsKey;
            }
        }
    }
}
