namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// One comment in rule text, kept by the lexer beside the token stream so the parser can attach it to a node as a note.
/// </summary>
/// <param name="Text">
/// The comment's content: for a <c>//</c> comment the rest of the line after <c>//</c> with one leading space removed;
/// for a <c>/* */</c> comment everything between the delimiters.
/// </param>
/// <param name="Span">Where the whole comment, delimiters included, sits in the source.</param>
/// <param name="IsLine"><see langword="true"/> for a <c>//</c> comment, which runs to the end of its line.</param>
internal readonly record struct Comment(string Text, SourceSpan Span, bool IsLine);
