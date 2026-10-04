namespace TruthWeaver.Evaluation;

/// <summary>
/// Per-call evaluation knobs (ADR-0002), passed to <c>CompiledRule.EvaluateAsync</c>. Defaults
/// preserve the engine's default semantics (unlimited fault tolerance, short-circuiting, no timeout)
/// — every knob here is an explicit caller opt-in, not a change to those defaults.
/// </summary>
/// <param name="FaultBudget">
/// Once this many faults have been recorded during one evaluation, evaluation aborts immediately.
/// <see langword="null"/> (the default) means unlimited.
/// </param>
/// <param name="Mode">Whether short-circuiting applies (<see cref="EvaluationMode.ShortCircuit"/>) or every reachable term runs (<see cref="EvaluationMode.Exhaustive"/>).</param>
/// <param name="Timeout">
/// An overall wall-clock bound for the evaluation, linked into the caller's
/// <see cref="CancellationToken"/>. <see langword="null"/> (the default) means no timeout.
/// </param>
/// <param name="IncludeResolvedValues">
/// <see langword="true"/> to show the value each variable reference resolved to in the trace (ADR-0006 decision 13); the
/// default <see langword="false"/> names only the reference, because resolved data may be sensitive. Faults never include
/// resolved values whatever this is set to.
/// </param>
public sealed record EvaluationOptions(
    int? FaultBudget = null,
    EvaluationMode Mode = EvaluationMode.ShortCircuit,
    TimeSpan? Timeout = null,
    bool IncludeResolvedValues = false
)
{
    /// <summary>Gets the default options: unlimited fault budget, <see cref="EvaluationMode.ShortCircuit"/>, no timeout.</summary>
    public static EvaluationOptions Default { get; } = new();
}
