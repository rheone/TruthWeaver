namespace TruthWeaver.Parsing;

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
