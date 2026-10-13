namespace TruthWeaver.Analysis;

/// <summary>The result of <see cref="RuleImplication.Compare{TContext}"/>.</summary>
/// <param name="Relation">How the after rule relates to the before rule.</param>
/// <param name="SatisfiesOnlyBefore">
/// An assignment that makes the before rule <c>True</c> and the after rule <c>False</c> or <c>Unknown</c>, when one exists
/// (<see cref="RuleRelation.Tighter"/>, <see cref="RuleRelation.Overlapping"/> and <see cref="RuleRelation.Disjoint"/>):
/// every distinct term in either rule (keyed by its printed form, such as <c>hasRole(role: "Y")</c>) mapped to the value it
/// takes. Terms the example does not depend on are <see cref="TruthValue.False"/>. <see langword="null"/> otherwise.
/// </param>
/// <param name="SatisfiesOnlyAfter">
/// An assignment that makes the after rule <c>True</c> and the before rule <c>False</c> or <c>Unknown</c>, when one exists
/// (<see cref="RuleRelation.Looser"/>, <see cref="RuleRelation.Overlapping"/> and <see cref="RuleRelation.Disjoint"/>), in
/// the same form as <paramref name="SatisfiesOnlyBefore"/>. <see langword="null"/> otherwise.
/// </param>
/// <param name="Reason">For <see cref="RuleRelation.Undecided"/>, why no verdict was reached; otherwise <see langword="null"/>.</param>
public sealed record RuleRelationResult(
    RuleRelation Relation,
    IReadOnlyDictionary<string, TruthValue>? SatisfiesOnlyBefore,
    IReadOnlyDictionary<string, TruthValue>? SatisfiesOnlyAfter,
    string? Reason
);
