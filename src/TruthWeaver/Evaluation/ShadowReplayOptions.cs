namespace TruthWeaver.Evaluation;

/// <summary>
/// The kind of difference between two decisions of the same context that <see cref="ShadowReplay"/> reports as a change.
/// </summary>
public enum ShadowReplayComparison
{
    /// <summary>The <see cref="Decision.Result"/> differs: any change among <c>True</c>, <c>False</c> and <c>Unknown</c>.</summary>
    Result = 0,

    /// <summary>The <see cref="Decision.IsSatisfied"/> value differs: only a change to or from <c>True</c>. A change between <c>False</c> and <c>Unknown</c> is not reported.</summary>
    IsSatisfied = 1,
}

/// <summary>Options for <see cref="ShadowReplay"/>.</summary>
/// <param name="Comparison">Which difference between the two decisions counts as a change. The default is <see cref="ShadowReplayComparison.Result"/>.</param>
/// <param name="IncludeFaultChanges">
/// <see langword="true"/> to also report a context whose set of <see cref="Decision.Faults"/> differs, even when the
/// <paramref name="Comparison"/> finds no difference. The default is <see langword="false"/>.
/// </param>
public sealed record ShadowReplayOptions(
    ShadowReplayComparison Comparison = ShadowReplayComparison.Result,
    bool IncludeFaultChanges = false
);
