namespace TruthWeaver.Analysis;

/// <summary>
/// Signals that building a shared BDD would exceed its node budget (ticket 21c, ADR-0009 decision 5). This is a
/// resource guard, not a business-logic control-flow exception: it crosses only the internal boundary between
/// <see cref="BddManager"/>/<see cref="Analyzer"/> and the handful of analysis entry points in this namespace
/// (<see cref="RuleEquivalence"/>, <see cref="RuleImplication"/>, <see cref="RuleOverlap"/>,
/// <see cref="RuleSetAnalysis"/>), each of which catches it and reports its own <c>Undecided</c>-shaped outcome with
/// a reason instead of letting it escape to a caller. No public API throws it.
/// </summary>
/// <param name="budget">The node budget that was exceeded.</param>
internal sealed class BddNodeBudgetExceededException(int budget)
    : InvalidOperationException($"Building the shared BDD would exceed its budget of {budget} decision nodes.")
{
    /// <summary>Gets the node budget that was exceeded.</summary>
    public int Budget { get; } = budget;
}
