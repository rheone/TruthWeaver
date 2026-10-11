namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Ast;

/// <summary>One generated rule: its expression tree, the notes put on it by path, and its text with and without them.</summary>
/// <param name="Tree">The generated expression tree.</param>
/// <param name="Notes">The notes, keyed by tree path (<c>$</c>, <c>$.operands[0]</c>, ...), already in normalized form.</param>
/// <param name="PlainText">The canonical text of the tree, with no notes.</param>
/// <param name="NotedText">The rule text with the notes written as comments.</param>
public sealed record NotedRule(Expression Tree, IReadOnlyDictionary<string, string> Notes, string PlainText, string NotedText);
