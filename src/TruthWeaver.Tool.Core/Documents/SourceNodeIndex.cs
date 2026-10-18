namespace TruthWeaver.Tool.Core.Documents;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Loading;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

/// <summary>
/// Finds the source span of every value of a JSON or YAML document by its path, so a rule inside the document can be
/// patched in place. A parsed document model keeps no usable end positions, so JSON is read with the tree-schema reader
/// (<see cref="TreeValue"/>, which keeps spans) and YAML with the YamlDotNet event parser, which marks each event.
/// </summary>
/// <remarks>
/// The keys are the normalized path texts of <see cref="DocumentPath.Segment.Text"/>, joined after <c>$</c>. Only the first
/// YAML document is indexed, the same one <see cref="EmbeddedRuleReader"/> selects from.
/// </remarks>
internal sealed class SourceNodeIndex
{
    private readonly Dictionary<string, SourceNode> nodes;

    private SourceNodeIndex(Dictionary<string, SourceNode> nodes)
    {
        this.nodes = nodes;
    }

    /// <summary>Indexes a document.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="format">The document format, <see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</param>
    /// <returns>The index; it is empty when the document is not well formed.</returns>
    public static SourceNodeIndex Create(string document, RuleFormat format)
    {
        Dictionary<string, SourceNode> nodes = [with(StringComparer.Ordinal)];
        if (format == RuleFormat.Json)
        {
            if (TreeValue.ReadJson(document) is { } root)
            {
                AddJson(nodes, "$", root);
            }
        }
        else
        {
            AddYaml(nodes, document);
        }

        return new SourceNodeIndex(nodes);
    }

    /// <summary>Gets the node at a path.</summary>
    /// <param name="path">The normalized path text, such as <c>$.rules.publish.operands[0]</c>.</param>
    /// <returns>The node, or <see langword="null"/> when the document has no value there.</returns>
    public SourceNode? Find(string path)
    {
        return this.nodes.GetValueOrDefault(path);
    }

    private static void AddJson(Dictionary<string, SourceNode> nodes, string path, TreeValue value)
    {
        SourceNodeKind kind = value.Kind switch
        {
            TreeValueKind.Object => SourceNodeKind.Mapping,
            TreeValueKind.Array => SourceNodeKind.Sequence,
            _ => SourceNodeKind.Scalar,
        };
        nodes[path] = new SourceNode(value.Span.Start, value.Span.End, kind, true, ScalarStyle.Any, false);

        // A repeated key: the last one wins, as in the JSON reader that loads the rule.
        foreach (KeyValuePair<string, TreeValue> member in value.Members)
        {
            AddJson(nodes, path + DocumentPath.Segment.Key(member.Key).Text, member.Value);
        }

        for (int i = 0; i < value.Items.Count; i++)
        {
            AddJson(nodes, path + DocumentPath.Segment.Index(i).Text, value.Items[i]);
        }
    }

    private static void AddYaml(Dictionary<string, SourceNode> nodes, string document)
    {
        using StringReader reader = new(document);
        Parser parser = new(reader);
        List<Frame> frames = [];
        bool rootSeen = false;
        try
        {
            while (parser.MoveNext())
            {
                ParsingEvent current = parser.Current!;
                switch (current)
                {
                    case DocumentEnd when rootSeen:
                        return;
                    case Scalar scalar:
                        AddScalar(nodes, document, frames, scalar, ref rootSeen);
                        break;
                    case AnchorAlias when frames.Count > 0 && frames[^1] is { IsMapping: true, ExpectsKey: true } parent:
                        // An alias used as a key names no path.
                        parent.Key = null;
                        parent.ExpectsKey = false;
                        break;
                    case AnchorAlias alias:
                        AddLeaf(
                            nodes,
                            frames,
                            ChildPath(frames, ref rootSeen),
                            Span(document, alias),
                            SourceNodeKind.Alias,
                            default,
                            true
                        );
                        break;
                    case NodeEvent { } start and (MappingStart or SequenceStart):
                        bool isMapping = start is MappingStart;
                        bool isFlow =
                            start is MappingStart { Style: MappingStyle.Flow } or SequenceStart { Style: SequenceStyle.Flow };
                        bool inherited = frames.Count > 0 && frames[^1].IsShared;
                        bool isKey = frames.Count > 0 && frames[^1] is { IsMapping: true, ExpectsKey: true };
                        frames.Add(
                            new Frame(ChildPath(frames, ref rootSeen), isMapping, isFlow, OffsetOf(document, start.Start))
                            {
                                IsShared = inherited || !start.Anchor.IsEmpty,
                                IsKey = isKey,
                            }
                        );
                        break;
                    case MappingEnd or SequenceEnd:
                        Close(nodes, document, frames, current);
                        break;
                }
            }
        }
        catch (YamlException)
        {
            // A malformed document was already reported by the loader; whatever was indexed before the error stays.
        }
    }

    private static void AddScalar(
        Dictionary<string, SourceNode> nodes,
        string document,
        List<Frame> frames,
        Scalar scalar,
        ref bool rootSeen
    )
    {
        (int start, int end) = Span(document, scalar);
        if (frames.Count > 0 && frames[^1] is { IsMapping: true, ExpectsKey: true } parent)
        {
            // A key: it names the next value. Its end still counts toward the end of the mapping.
            parent.Key = scalar.Value;
            parent.ExpectsKey = false;
            parent.LastEnd = Math.Max(parent.LastEnd, end);
            return;
        }

        bool shared = !scalar.Anchor.IsEmpty || !scalar.Tag.IsEmpty || (frames.Count > 0 && frames[^1].IsShared);
        AddLeaf(nodes, frames, ChildPath(frames, ref rootSeen), (start, end), SourceNodeKind.Scalar, scalar.Style, shared);
    }

    private static void AddLeaf(
        Dictionary<string, SourceNode> nodes,
        List<Frame> frames,
        string? path,
        (int Start, int End) span,
        SourceNodeKind kind,
        ScalarStyle style,
        bool shared
    )
    {
        if (path is not null)
        {
            nodes[path] = new SourceNode(span.Start, span.End, kind, false, style, shared);
        }

        Ended(frames, span.End);
    }

    private static void Close(Dictionary<string, SourceNode> nodes, string document, List<Frame> frames, ParsingEvent end)
    {
        Frame closed = frames[^1];
        frames.RemoveAt(frames.Count - 1);
        int stop;
        if (closed.IsFlow)
        {
            // A flow container's end event is marked at its closing bracket; the span takes the bracket in.
            stop = OffsetOf(document, end.Start);
            if (stop < document.Length && document[stop] is '}' or ']')
            {
                stop++;
            }
        }
        else
        {
            // A block container's end event is marked at the next token, after any comments and blank lines, so the
            // span ends with its last value instead. That value can be a block scalar that ends with line breaks.
            stop = Math.Max(closed.LastEnd, closed.Start);
            while (stop > closed.Start && char.IsWhiteSpace(document[stop - 1]))
            {
                stop--;
            }
        }

        if (closed.Path is not null)
        {
            SourceNodeKind kind = closed.IsMapping ? SourceNodeKind.Mapping : SourceNodeKind.Sequence;
            nodes[closed.Path] = new SourceNode(closed.Start, stop, kind, closed.IsFlow, ScalarStyle.Any, closed.IsShared);
        }

        if (closed.IsKey)
        {
            // A container used as a key: the value of the entry comes next.
            frames[^1].LastEnd = Math.Max(frames[^1].LastEnd, stop);
            return;
        }

        Ended(frames, stop);
    }

    // A value ended at 'end': the parent mapping now waits for a key, and its last end moves on.
    private static void Ended(List<Frame> frames, int end)
    {
        if (frames.Count == 0)
        {
            return;
        }

        Frame parent = frames[^1];
        parent.LastEnd = Math.Max(parent.LastEnd, end);
        if (parent.IsMapping)
        {
            parent.ExpectsKey = true;
        }
    }

    // The path of the value that starts now: the root, the next item of a sequence, or the value of the current key. A
    // value under a key that is not a scalar (a complex key) has no path, and neither has anything inside it.
    private static string? ChildPath(List<Frame> frames, ref bool rootSeen)
    {
        if (frames.Count == 0)
        {
            rootSeen = true;
            return "$";
        }

        Frame parent = frames[^1];
        if (parent.Path is null)
        {
            return null;
        }

        if (!parent.IsMapping)
        {
            return parent.Path + DocumentPath.Segment.Index(++parent.ItemIndex).Text;
        }

        if (parent.ExpectsKey)
        {
            // A container used as a key.
            parent.ExpectsKey = false;
            parent.Key = null;
            return null;
        }

        return parent.Key is null ? null : parent.Path + DocumentPath.Segment.Key(parent.Key).Text;
    }

    private static (int Start, int End) Span(string document, ParsingEvent parsingEvent)
    {
        return (OffsetOf(document, parsingEvent.Start), OffsetOf(document, parsingEvent.End));
    }

    private static int OffsetOf(string document, Mark mark)
    {
        return EmbeddedRuleReader.OffsetOf(document, mark);
    }

    // An open mapping or sequence while the events are read.
    private sealed class Frame(string? path, bool isMapping, bool isFlow, int start)
    {
        public string? Path { get; } = path;

        public bool IsMapping { get; } = isMapping;

        public bool IsFlow { get; } = isFlow;

        public int Start { get; } = start;

        public bool IsShared { get; init; }

        public bool IsKey { get; init; }

        public bool ExpectsKey { get; set; } = isMapping;

        public string? Key { get; set; }

        public int ItemIndex { get; set; } = -1;

        public int LastEnd { get; set; } = start;
    }
}
