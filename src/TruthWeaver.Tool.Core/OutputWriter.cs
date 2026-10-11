namespace TruthWeaver.Tool.Core;

/// <summary>
/// Writes tool output deterministically: the same text gives the same bytes on every platform and every run. The writer
/// turns every line ending into a line feed (LF), ends non-empty output with exactly one line feed, and encodes the
/// result as UTF-8 with no byte order mark. <see cref="Check"/> compares a file with the output byte for byte, for a
/// <c>--check</c> option that fails when a file is out of date.
/// </summary>
public static class OutputWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    /// <summary>
    /// Normalizes text for output: CRLF and CR become LF, trailing line feeds become one line feed, and text with no
    /// content but line feeds becomes empty. Text that is already normalized does not change.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The normalized text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string lf = text.ReplaceLineEndings("\n");
        string body = lf.TrimEnd('\n');
        return body.Length == 0 ? string.Empty : body + "\n";
    }

    /// <summary>Gets the bytes that the writer writes for <paramref name="text"/>: the normalized text in UTF-8 with no byte order mark.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static byte[] Encode(string text)
    {
        return Utf8NoBom.GetBytes(Normalize(text));
    }

    /// <summary>Writes normalized text to a writer, such as standard output.</summary>
    /// <param name="writer">The writer. Its own encoding applies; the writer here controls only the line endings and the final newline.</param>
    /// <param name="text">The text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    public static void Write(TextWriter writer, string text)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(Normalize(text));
    }

    /// <summary>
    /// Writes normalized text to a file, and creates the directory when it does not exist. The writer always writes the
    /// file, so its bytes are exactly <see cref="Encode"/> of the text after a success.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="text">The text.</param>
    /// <returns>The full path of the file, or a <see cref="ToolDiagnosticCodes.OutputFailed"/> error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> WriteFile(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(path);
        byte[] bytes = Encode(text);
        try
        {
            string fullPath = Path.GetFullPath(path);
            if (Path.GetDirectoryName(fullPath) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, bytes);
            return ToolResults.Success(fullPath);
        }
        catch (Exception ex) when (IsFileError(ex))
        {
            return ToolResults.Failure<string>(
                ToolDiagnosticCodes.OutputFailed,
                $"The file '{path}' cannot be written: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Compares a file with the output for <paramref name="text"/>, byte for byte. The file does not change. A file with
    /// CRLF line endings, a byte order mark or a missing final newline differs.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="text">The text the file should hold.</param>
    /// <returns>The comparison, or a <see cref="ToolDiagnosticCodes.OutputFailed"/> error when the file exists but cannot be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    public static ToolResult<OutputCheck> Check(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(path);
        byte[] expected = Encode(text);
        if (!File.Exists(path))
        {
            return ToolResults.Success(new OutputCheck(path, OutputCheckStatus.Missing, null));
        }

        byte[] actual;
        try
        {
            actual = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (IsFileError(ex))
        {
            return ToolResults.Failure<OutputCheck>(
                ToolDiagnosticCodes.OutputFailed,
                $"The file '{path}' cannot be read: {ex.Message}"
            );
        }

        int? line = FirstDifferentLine(expected, actual);
        return ToolResults.Success(
            new OutputCheck(path, line is null ? OutputCheckStatus.Identical : OutputCheckStatus.Differs, line)
        );
    }

    // The line of the first differing byte, counted by the line feeds before it, or null when the bytes are equal.
    private static int? FirstDifferentLine(byte[] expected, byte[] actual)
    {
        int length = Math.Min(expected.Length, actual.Length);
        int line = 1;
        for (int i = 0; i < length; i++)
        {
            if (expected[i] != actual[i])
            {
                return line;
            }

            if (expected[i] == (byte)'\n')
            {
                line++;
            }
        }

        return expected.Length == actual.Length ? null : line;
    }

    private static bool IsFileError(Exception ex)
    {
        return ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;
    }
}
