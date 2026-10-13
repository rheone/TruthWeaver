namespace TruthWeaver.Tool.Core;

/// <summary>
/// Reads the text of an input from a file, a stream (such as standard input) or a string. The reader finds the encoding
/// from the byte order mark (UTF-8, UTF-16 or UTF-32, either byte order), removes the mark, and reads input with no mark as
/// UTF-8. An invalid byte is a <see cref="ToolDiagnosticCodes.InvalidEncoding"/> error, never a replacement character.
/// </summary>
public static class SourceReader
{
    /// <summary>The origin name of standard input.</summary>
    public const string StandardInput = "-";

    /// <summary>Reads a file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The text, or a <see cref="ToolDiagnosticCodes.InputUnreadable"/> or <see cref="ToolDiagnosticCodes.InvalidEncoding"/> error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<SourceText> ReadFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (IsFileError(ex))
        {
            return ToolResults.Failure<SourceText>(
                ToolDiagnosticCodes.InputUnreadable,
                $"The file '{path}' cannot be read: {ex.Message}"
            );
        }

        return Decode(bytes, path);
    }

    /// <summary>Reads a stream to its end. The stream stays open.</summary>
    /// <param name="stream">The stream, for example <see cref="Console.OpenStandardInput()"/>.</param>
    /// <param name="origin">The name to report the input by; <see cref="StandardInput"/> for standard input.</param>
    /// <returns>The text, or a <see cref="ToolDiagnosticCodes.InputUnreadable"/> or <see cref="ToolDiagnosticCodes.InvalidEncoding"/> error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="origin"/> is <see langword="null"/>.</exception>
    public static ToolResult<SourceText> ReadStream(Stream stream, string origin = StandardInput)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(origin);

        using MemoryStream buffer = new();
        try
        {
            stream.CopyTo(buffer);
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ObjectDisposedException)
        {
            return ToolResults.Failure<SourceText>(
                ToolDiagnosticCodes.InputUnreadable,
                $"The input '{origin}' cannot be read: {ex.Message}"
            );
        }

        return Decode(buffer.ToArray(), origin);
    }

    /// <summary>Wraps a string that is already decoded. A leading U+FEFF (a byte order mark that survived decoding) is removed.</summary>
    /// <param name="text">The text.</param>
    /// <param name="origin">The name to report the input by.</param>
    /// <returns>The text. This never fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="origin"/> is <see langword="null"/>.</exception>
    public static ToolResult<SourceText> ReadString(string text, string origin = "<string>")
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(origin);

        bool hasMark = text.StartsWith('﻿');
        return ToolResults.Success(
            new SourceText(hasMark ? text[1..] : text, origin, hasMark ? SourceEncoding.Utf8WithBom : SourceEncoding.Utf8)
        );
    }

    /// <summary>Decodes bytes: finds the byte order mark, removes it, and decodes the rest strictly.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="origin">The name to report the input by.</param>
    /// <returns>The text, or a <see cref="ToolDiagnosticCodes.InvalidEncoding"/> error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="bytes"/> or <paramref name="origin"/> is <see langword="null"/>.</exception>
    public static ToolResult<SourceText> Decode(byte[] bytes, string origin)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(origin);

        (SourceEncoding kind, int markLength) = DetectMark(bytes);
        Encoding encoding = StrictEncoding(kind);
        try
        {
            string text = encoding.GetString(bytes, markLength, bytes.Length - markLength);
            return ToolResults.Success(new SourceText(text, origin, kind));
        }
        catch (DecoderFallbackException ex)
        {
            // The offset counts from the start of the input, mark included, so it matches a hex dump of the file.
            int offset = markLength + Math.Max(ex.Index, 0);
            return ToolResults.Failure<SourceText>(
                ToolDiagnosticCodes.InvalidEncoding,
                $"The input '{origin}' is not valid {Describe(kind)}: the byte at offset {offset} cannot be decoded."
            );
        }
    }

    /// <summary>Gets the strict encoding for a kind: it throws on an invalid byte and writes no byte order mark.</summary>
    /// <param name="kind">The encoding kind.</param>
    /// <returns>The encoding.</returns>
    internal static Encoding StrictEncoding(SourceEncoding kind)
    {
        return kind switch
        {
            SourceEncoding.Utf16LittleEndian => new UnicodeEncoding(false, false, true),
            SourceEncoding.Utf16BigEndian => new UnicodeEncoding(true, false, true),
            SourceEncoding.Utf32LittleEndian => new UTF32Encoding(false, false, true),
            SourceEncoding.Utf32BigEndian => new UTF32Encoding(true, false, true),
            _ => new UTF8Encoding(false, true),
        };
    }

    private static (SourceEncoding Kind, int MarkLength) DetectMark(byte[] bytes)
    {
        // The UTF-32 little-endian mark starts with the UTF-16 little-endian mark, so it is tested first.
        return bytes switch
        {
            [0xFF, 0xFE, 0x00, 0x00, ..] => (SourceEncoding.Utf32LittleEndian, 4),
            [0x00, 0x00, 0xFE, 0xFF, ..] => (SourceEncoding.Utf32BigEndian, 4),
            [0xEF, 0xBB, 0xBF, ..] => (SourceEncoding.Utf8WithBom, 3),
            [0xFF, 0xFE, ..] => (SourceEncoding.Utf16LittleEndian, 2),
            [0xFE, 0xFF, ..] => (SourceEncoding.Utf16BigEndian, 2),
            _ => (SourceEncoding.Utf8, 0),
        };
    }

    private static string Describe(SourceEncoding kind)
    {
        return kind switch
        {
            SourceEncoding.Utf16LittleEndian or SourceEncoding.Utf16BigEndian => "UTF-16",
            SourceEncoding.Utf32LittleEndian or SourceEncoding.Utf32BigEndian => "UTF-32",
            _ => "UTF-8",
        };
    }

    private static bool IsFileError(Exception ex)
    {
        return ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;
    }
}
