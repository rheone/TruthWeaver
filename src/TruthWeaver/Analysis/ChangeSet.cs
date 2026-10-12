namespace TruthWeaver.Analysis;

/// <summary>
/// A set of term changes that gives a rule the target value of a <see cref="RuleCounterfactual"/> search. The set is
/// minimal: no smaller part of it gives the rule the target value.
/// </summary>
/// <param name="Changes">The changes, in the order of the first occurrence of each term in the rule.</param>
public sealed record ChangeSet(EquatableArray<TermChange> Changes)
{
    /// <summary>Gets the number of term changes. <see cref="CounterfactualResult.ChangeSets"/> is in order of this number, smallest first.</summary>
    public int Count => this.Changes.Count;
}
