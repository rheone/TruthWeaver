namespace TruthWeaver.Tool.Core.Reports;

/// <summary>Whether a whole rule has the same value for every assignment of <c>True</c>, <c>False</c> and <c>Unknown</c> to its terms.</summary>
public enum RuleConstancy
{
    /// <summary>The value of the rule depends on its terms.</summary>
    Contingent,

    /// <summary>The rule is <c>True</c> for every assignment: a Strong K3 tautology.</summary>
    Tautology,

    /// <summary>The rule is <c>False</c> for every assignment: a Strong K3 contradiction.</summary>
    Contradiction,

    /// <summary>The check did not run, because the rule has more distinct terms than the analysis limit.</summary>
    Undecided,
}
