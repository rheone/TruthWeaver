namespace TruthWeaver.Analysis;

/// <summary>The verdict of <see cref="RuleEquivalence.Compare{TContext}"/>.</summary>
public enum RuleEquivalenceOutcome
{
    /// <summary>The rules have the same value for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment of their terms.</summary>
    Equivalent,

    /// <summary>Some assignment gives the rules different values; see <see cref="RuleEquivalenceResult.CounterExample"/>.</summary>
    NotEquivalent,

    /// <summary>
    /// No verdict was reached: the rules have more distinct terms than the configured cap, or their shared BDD would
    /// exceed the internal node budget. The result's <c>Reason</c> says which.
    /// </summary>
    Undecided,
}
