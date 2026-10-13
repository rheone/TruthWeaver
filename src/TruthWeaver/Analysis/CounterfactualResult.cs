namespace TruthWeaver.Analysis;

/// <summary>The result of a <see cref="RuleCounterfactual"/> search.</summary>
/// <param name="Outcome">The verdict.</param>
/// <param name="ChangeSets">
/// For <see cref="CounterfactualOutcome.Found"/>, the minimal change sets, fewest changes first; empty otherwise.
/// </param>
/// <param name="IsComplete">
/// <see langword="true"/> when <see cref="ChangeSets"/> holds every minimal change set. <see langword="false"/> when the
/// search stopped at <see cref="CounterfactualOptions.MaxChangeSets"/> or at its work limit, or did not run.
/// </param>
/// <param name="Reason">For <see cref="CounterfactualOutcome.Undecided"/>, why the search did not run; otherwise <see langword="null"/>.</param>
public sealed record CounterfactualResult(
    CounterfactualOutcome Outcome,
    EquatableArray<ChangeSet> ChangeSets,
    bool IsComplete,
    string? Reason
)
{
    /// <summary>
    /// Gets the change sets without a check of <see cref="Outcome"/> first, for example to show a "what to fix" list for a
    /// denied decision.
    /// </summary>
    /// <param name="changeSets">The change sets when this method returns <see langword="true"/>; otherwise empty.</param>
    /// <returns><see langword="true"/> when <see cref="Outcome"/> is <see cref="CounterfactualOutcome.Found"/>; otherwise <see langword="false"/>.</returns>
    public bool TryGetChangeSets(out EquatableArray<ChangeSet> changeSets)
    {
        changeSets = this.ChangeSets;
        return this.Outcome == CounterfactualOutcome.Found;
    }
}
