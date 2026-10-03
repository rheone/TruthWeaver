namespace TruthWeaver.Json;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

// IDISP004 false-positives on `foreach (var x in jsonElement.EnumerateObject()/.EnumerateArray())`:
// JsonElement's enumerators are disposable structs, but a `foreach` loop already compiles to a
// `using`-equivalent dispose in its generated finally block: there is no undisposed value here.
#pragma warning disable IDISP004

/// <summary>
/// The JSON adapter for the shared <see cref="TreeFormatReader"/>: a view of one <see cref="JsonElement"/>. A
/// <see cref="JsonElement"/> keeps no source positions, so <see cref="Span"/> is always <see cref="SourceSpan.None"/>;
/// <c>RuleCompiler.CompileJson</c> adds spans afterwards from the path (see <see cref="JsonSpanLocator"/>).
/// </summary>
/// <param name="element">The element this cursor views.</param>
internal readonly struct JsonNodeCursor(JsonElement element) : ITreeNodeCursor
{
    /// <summary>Gets the diagnostic wording for JSON trees.</summary>
    public static TreeFormatVocabulary Vocabulary { get; } =
        new(
            MappingNoun: "JSON object",
            SequenceNoun: "array",
            StringNoun: "JSON string",
            KeyNoun: "property",
            ConstMessage: "'const' must be a JSON boolean or one of \"true\", \"false\", \"unknown\".",
            ConstExpected: "a JSON boolean or one of \"true\", \"false\", \"unknown\""
        );

    /// <inheritdoc />
    public TreeNodeShape Shape =>
        element.ValueKind switch
        {
            JsonValueKind.Object => TreeNodeShape.Mapping,
            JsonValueKind.Array => TreeNodeShape.Sequence,
            JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => TreeNodeShape.Scalar,
            _ => TreeNodeShape.Other,
        };

    /// <inheritdoc />
    public SourceSpan Span => SourceSpan.None;

    /// <inheritdoc />
    public string KindName => element.ValueKind.ToString();

    /// <inheritdoc />
    public string? StringValue => element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    /// <inheritdoc />
    public IEnumerable<ITreeNodeCursor> Elements => element.ValueKind == JsonValueKind.Array ? ElementsOf(element) : [];

    /// <inheritdoc />
    public IEnumerable<TreeMember> Members => element.ValueKind == JsonValueKind.Object ? MembersOf(element) : [];

    /// <inheritdoc />
    public string UnsupportedLiteralMessage => $"Unsupported literal JSON value kind '{element.ValueKind}'.";

    /// <inheritdoc />
    public string Describe()
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            JsonValueKind.True or JsonValueKind.False => "a boolean",
            JsonValueKind.Array => "an array",
            JsonValueKind.Object => "an object",
            _ => "null",
        };
    }

    /// <inheritdoc />
    public string DescribeValue()
    {
        return this.Describe();
    }

    /// <inheritdoc />
    public bool TryGetChild(string key, [NotNullWhen(true)] out ITreeNodeCursor? child)
    {
        if (element.TryGetProperty(key, out JsonElement found))
        {
            child = new JsonNodeCursor(found);
            return true;
        }

        child = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetInt32(out int value)
    {
        // TryGetInt32 (not GetInt32) so a fractional or oversized number is a diagnostic instead of an exception.
        value = 0;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
    }

    /// <inheritdoc />
    public bool TryGetTruthValue(out TruthValue value)
    {
        // A constant is a JSON boolean (the original True/False form) or a string naming a K3 value, so
        // Unknown (which JSON has no literal for) can be written as "unknown" in any letter case.
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                value = TruthValue.True;
                return true;
            case JsonValueKind.False:
                value = TruthValue.False;
                return true;
            case JsonValueKind.String:
                return TruthValueText.TryParse(element.GetString(), out value);
            default:
                value = default;
                return false;
        }
    }

    /// <inheritdoc />
    public RawLiteral? ReadScalarLiteral()
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => RawLiteral.OfString(element.GetString()!, SourceSpan.None),
            JsonValueKind.Number => RawLiteral.OfNumber(element.GetRawText(), SourceSpan.None),
            JsonValueKind.True => RawLiteral.OfBoolean(true, SourceSpan.None),
            JsonValueKind.False => RawLiteral.OfBoolean(false, SourceSpan.None),
            _ => null,
        };
    }

    private static IEnumerable<ITreeNodeCursor> ElementsOf(JsonElement array)
    {
        foreach (JsonElement item in array.EnumerateArray())
        {
            yield return new JsonNodeCursor(item);
        }
    }

    private static IEnumerable<TreeMember> MembersOf(JsonElement obj)
    {
        foreach (JsonProperty property in obj.EnumerateObject())
        {
            yield return new TreeMember(property.Name, new JsonNodeCursor(property.Value));
        }
    }
}
