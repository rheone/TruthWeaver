namespace TruthWeaver.Analysis;

/// <summary>The result of <see cref="RuleOverlap.Compare{TContext}"/>.</summary>
/// <param name="Outcome">The verdict.</param>
/// <param name="Reachable">
/// The reachable pairs, each with an example (ADR-0009 decision 1), in the fixed order <c>True</c>, <c>False</c>,
/// <c>Unknown</c> for each side. Empty when <paramref name="Outcome"/> is <see cref="RuleOverlapOutcome.Undecided"/>.
/// An entry here is a <em>candidate</em>, not a proof; a pair missing from this list is a <em>proof</em> that no real
/// application context can ever produce it, provided the predicate-author contract holds (ADR-0009 decision 4).
/// </param>
/// <param name="Reason">For <see cref="RuleOverlapOutcome.Undecided"/>, why no verdict was reached; otherwise <see langword="null"/>.</param>
public sealed record RuleOverlapResult(RuleOverlapOutcome Outcome, IReadOnlyList<JointOutcome> Reachable, string? Reason)
{
    /// <summary>
    /// Gets a value indicating whether the two rules can ever both be <c>True</c> for the same assignment (ADR-0009
    /// decision 2: <c>(True, True)</c> is reachable). <see langword="false"/> when <see cref="Outcome"/> is
    /// <see cref="RuleOverlapOutcome.Undecided"/> — check <see cref="Outcome"/> first; this member does not itself
    /// distinguish "proved never" from "not decided".
    /// </summary>
    public bool CanOverlap => this.Find(TruthValue.True, TruthValue.True) is not null;

    /// <summary>
    /// Gets a value indicating whether the two rules can ever disagree in the satisfaction reading: one rule
    /// <c>True</c> and the other not <c>True</c> for the same assignment (ADR-0009 decision 3: <c>(True, False)</c>,
    /// <c>(True, Unknown)</c>, <c>(False, True)</c> or <c>(Unknown, True)</c> is reachable). <see langword="false"/>
    /// when <see cref="Outcome"/> is <see cref="RuleOverlapOutcome.Undecided"/> — check <see cref="Outcome"/> first.
    /// This reading is noisier than the strict "both definite and opposite" question, which the <c>(True, False)</c>
    /// and <c>(False, True)</c> entries of <see cref="Reachable"/> answer on their own.
    /// </summary>
    public bool CanConflict =>
        this.Find(TruthValue.True, TruthValue.False) is not null
        || this.Find(TruthValue.True, TruthValue.Unknown) is not null
        || this.Find(TruthValue.False, TruthValue.True) is not null
        || this.Find(TruthValue.Unknown, TruthValue.True) is not null;

    /// <summary>Finds the reachable entry for one pair of values.</summary>
    /// <param name="first">The value of the first rule.</param>
    /// <param name="second">The value of the second rule.</param>
    /// <returns>The entry, or <see langword="null"/> when that pair is not reachable (or <see cref="Outcome"/> is <see cref="RuleOverlapOutcome.Undecided"/>).</returns>
    public JointOutcome? Find(TruthValue first, TruthValue second)
    {
        foreach (JointOutcome outcome in this.Reachable)
        {
            if (outcome.First == first && outcome.Second == second)
            {
                return outcome;
            }
        }

        return null;
    }
}
