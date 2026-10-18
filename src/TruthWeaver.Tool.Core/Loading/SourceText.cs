namespace TruthWeaver.Tool.Core.Loading;

/// <summary>
/// The decoded text of one input (a file, standard input or a string) and where it came from. The text has no byte order
/// mark and keeps its line endings, so the spans of a diagnostic point into it.
/// </summary>
/// <param name="Text">The decoded text, without the byte order mark.</param>
/// <param name="Origin">The file path, <c>-</c> for standard input, or the name the caller gave a string.</param>
/// <param name="Encoding">The encoding the reader found.</param>
public sealed record SourceText(string Text, string Origin, SourceEncoding Encoding);
