namespace TruthWeaver.Tool.Core;

/// <summary>One node of the trace tree of an <see cref="AssignmentOutcome"/>: the value of a node and a plain reason for it.</summary>
/// <param name="Text">The node as the trace names it: an operator name such as <c>AND</c>, or a term.</param>
/// <param name="Value">The value of the node, or <see langword="null"/> when short-circuiting skipped the node.</param>
/// <param name="Reason">One plain sentence that says why the node has its value, and how an <c>Unknown</c> operand changes it.</param>
/// <param name="Children">The operands of the node, in order; empty for a term, a constant and a skipped node.</param>
public sealed record TraceLine(string Text, TruthValue? Value, string Reason, IReadOnlyList<TraceLine> Children)
{
    /// <summary>Gets a value indicating whether short-circuiting skipped this node, so it has no value.</summary>
    public bool NotEvaluated => this.Value is null;

    /// <summary>
    /// Compares by reference. Two nodes with the same text, value and reason, such as two unassigned terms, are different
    /// nodes of the tree, and a tree view that keeps its state by node must tell them apart.
    /// </summary>
    /// <param name="other">The other node.</param>
    /// <returns><see langword="true"/> when both are the same object.</returns>
    public bool Equals(TraceLine? other)
    {
        return ReferenceEquals(this, other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
}
