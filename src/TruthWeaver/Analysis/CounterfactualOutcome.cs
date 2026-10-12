namespace TruthWeaver.Analysis;

/// <summary>The verdict of a <see cref="RuleCounterfactual"/> search.</summary>
public enum CounterfactualOutcome
{
    /// <summary>At least one set of allowed changes gives the rule the target value; see <see cref="CounterfactualResult.ChangeSets"/>.</summary>
    Found,

    /// <summary>The facts already give the rule the target value, so no change is necessary.</summary>
    NoChangeNeeded,

    /// <summary>No set of changes to the terms that are not fixed gives the rule the target value.</summary>
    NonePossible,

    /// <summary>The search was not run because the rule has more distinct terms than the configured cap.</summary>
    Undecided,
}
