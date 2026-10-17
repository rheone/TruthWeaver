namespace TruthWeaver.Evaluation;

/// <summary>
/// Per-call evaluation knobs (ADR-0002), passed to <c>CompiledRule.EvaluateAsync</c>. Defaults
/// preserve the engine's default semantics (unlimited fault tolerance, short-circuiting, no timeout). Every knob here is an explicit caller opt-in, not a change to those defaults.
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
/// <param name="MaxElementEvaluations">
/// The total number of quantifier element-body evaluations allowed across every <c>FORALL</c>/<c>EXISTS</c> in one
/// evaluation (ADR-0010 decision 6); nested quantifiers multiply, so a 50-element quantifier over a 50-element
/// quantifier counts as up to 2,500. Over the bound, the quantifier being evaluated becomes <c>Unknown</c> with one
/// fault and the whole evaluation aborts, the same way <see cref="FaultBudget"/> aborts. Unlike every other option
/// here, the default (10,000) is not unlimited: a rule's *author*, not the host calling <c>EvaluateAsync</c>,
/// controls how deeply a rule nests quantifiers and over what collections, so an unbounded default would let
/// authored rule text alone make one evaluation arbitrarily expensive.
/// </param>
/// <param name="IncludeTrace">
/// <see langword="true"/> to fill <c>Decision.Trace</c>, the flat log of every term and operator in evaluation order
/// (ADR-0001). The default <see langword="false"/> leaves <c>Decision.Trace</c> <see langword="null"/>. <c>Decision.TraceTree</c>
/// is always filled.
/// </param>
public sealed record EvaluationOptions(
    int? FaultBudget = null,
    EvaluationMode Mode = EvaluationMode.ShortCircuit,
    TimeSpan? Timeout = null,
    bool IncludeResolvedValues = false,
    int MaxElementEvaluations = 10_000,
    bool IncludeTrace = false
)
{
    /// <summary>
    /// Gets the default options: unlimited fault budget, <see cref="EvaluationMode.ShortCircuit"/>, no timeout, a
    /// 10,000 element-evaluation bound.
    /// </summary>
    public static EvaluationOptions Default { get; } = new();
}
