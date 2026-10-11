namespace TruthWeaver.Parsing;

/// <summary>
/// The one normal form of a node note, shared by the rule text, JSON and YAML readers so a note reads back the same from
/// every format: line endings become a line feed, each line loses its trailing whitespace, and the whole note loses its
/// leading and trailing blank space. A note that is empty after that is no note.
/// </summary>
internal static class NoteText
{
    /// <summary>Brings note text to its normal form.</summary>
    /// <param name="text">The note as written.</param>
    /// <returns>The normalized note, or <see langword="null"/> when nothing but whitespace is left.</returns>
    public static string? Normalize(string text)
    {
        string unified = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string normalized = string.Join('\n', unified.Split('\n').Select(line => line.TrimEnd())).Trim();
        return normalized.Length == 0 ? null : normalized;
    }
}
