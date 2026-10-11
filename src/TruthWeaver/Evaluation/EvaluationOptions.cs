namespace TruthWeaver.Evaluation;

/// <summary>
/// Per-call evaluation knobs (ADR-0002), passed to <c>CompiledRule.EvaluateAsync</c>. Defaults
/// preserve the engine's default semantics (unlimited fault tolerance, short-circuiting, no timeout)
/// — every knob here is an explicit caller opt-in, not a change to those defaults.
/// </summary>
/// <param name="FaultBudget">
/// Evaluation aborts when the number of recorded faults reaches this value, so a budget of 1 aborts on the first fault.
/// <see langword="null"/> (the default) means unlimited. A value below 1 makes
/// <c>CompiledRule.EvaluateAsync</c> throw <see cref="ArgumentOutOfRangeException"/>.
/// </param>
/// <param name="Mode">Whether short-circuiting applies (<see cref="EvaluationMode.ShortCircuit"/>) or every reachable term runs (<see cref="EvaluationMode.Exhaustive"/>).</param>
/// <param name="Timeout">
/// An overall wall-clock bound for the evaluation, linked into the caller's
/// <see cref="CancellationToken"/>. <see langword="null"/> (the default) means no timeout. When the time expires, the
/// evaluation throws <see cref="OperationCanceledException"/>; the timeout is not a <c>Fault</c> and does not become
/// <c>Unknown</c>.
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
