namespace TruthWeaver.Evaluation;

/// <summary>One context for which the two rule versions of a <see cref="ShadowReplay"/> decided differently.</summary>
/// <typeparam name="TContext">The context type of the rules.</typeparam>
/// <param name="Index">The zero-based position of the context in the replayed sequence.</param>
/// <param name="Context">The context.</param>
/// <param name="Baseline">The decision of the baseline rule, with its trace and trace tree.</param>
/// <param name="Candidate">The decision of the candidate rule, with its trace and trace tree.</param>
public sealed record ShadowReplayChange<TContext>(int Index, TContext Context, Decision Baseline, Decision Candidate)
{
    /// <summary>Gets a value indicating whether the <see cref="Decision.Result"/> differs between the two decisions.</summary>
    public bool ResultChanged => this.Baseline.Result != this.Candidate.Result;

    /// <summary>Gets a value indicating whether <see cref="Decision.IsSatisfied"/> differs between the two decisions.</summary>
    public bool SatisfactionChanged => this.Baseline.IsSatisfied != this.Candidate.IsSatisfied;

    /// <summary>Gets a value indicating whether the set of faults differs between the two decisions.</summary>
    public bool FaultsChanged => ShadowReplay.FaultsDiffer(this.Baseline, this.Candidate);
}
