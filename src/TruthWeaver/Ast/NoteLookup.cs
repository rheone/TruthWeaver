namespace TruthWeaver.Ast;

using TruthWeaver.Parsing;

/// <summary>
/// The notes of one compiled tree, looked up by node for the printers. Notes are stored by tree path
/// (<c>CompiledRule.Notes</c>); a printer walks nodes, not paths, so this maps each noted path to the node object at that
/// path once. The lookup is by reference: a compiled tree never shares one node object between two places (the compiler
/// builds a new node for every source node), so each node has exactly one path.
/// </summary>
internal sealed class NoteLookup
{
    private readonly Dictionary<Expression, string> byNode = [with(ReferenceEqualityComparer.Instance)];

    private NoteLookup() { }

    /// <summary>Builds the lookup for <paramref name="root"/>, or returns <see langword="null"/> when there are no notes.</summary>
    /// <param name="root">The tree the notes belong to.</param>
    /// <param name="notes">The notes by tree path.</param>
    /// <returns>The lookup, or <see langword="null"/> for no notes.</returns>
    public static NoteLookup? Create(Expression root, IReadOnlyDictionary<string, string> notes)
    {
        if (notes.Count == 0)
        {
            return null;
        }

        NoteLookup lookup = new();
        lookup.Add(root, TreePath.Root, notes);
        return lookup;
    }

    /// <summary>Gets the note on <paramref name="node"/>.</summary>
    /// <param name="node">A node of the tree this lookup was built for.</param>
    /// <returns>The note, or <see langword="null"/> for none.</returns>
    public string? Of(Expression node)
    {
        return this.byNode.GetValueOrDefault(node);
    }

    private void Add(Expression node, string path, IReadOnlyDictionary<string, string> notes)
    {
        if (notes.TryGetValue(path, out string? note))
        {
            this.byNode[node] = note;
        }

        if (node is ConstantExpression or TermExpression)
        {
            return;
        }

        IReadOnlyList<Expression> operands = ExpressionShape.Of(node).Operands;
        string operandsPath = TreePath.Property(path, "operands");
        for (int i = 0; i < operands.Count; i++)
        {
            this.Add(operands[i], TreePath.Index(operandsPath, i), notes);
        }
    }
}
