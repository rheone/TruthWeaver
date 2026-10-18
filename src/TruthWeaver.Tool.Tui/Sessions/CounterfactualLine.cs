namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Analysis;

/// <summary>One line of the What would make this True panel.</summary>
/// <param name="Text">The text of the line.</param>
/// <param name="Set">The change set that Enter on the line applies, or <see langword="null"/> for a line that only informs.</param>
internal sealed record CounterfactualLine(string Text, ChangeSet? Set);
