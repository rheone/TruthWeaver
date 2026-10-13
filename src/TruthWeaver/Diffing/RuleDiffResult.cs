namespace TruthWeaver.Diffing;

using TruthWeaver.Analysis;

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
    /// <summary>
    /// Gets how the satisfying assignments of the "after" rule relate to those of the "before" rule (whether the change
    /// makes the rule looser or tighter), as <see cref="RuleImplication.Compare{TContext}"/> decides it:
    /// <see cref="RuleRelation.Equivalent"/> for structurally identical rules, <see cref="RuleRelation.Undecided"/> when
    /// the rules have more distinct terms than the term cap, or <see langword="null"/> when the result was built without
    /// the check.
    /// </summary>
    /// <remarks>
    /// This property is not a positional parameter, so the constructor and <c>Deconstruct</c> keep their two-parameter
    /// shape. Only <c>True</c> counts for the relation, so it can be <see cref="RuleRelation.Equivalent"/> while
    /// <see cref="PreservesMeaning"/> is <see langword="false"/>: for example <c>a</c> and <c>IsTrue(a)</c>.
    /// </remarks>
    public RuleRelation? Relation { get; init; }

    /// <summary>Gets a value indicating whether any difference was found.</summary>
    public bool HasChanges => this.Entries.Count > 0;
}
