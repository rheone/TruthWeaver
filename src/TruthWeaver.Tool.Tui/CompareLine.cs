namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Evaluation;

/// <summary>One line of the Compare pane.</summary>
/// <param name="Text">The text of the line.</param>
/// <param name="Example">The assignment that Enter on the line loads into the Assign pane, or <see langword="null"/> for a line that only informs.</param>
internal sealed record CompareLine(string Text, IReadOnlyDictionary<string, TruthValue>? Example = null);
