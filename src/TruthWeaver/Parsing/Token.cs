namespace TruthWeaver.Parsing;

using System.Diagnostics;
using TruthWeaver.Diagnostics;

/// <summary>One lexical token, with its source span and (for literals) decoded text.</summary>
/// <param name="Kind">The token's kind.</param>
/// <param name="Text">The token's raw or decoded text (identifier name, or a literal's value text).</param>
/// <param name="Span">The token's location in the source text.</param>
[DebuggerDisplay("{Kind} '{Text,nq}'")]
internal readonly record struct Token(TokenKind Kind, string Text, SourceSpan Span);
