namespace TruthWeaver.Analysis;

/// <summary>
/// Reads the Strong K3 value of one tree for any assignment of its terms from the tree's dual-rail BDD, which is built
/// once. A truth table asks for thousands of rows, so building the diagram per row (as <c>Analyzer.ValueAt</c> does)
/// would be wasteful. The rule's semantics stay in <see cref="Analyzer"/>: this type only walks the finished diagram.
/// </summary>
/// <param name="terms">The tree's distinct terms in first-occurrence order, the order of the BDD variable pairs.</param>
/// <param name="bdd">The manager that owns the two rails.</param>
/// <param name="definite">The rail that is true exactly when the tree is <c>True</c>.</param>
/// <param name="possible">The rail that is true exactly when the tree is <c>True</c> or <c>Unknown</c>.</param>
/// <remarks>An instance holds the assignment that it is reading, so it is not safe for use from more than one thread.</remarks>
internal sealed class TermValuation(IReadOnlyList<TermIdentity> terms, BddManager bdd, int definite, int possible)
{
    private readonly TruthValue[] current = new TruthValue[terms.Count];

    /// <summary>Gets the distinct terms, in the order of first occurrence (depth first, left to right).</summary>
    public IReadOnlyList<TermIdentity> Terms { get; } = terms;

    /// <summary>Gets the tree's value when each term has the value that <paramref name="assignment"/> gives it.</summary>
    /// <param name="assignment">One value for each term, in <see cref="Terms"/> order.</param>
    /// <returns>The tree's Strong K3 value.</returns>
    public TruthValue ValueAt(IReadOnlyList<TruthValue> assignment)
    {
        for (int i = 0; i < this.current.Length; i++)
        {
            this.current[i] = assignment[i];
        }

        // Each term owns two BDD variables: 2i is "is True" and 2i + 1 is "is Unknown" (see Analyzer.Build).
        if (bdd.Evaluate(definite, this.ValueOf))
        {
            return TruthValue.True;
        }

        return bdd.Evaluate(possible, this.ValueOf) ? TruthValue.Unknown : TruthValue.False;
    }

    private bool ValueOf(int variable)
    {
        TruthValue value = this.current[variable / 2];
        return variable % 2 == 0 ? value == TruthValue.True : value == TruthValue.Unknown;
    }
}
