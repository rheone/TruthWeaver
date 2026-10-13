namespace TruthWeaver.Tool.Core;

/// <summary>The outcome of writing back one edited rule.</summary>
/// <param name="Path">The normalized path of the rule in the document, such as <c>$.rules.publish</c>, or <see langword="null"/> when the whole document is the rule.</param>
/// <param name="Kind">What was done to the rule.</param>
public sealed record RulePatchOutcome(string? Path, RulePatchKind Kind);
