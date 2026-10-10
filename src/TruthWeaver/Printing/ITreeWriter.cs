namespace TruthWeaver.Printing;

/// <summary>
/// A write-only sink for one tree-format document (JSON or YAML), the seam between the shared
/// <see cref="TreeFormatEmitter"/> and a document model. It mirrors the read-side
/// <c>ITreeNodeCursor</c>: the emitter owns every decision about keys and structure, so an adapter only
/// maps these calls onto its own node type and the formats cannot drift. Calls must be well nested; a
/// <see cref="Property"/> call is always followed by exactly one value (a scalar, an object or an array).
/// Internal on purpose: the operator set is closed and the public surface stays small (ADR-0004).
/// </summary>
internal interface ITreeWriter
{
    /// <summary>Opens a keyed collection (a JSON object or a YAML mapping).</summary>
    public void BeginObject();

    /// <summary>Closes the keyed collection opened by the matching <see cref="BeginObject"/>.</summary>
    public void EndObject();

    /// <summary>Opens an ordered collection (a JSON array or a YAML sequence).</summary>
    public void BeginArray();

    /// <summary>Closes the ordered collection opened by the matching <see cref="BeginArray"/>.</summary>
    public void EndArray();

    /// <summary>Writes the key of the next member of the open object. The value follows in the next call.</summary>
    /// <param name="name">The key.</param>
    public void Property(string name);

    /// <summary>Writes a string value.</summary>
    /// <param name="value">The text.</param>
    /// <param name="quoted">
    /// <see langword="true"/> when the value is user data that must never be re-read as another literal
    /// kind (a string that looks like <c>1</c> or <c>true</c>), so a format with plain scalars must quote it.
    /// <see langword="false"/> for fixed vocabulary such as operator names. JSON strings are always quoted.
    /// </param>
    public void String(string value, bool quoted);

    /// <summary>Writes an integer value.</summary>
    /// <param name="value">The number.</param>
    public void Int64(long value);

    /// <summary>Writes a decimal value.</summary>
    /// <param name="value">The number.</param>
    public void Decimal(decimal value);

    /// <summary>Writes a boolean value.</summary>
    /// <param name="value">The flag.</param>
    public void Boolean(bool value);
}
