namespace TruthWeaver.Tool.Core;

using TruthWeaver.Analysis;

/// <summary>The comparison of a rule with an earlier version of it, from <see cref="RewritePlayground.Compare"/>.</summary>
/// <param name="Equivalence">
/// Whether the two rules give the same value for every assignment of <c>True</c>, <c>False</c> and <c>Unknown</c>
/// (<see cref="RuleEquivalence"/>), or <see cref="RuleEquivalenceOutcome.Undecided"/> when they have more distinct terms
/// than <see cref="ToolSettings.MaxAnalysisTerms"/>.
/// </param>
/// <param name="Relation">
/// Whether the current rule is satisfied by the same, fewer (tighter) or more (looser) assignments than the earlier one
/// (<see cref="RuleImplication"/>), with the same cap.
/// </param>
public sealed record RuleComparison(RuleEquivalenceResult Equivalence, RuleRelationResult Relation);
