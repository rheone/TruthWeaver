namespace TruthWeaver.Analysis;

/// <summary>The verdict of <see cref="RuleOverlap.Compare{TContext}"/>.</summary>
public enum RuleOverlapOutcome
{
    /// <summary>
    /// The joint-outcome table was built; see <see cref="RuleOverlapResult.Reachable"/> for which of the nine pairs
    /// some assignment of the terms can produce.
    /// </summary>
    Decided,

    /// <summary>The check was not run because the rules have more distinct terms than the configured cap.</summary>
    Undecided,
}
