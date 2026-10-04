namespace TruthWeaver.Diffing;

/// <summary>The result of <see cref="RuleDiff.Compare{TContext}"/>: every structural difference found, in tree order.</summary>
/// <param name="Entries">The diff entries, or empty when the two rules are structurally identical.</param>
/// <param name="PreservesMeaning">
/// Whether the two rules are equivalent under Strong K3 (same value for every True/False/Unknown assignment of their
/// terms): <see langword="true"/> when they are, even if the structure differs; <see langword="false"/> when they are
/// not; <see langword="null"/> when that could not be decided (more distinct terms than the default
/// <c>MaxAnalysisTerms</c>; call <c>RuleEquivalence.Compare</c> with a larger cap instead) or when the result was
/// built without the check.
/// </param>
public sealed record RuleDiffResult(IReadOnlyList<RuleDiffEntry> Entries, bool? PreservesMeaning = null)
{
    /// <summary>Gets a value indicating whether any difference was found.</summary>
    public bool HasChanges => this.Entries.Count > 0;
}
