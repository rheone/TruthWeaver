namespace TruthWeaver.Analysis;

/// <summary>
/// How the satisfying assignments of an "after" rule relate to those of a "before" rule, the verdict of
/// <see cref="RuleImplication.Compare{TContext}"/>. An assignment of the terms satisfies a rule when the rule is
/// <c>True</c> for it; <c>False</c> and <c>Unknown</c> do not satisfy the rule.
/// </summary>
public enum RuleRelation
{
    /// <summary>The same assignments satisfy both rules. The rules can still differ in where they are <c>False</c> or <c>Unknown</c>.</summary>
    Equivalent,

    /// <summary>
    /// The after rule is tighter: every assignment that satisfies it also satisfies the before rule, and some assignment
    /// satisfies only the before rule. A rule that is never <c>True</c> is tighter than any rule that is sometimes <c>True</c>.
    /// </summary>
    Tighter,

    /// <summary>
    /// The after rule is looser: every assignment that satisfies the before rule also satisfies it, and some assignment
    /// satisfies only the after rule.
    /// </summary>
    Looser,

    /// <summary>
    /// Some assignment satisfies both rules, and each rule is satisfied by some assignment that does not satisfy the
    /// other.
    /// </summary>
    Overlapping,

    /// <summary>No assignment satisfies both rules, and each rule is satisfied by some assignment.</summary>
    Disjoint,

    /// <summary>
    /// No verdict was reached: the rules have more distinct terms than the configured cap, or their shared BDD would
    /// exceed the internal node budget. The result's <c>Reason</c> says which.
    /// </summary>
    Undecided,
}
