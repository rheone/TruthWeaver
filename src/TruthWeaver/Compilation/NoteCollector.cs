namespace TruthWeaver.Compilation;

using TruthWeaver.Ast;
using TruthWeaver.Parsing;

/// <summary>
/// Collects the notes of a parsed tree by the tree path of the compiled node each one belongs to. The compiler builds
/// exactly one expression node for each raw node, with the operands in the same order, so the two trees can be walked
/// side by side; the notes stay beside the compiled tree and never enter it.
/// </summary>
internal static class NoteCollector
{
    /// <summary>Collects the notes of <paramref name="source"/> onto the paths of <paramref name="compiled"/>.</summary>
    /// <param name="source">The raw tree, with notes.</param>
    /// <param name="compiled">The tree the compiler built from <paramref name="source"/>.</param>
    /// <returns>The notes by tree path; empty when the source has none.</returns>
    public static Dictionary<string, string> Collect(RuleNode source, Expression compiled)
    {
        Dictionary<string, string> notes = [with(StringComparer.Ordinal)];
        Walk(source, compiled, TreePath.Root, notes);
        return notes;
    }

    private static void Walk(RuleNode source, Expression compiled, string path, Dictionary<string, string> notes)
    {
        if (source.Note is { } note)
        {
            notes[path] = note;
        }

        if (compiled is ConstantExpression or TermExpression)
        {
            return;
        }

        IReadOnlyList<RuleNode> sourceOperands = RuleNode.OperandsOf(source);
        IReadOnlyList<Expression> compiledOperands = ExpressionShape.Of(compiled).Operands;

        // The counts always agree for a tree that compiled; the check keeps a future mismatch from misplacing notes.
        if (sourceOperands.Count != compiledOperands.Count)
        {
            return;
        }

        string operandsPath = TreePath.Property(path, "operands");
        for (int i = 0; i < sourceOperands.Count; i++)
        {
            Walk(sourceOperands[i], compiledOperands[i], TreePath.Index(operandsPath, i), notes);
        }
    }
}
