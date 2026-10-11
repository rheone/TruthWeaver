namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;

/// <summary>
/// A rule that <see cref="RuleLoader"/> read: its own text and format, and the input it came from. Loading does not
/// compile the rule; <see cref="RuleConverter.Compile"/> does.
/// </summary>
/// <param name="Source">The whole input, decoded.</param>
/// <param name="Detection">The format of the whole input and what decided it.</param>
/// <param name="Path">The normalized path of the rule inside the input, such as <c>$.rules.admin</c>, or <see langword="null"/> when the whole input is the rule.</param>
/// <param name="Format">The format of the rule itself. For an embedded rule written as a string, this is <see cref="RuleFormat.Text"/>.</param>
/// <param name="Text">The text of the rule itself, the text that diagnostic spans point into.</param>
public sealed record LoadedRule(SourceText Source, FormatDetection Detection, string? Path, RuleFormat Format, string Text);
