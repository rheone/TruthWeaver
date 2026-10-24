namespace TruthWeaver.Yaml;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// The YAML adapter for the shared <see cref="TreeFormatReader"/>: a view of one <see cref="YamlNode"/>. YamlDotNet keeps
/// node positions, so <see cref="Span"/> is the real source range of the node and each diagnostic is located by it.
/// </summary>
internal readonly struct YamlNodeCursor : ITreeNodeCursor
{
    private readonly YamlNode node;

    // The ancestors of 'node' (not including 'node' itself), by reference identity. YamlStream.Load resolves an
    // anchor's alias into a reference to the same YamlNode object, so an anchor aliased inside its own body
    // (`a: &x [*x]`) makes the node graph cyclic rather than a tree. This set lets every child cursor this one
    // creates (Child) detect that before recursing into it, mirroring the ancestor set YamlDataSource.ToJson
    // already keeps for the same problem on the data-source path. It must use ReferenceEqualityComparer: YamlNode's
    // own Equals/GetHashCode are structural, recursing into every descendant, so the default comparer would walk the
    // whole subtree (and overflow the stack on a deep one) on every Add instead of comparing by identity.
    private readonly ImmutableHashSet<YamlNode> ancestors;

    /// <summary>Initializes a new instance of the <see cref="YamlNodeCursor"/> struct viewing the root of a YAML tree, with no recorded ancestors yet.</summary>
    /// <param name="node">The node this cursor views.</param>
    public YamlNodeCursor(YamlNode node)
        : this(node, ImmutableHashSet<YamlNode>.Empty.WithComparer(ReferenceEqualityComparer.Instance)) { }

    private YamlNodeCursor(YamlNode node, ImmutableHashSet<YamlNode> ancestors)
    {
        this.node = node;
        this.ancestors = ancestors;
    }

    /// <summary>Gets the diagnostic wording for YAML trees.</summary>
    public static TreeFormatVocabulary Vocabulary { get; } =
        new(
            MappingNoun: "YAML mapping",
            SequenceNoun: "sequence",
            StringNoun: "YAML string",
            KeyNoun: "key",
            ConstMessage: "'const' must be a YAML boolean or one of true, false, unknown.",
            ConstExpected: "a YAML boolean or one of true, false, unknown",
            LiteralExpected: "a string, number, boolean or array"
        );

    /// <inheritdoc />
    public TreeNodeShape Shape =>
        this.node switch
        {
            YamlMappingNode => TreeNodeShape.Mapping,
            YamlSequenceNode => TreeNodeShape.Sequence,
            YamlScalarNode => TreeNodeShape.Scalar,
            _ => TreeNodeShape.Other,
        };

    /// <inheritdoc />
    public SourceSpan Span => SpanOf(this.node);

    /// <inheritdoc />
    public string KindName => this.node.NodeType.ToString();

    /// <inheritdoc />
    public string? StringValue => this.node is YamlScalarNode { Value: { } text } ? text : null;

    /// <inheritdoc />
    public IEnumerable<ITreeNodeCursor> Elements => this.node is YamlSequenceNode sequence ? this.ElementsOf(sequence) : [];

    /// <inheritdoc />
    public IEnumerable<TreeMember> Members => this.node is YamlMappingNode mapping ? this.MembersOf(mapping) : [];

    /// <inheritdoc />
    public string UnsupportedLiteralMessage =>
        IsNull(this.node)
            ? "Unsupported literal YAML value kind 'Null'."
            : $"Unsupported YAML node type '{this.node.NodeType}' for a literal value.";

    /// <inheritdoc />
    public string Describe()
    {
        return this.node switch
        {
            YamlScalarNode scalar when IsNull(scalar) => "null",
            YamlScalarNode => "a scalar",
            YamlSequenceNode => "a sequence",
            YamlMappingNode => "a mapping",
            _ => "an alias",
        };
    }

    /// <inheritdoc />
    public string DescribeValue()
    {
        return this.node is YamlScalarNode { Value: { } text } ? $"'{text}'" : this.Describe();
    }

    /// <inheritdoc />
    public bool TryGetChild(string key, [NotNullWhen(true)] out ITreeNodeCursor? child)
    {
        if (this.node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                if (entry.Key is YamlScalarNode { Value: { } keyText } && string.Equals(keyText, key, StringComparison.Ordinal))
                {
                    child = this.Child(entry.Value);
                    return true;
                }
            }
        }

        child = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetInt32(out int value)
    {
        value = 0;
        return this.node is YamlScalarNode { Value: { } text }
            && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    /// <inheritdoc />
    public bool TryGetTruthValue(out TruthValue value)
    {
        // YAML has a native boolean, but a plain scalar's text is all the parser keeps, so the words true, false and unknown and the letters T, F and U share one spelling rule.
        value = TruthValue.Unknown;
        return this.node is YamlScalarNode { Value: { } text }
            && (TruthValueText.TryParse(text, out value) || TruthValueText.TryParseLetter(text, out value));
    }

    /// <inheritdoc />
    public RawLiteral? ReadScalarLiteral()
    {
        if (this.node is not YamlScalarNode scalar)
        {
            return null;
        }

        // An unquoted null has no literal form. Returning nothing makes the reader report it as JSON's null is reported,
        // instead of silently turning it into the text "null".
        if (IsNull(scalar))
        {
            return null;
        }

        string text = scalar.Value ?? string.Empty;
        SourceSpan span = this.Span;

        // A quoted scalar is always the author's explicit string, regardless of its content
        // (e.g. role: "true" must stay the string "true", not become a boolean).
        if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted or ScalarStyle.Literal or ScalarStyle.Folded)
        {
            return RawLiteral.OfString(text, span);
        }

        if (TryParseBoolean(text, out bool boolValue))
        {
            return RawLiteral.OfBoolean(boolValue, span);
        }

        return IsNumber(text) ? RawLiteral.OfNumber(text, span) : RawLiteral.OfString(text, span);
    }

    /// <summary>
    /// Tests for a YAML null: an unquoted <c>null</c> (any case), <c>~</c> or empty value. A quoted, block or
    /// <c>!!str</c>-tagged scalar is the author's explicit string, so <c>"null"</c> is not a null.
    /// </summary>
    private static bool IsNull(YamlNode node)
    {
        if (node is not YamlScalarNode scalar || scalar.Style is not ScalarStyle.Plain || scalar.Tag == "tag:yaml.org,2002:str")
        {
            return false;
        }

        string text = scalar.Value ?? string.Empty;
        return text.Length == 0 || text == "~" || text.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Gets the source range YamlDotNet recorded for a node, as a span an editor can underline.</summary>
    private static SourceSpan SpanOf(YamlNode node)
    {
        int start = (int)node.Start.Index;
        return new SourceSpan(start, Math.Max(0, (int)node.End.Index - start));
    }

    private static bool TryParseBoolean(string text, out bool value)
    {
        if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    private static bool IsNumber(string text)
    {
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Builds the cursor for a direct child of <see cref="node"/> (a sequence element, a mapping value or key),
    /// recording <see cref="node"/> as that child's ancestor. Throws when the child is already one of this cursor's
    /// own ancestors, including <see cref="node"/> itself: that can only happen when an alias resolved to a node
    /// still being read, i.e. the YAML document is cyclic rather than a tree (YamlDataSource.ToJson detects the
    /// identical case the same way, on the data-source path).
    /// </summary>
    private YamlNodeCursor Child(YamlNode child)
    {
        ImmutableHashSet<YamlNode> childAncestors = this.ancestors.Add(this.node);
        if (childAncestors.Contains(child))
        {
            throw new YamlException(
                child.Start,
                child.End,
                "An alias refers to its own ancestor, which cannot be read as a rule tree."
            );
        }

        return new YamlNodeCursor(child, childAncestors);
    }

    private IEnumerable<ITreeNodeCursor> ElementsOf(YamlSequenceNode sequence)
    {
        foreach (YamlNode child in sequence.Children)
        {
            yield return this.Child(child);
        }
    }

    private IEnumerable<TreeMember> MembersOf(YamlMappingNode mapping)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
        {
            // The key cursor is always supplied (not only for a non-string key): YamlDotNet keeps every node's
            // position, so a diagnostic about a keyed-by-name construct, such as a 'definitions' entry's own name, can
            // locate itself from this key's real span rather than falling back to a path.
            YamlNodeCursor keyCursor = this.Child(entry.Key);
            yield return entry.Key is YamlScalarNode { Value: { } name }
                ? new TreeMember(name, this.Child(entry.Value), keyCursor)
                : new TreeMember(null, this.Child(entry.Value), keyCursor);
        }
    }
}
