namespace TruthWeaver.Parsing;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;

/// <summary>The structural kind of a tree-format node, as far as the shared <see cref="TreeFormatReader"/> cares.</summary>
internal enum TreeNodeShape
{
    /// <summary>A keyed collection (a JSON object or a YAML mapping).</summary>
    Mapping,

    /// <summary>An ordered collection (a JSON array or a YAML sequence).</summary>
    Sequence,

    /// <summary>A single value (a string, number, boolean, or YAML scalar).</summary>
    Scalar,

    /// <summary>Anything else: JSON <c>null</c>, a YAML alias.</summary>
    Other,
}

/// <summary>
/// A read-only view of one node of a tree-format document (JSON or YAML), the seam between a
/// document model and the shared <see cref="TreeFormatReader"/>. An adapter implements it over its
/// own node type; the reader owns every rule about what a valid rule tree looks like, so the formats
/// cannot drift. Internal on purpose: the operator set is closed and the public surface stays small (ADR-0004).
/// </summary>
internal interface ITreeNodeCursor
{
    /// <summary>Gets the structural kind of this node.</summary>
    public TreeNodeShape Shape { get; }

    /// <summary>Gets the node's source range, or <see cref="SourceSpan.None"/> when the format keeps no positions.</summary>
    public SourceSpan Span { get; }

    /// <summary>Gets the format's own name for this node's kind, used in "Expected a mapping node but found X." messages.</summary>
    public string KindName { get; }

    /// <summary>Gets the text of this node when it is a string value; otherwise <see langword="null"/>.</summary>
    public string? StringValue { get; }

    /// <summary>Gets the children of a <see cref="TreeNodeShape.Sequence"/> node in order; empty for any other shape.</summary>
    public IEnumerable<ITreeNodeCursor> Elements { get; }

    /// <summary>Gets the entries of a <see cref="TreeNodeShape.Mapping"/> node in document order; empty for any other shape.</summary>
    public IEnumerable<TreeMember> Members { get; }

    /// <summary>Gets the message for a node that cannot be a predicate-argument literal (a mapping, <c>null</c>, an alias).</summary>
    public string UnsupportedLiteralMessage { get; }

    /// <summary>Names this node's shape for a diagnostic's <c>Found</c>, e.g. <c>a string</c> or <c>an array</c>.</summary>
    /// <returns>The description.</returns>
    public string Describe();

    /// <summary>Quotes this node's text when it is a scalar, or otherwise names its shape: for fields where the value itself is what is wrong.</summary>
    /// <returns>The description.</returns>
    public string DescribeValue();

    /// <summary>Looks up a property of a <see cref="TreeNodeShape.Mapping"/> node by name.</summary>
    /// <param name="key">The property name.</param>
    /// <param name="child">The property's value when found.</param>
    /// <returns><see langword="true"/> when the property exists.</returns>
    public bool TryGetChild(string key, [NotNullWhen(true)] out ITreeNodeCursor? child);

    /// <summary>Reads this node as a 32-bit integer; fails for any value that is not exactly one.</summary>
    /// <param name="value">The integer when successful.</param>
    /// <returns><see langword="true"/> when this node is an integer.</returns>
    public bool TryGetInt32(out int value);

    /// <summary>Reads this node as a K3 constant in the format's accepted spellings.</summary>
    /// <param name="value">The truth value when successful.</param>
    /// <returns><see langword="true"/> when this node is a valid constant.</returns>
    public bool TryGetTruthValue(out TruthValue value);

    /// <summary>Reads a scalar node as a predicate-argument literal.</summary>
    /// <returns>The literal, or <see langword="null"/> when this node is not a scalar the format supports as a literal.</returns>
    public RawLiteral? ReadScalarLiteral();
}
