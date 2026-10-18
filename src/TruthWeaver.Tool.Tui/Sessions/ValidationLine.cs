namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Diagnostics;

/// <summary>One line of the validation panel.</summary>
/// <param name="Text">The text of the line.</param>
/// <param name="Span">The span of the diagnostic that the line shows, or <see langword="null"/> for a line with no place in the text.</param>
internal sealed record ValidationLine(string Text, SourceSpan? Span);
