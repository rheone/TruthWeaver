namespace TruthWeaver.Evaluation;

/// <summary>
/// What a data source answered to one <see cref="VariableReference"/>: its matches, or the failure to replay.
/// Shared (not generic over <c>TContext</c>) so it can live on <see cref="EvaluationState"/>, which every nested
/// <c>Evaluator&lt;TContext&gt;</c> a quantifier's element iteration creates shares with its parent.
/// </summary>
internal sealed record SourceAnswer(IReadOnlyList<LiteralValue> Matches, VariableResolutionException? Failure)
{
    /// <summary>Creates an answer that resolved successfully.</summary>
    /// <param name="matches">The matched values.</param>
    /// <returns>The answer.</returns>
    public static SourceAnswer Answered(IReadOnlyList<LiteralValue> matches)
    {
        return new(matches, null);
    }

    /// <summary>Creates an answer that failed, so a repeated reference replays the same failure instead of re-querying.</summary>
    /// <param name="failure">The failure to replay.</param>
    /// <returns>The answer.</returns>
    public static SourceAnswer Failed(VariableResolutionException failure)
    {
        return new([], failure);
    }
}
