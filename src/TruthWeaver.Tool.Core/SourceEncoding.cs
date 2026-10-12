namespace TruthWeaver.Tool.Core;

/// <summary>The encoding of an input, as <see cref="SourceReader"/> found it from the byte order mark.</summary>
public enum SourceEncoding
{
    /// <summary>UTF-8 with no byte order mark. The reader uses it when the input has no mark.</summary>
    Utf8,

    /// <summary>UTF-8 with the byte order mark <c>EF BB BF</c>.</summary>
    Utf8WithBom,

    /// <summary>UTF-16, little-endian, with the byte order mark <c>FF FE</c>.</summary>
    Utf16LittleEndian,

    /// <summary>UTF-16, big-endian, with the byte order mark <c>FE FF</c>.</summary>
    Utf16BigEndian,

    /// <summary>UTF-32, little-endian, with the byte order mark <c>FF FE 00 00</c>.</summary>
    Utf32LittleEndian,

    /// <summary>UTF-32, big-endian, with the byte order mark <c>00 00 FE FF</c>.</summary>
    Utf32BigEndian,
}
