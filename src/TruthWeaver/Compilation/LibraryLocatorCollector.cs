namespace TruthWeaver.Compilation;

using TruthWeaver.Ast;
using TruthWeaver.Parsing;

/// <summary>
/// Collects the library locator of each compiled node that a <c>rule("name")</c> reference expanded to, keyed by the
/// compiled node itself. It walks the expanded raw tree and the compiled tree side by side, as <see cref="NoteCollector"/>
/// does; the locators stay beside the compiled tree and never enter it.
/// </summary>
internal static class LibraryLocatorCollector
{
    /// <summary>Collects the library locators of <paramref name="source"/> onto the nodes of <paramref name="compiled"/>.</summary>
    /// <param name="source">The expanded raw tree.</param>
    /// <param name="compiled">The tree the compiler built from <paramref name="source"/>.</param>
    /// <returns>The locator text by compiled node, compared by reference; empty when no node is from a library rule.</returns>
    public static Dictionary<Expression, string> Collect(RuleNode source, Expression compiled)
    {
        Dictionary<Expression, string> locators = [with(ReferenceEqualityComparer.Instance)];
        Walk(source, compiled, locators);
        return locators;
    }

    private static void Walk(RuleNode source, Expression compiled, Dictionary<Expression, string> locators)
    {
        if (source.Library is { } locator)
        {
            locators[compiled] = locator.ToString();
        }

        if (compiled is ConstantExpression or TermExpression)
        {
            return;
        }

        IReadOnlyList<RuleNode> sourceOperands = RuleNode.OperandsOf(source);
        IReadOnlyList<Expression> compiledOperands = ExpressionShape.Of(compiled).Operands;

        // The counts always agree for a tree that compiled; the check keeps a future mismatch from misplacing a locator.
        if (sourceOperands.Count != compiledOperands.Count)
        {
            return;
        }

        for (int i = 0; i < sourceOperands.Count; i++)
        {
            Walk(sourceOperands[i], compiledOperands[i], locators);
        }
    }
}
