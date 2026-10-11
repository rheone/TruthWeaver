namespace TruthWeaver.Yaml;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// The YAML adapter for the shared <see cref="TreeFormatReader"/>: a view of one <see cref="YamlNode"/>. YamlDotNet keeps
/// node positions, so <see cref="Span"/> is the real source range of the node and each diagnostic is located by it.
/// </summary>
/// <param name="node">The node this cursor views.</param>
internal readonly struct YamlNodeCursor(YamlNode node) : ITreeNodeCursor
{
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
        node switch
        {
            YamlMappingNode => TreeNodeShape.Mapping,
            YamlSequenceNode => TreeNodeShape.Sequence,
            YamlScalarNode => TreeNodeShape.Scalar,
            _ => TreeNodeShape.Other,
        };

    /// <inheritdoc />
    public SourceSpan Span => SpanOf(node);

    /// <inheritdoc />
    public string KindName => node.NodeType.ToString();

    /// <inheritdoc />
    public string? StringValue => node is YamlScalarNode { Value: { } text } ? text : null;

    /// <inheritdoc />
    public IEnumerable<ITreeNodeCursor> Elements => node is YamlSequenceNode sequence ? ElementsOf(sequence) : [];

    /// <inheritdoc />
    public IEnumerable<TreeMember> Members => node is YamlMappingNode mapping ? MembersOf(mapping) : [];

    /// <inheritdoc />
    public string UnsupportedLiteralMessage =>
        IsNull(node)
            ? "Unsupported literal YAML value kind 'Null'."
            : $"Unsupported YAML node type '{node.NodeType}' for a literal value.";

    /// <inheritdoc />
    public string Describe()
    {
        return node switch
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
        return node is YamlScalarNode { Value: { } text } ? $"'{text}'" : this.Describe();
    }

    /// <inheritdoc />
    public bool TryGetChild(string key, [NotNullWhen(true)] out ITreeNodeCursor? child)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                if (entry.Key is YamlScalarNode { Value: { } keyText } && string.Equals(keyText, key, StringComparison.Ordinal))
                {
                    child = new YamlNodeCursor(entry.Value);
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
        return node is YamlScalarNode { Value: { } text }
            && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    /// <inheritdoc />
    public bool TryGetTruthValue(out TruthValue value)
    {
        // YAML has a native boolean, but a plain scalar's text is all the parser keeps, so true, false and unknown share one spelling rule.
        value = default;
        return node is YamlScalarNode { Value: { } text } && TruthValueText.TryParse(text, out value);
    }

    /// <inheritdoc />
    public RawLiteral? ReadScalarLiteral()
    {
        if (node is not YamlScalarNode scalar)
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

    private static IEnumerable<ITreeNodeCursor> ElementsOf(YamlSequenceNode sequence)
    {
        foreach (YamlNode child in sequence.Children)
        {
            yield return new YamlNodeCursor(child);
        }
    }

    private static IEnumerable<TreeMember> MembersOf(YamlMappingNode mapping)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
        {
            yield return entry.Key is YamlScalarNode { Value: { } name }
                ? new TreeMember(name, new YamlNodeCursor(entry.Value))
                : new TreeMember(null, new YamlNodeCursor(entry.Value), new YamlNodeCursor(entry.Key));
        }
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
}
