namespace TruthWeaver.Analysis;

/// <summary>The verdict of <see cref="RuleEquivalence.Compare{TContext}"/>.</summary>
public enum RuleEquivalenceOutcome
{
    /// <summary>The rules have the same value for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment of their terms.</summary>
    Equivalent,

    /// <summary>Some assignment gives the rules different values; see <see cref="RuleEquivalenceResult.CounterExample"/>.</summary>
    NotEquivalent,

    /// <summary>The check was not run because the rules have more distinct terms than the configured cap.</summary>
    Undecided,
}
