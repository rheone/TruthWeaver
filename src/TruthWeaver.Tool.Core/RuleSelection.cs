namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;

/// <summary>A rule that <see cref="EmbeddedRuleReader"/> selected from a document.</summary>
/// <param name="Format">The rule's format: the document's format for an object, rule text for a string.</param>
/// <param name="Text">The rule's own text.</param>
internal sealed record RuleSelection(RuleFormat Format, string Text);
