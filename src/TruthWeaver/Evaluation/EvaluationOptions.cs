namespace TruthWeaver.Evaluation;

/// <summary>
/// Per-call evaluation knobs (ADR-0002), passed to <c>CompiledRule.EvaluateAsync</c>. Defaults
/// preserve the engine's default semantics (unlimited fault tolerance, short-circuiting, no timeout). Every knob here is an explicit caller opt-in, not a change to those defaults.
/// </summary>
/// <param name="FaultBudget">
/// Evaluation aborts when the number of recorded faults reaches this value, so a budget of 1 aborts on the first fault.
/// Every term, operand or quantifier element not yet run when the evaluation aborts counts as
/// <see cref="Abstractions.TruthValue.Unknown"/> in the result. <see langword="null"/> (the default) means unlimited. A value below 1 makes
/// <c>CompiledRule.EvaluateAsync</c> throw <see cref="ArgumentOutOfRangeException"/>.
/// </param>
/// <param name="Mode">Whether short-circuiting applies (<see cref="EvaluationMode.ShortCircuit"/>) or every reachable term runs (<see cref="EvaluationMode.Exhaustive"/>).</param>
/// <param name="Timeout">
/// An overall wall-clock bound for the evaluation, linked into the caller's
/// <see cref="CancellationToken"/>. <see langword="null"/> (the default) means no timeout. When the time expires, the
/// evaluation throws <see cref="OperationCanceledException"/>; the timeout is not a <c>Fault</c> and does not become
/// <c>Unknown</c>. A negative value other than <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>, or one longer
/// than 4,294,967,294 ms, makes the evaluation call throw <see cref="ArgumentOutOfRangeException"/>. The timeout is
/// cooperative: it cancels a token, and the evaluation stops at the next point where it observes that token. A predicate that
/// blocks synchronously and never observes its <see cref="CancellationToken"/> runs to completion first, so the timeout
/// gives no hard wall-clock guarantee against such a predicate.
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
/// authored rule text alone make one evaluation arbitrarily expensive. A value below 1 makes the evaluation call throw
/// <see cref="ArgumentOutOfRangeException"/>.
/// </param>
/// <param name="IncludeTrace">
/// <see langword="true"/> to fill <c>Decision.Trace</c>, the flat log of every term and operator in evaluation order
/// (ADR-0001). The default <see langword="false"/> leaves <c>Decision.Trace</c> <see langword="null"/>. <c>Decision.TraceTree</c>
/// is always filled.
/// </param>
/// <param name="TracePredicates">
/// <see langword="true"/> to start one child span (<c>truthweaver.predicate</c>) of the <c>TruthWeaver</c>
/// <see cref="System.Diagnostics.ActivitySource"/> for each predicate invocation. The default <see langword="false"/>
/// starts only the evaluation span, because a span for each predicate costs allocation and exporter volume. It has no
/// effect while no <see cref="System.Diagnostics.ActivityListener"/> samples the source.
/// </param>
public sealed record EvaluationOptions(
    int? FaultBudget = null,
    EvaluationMode Mode = EvaluationMode.ShortCircuit,
    TimeSpan? Timeout = null,
    bool IncludeResolvedValues = false,
    int MaxElementEvaluations = 10_000,
    bool IncludeTrace = false,
    bool TracePredicates = false
)
{
    // The largest delay CancellationTokenSource.CancelAfter accepts, in milliseconds (uint.MaxValue is reserved by the timer).
    private const double MaxTimeoutMilliseconds = uint.MaxValue - 1d;

    /// <summary>
    /// Gets the default options: unlimited fault budget, <see cref="EvaluationMode.ShortCircuit"/>, no timeout, a
    /// 10,000 element-evaluation bound.
    /// </summary>
    public static EvaluationOptions Default { get; } = new();

    /// <summary>
    /// Rejects an option value the evaluator cannot honor, so a caller error surfaces at the call and not later as a
    /// late, undocumented exception from the timer or as a quantifier that faults on every element.
    /// </summary>
    /// <param name="paramName">The name of the caller's options parameter, reported by the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="FaultBudget"/> or <see cref="MaxElementEvaluations"/> is below 1, or <see cref="Timeout"/> is negative
    /// (other than <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>) or longer than 4,294,967,294 ms, the longest delay a timer accepts.
    /// </exception>
    internal void ThrowIfInvalid(string paramName)
    {
        // A budget below 1 could never be met, so it is a caller error, not a value to interpret.
        if (this.FaultBudget is < 1)
        {
            throw new ArgumentOutOfRangeException(paramName, this.FaultBudget, "The fault budget must be 1 or more.");
        }

        // A bound below 1 would fault every quantifier on its first element, whatever the rule says.
        if (this.MaxElementEvaluations < 1)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                this.MaxElementEvaluations,
                "The element evaluation bound must be 1 or more."
            );
        }

        if (
            this.Timeout is { } timeout
            && timeout != System.Threading.Timeout.InfiniteTimeSpan
            && (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > MaxTimeoutMilliseconds)
        )
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                timeout,
                "The timeout must be Timeout.InfiniteTimeSpan or between zero and 4,294,967,294 milliseconds."
            );
        }
    }
}
