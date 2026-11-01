namespace TruthWeaver.Evaluation;

using Microsoft.Extensions.Logging;
using TruthWeaver.Logging;
using TruthWeaver.Metrics;

/// <summary>
/// The single path every absorbed fault goes through: a predicate's or a variable's own fault (via
/// <c>Evaluator&lt;TContext&gt;</c>'s private instance <c>RecordFault</c>) and a collection's (a throwing selector or
/// an exceeded element-evaluation limit, recorded by
/// <see cref="Registry.CollectionBinding{TContext, TItem}.EvaluateQuantifierAsync"/>, which has no
/// <c>Evaluator&lt;TContext&gt;</c> instance of its own) alike. Not a member of <c>Evaluator&lt;TContext&gt;</c>
/// itself, because nothing it does depends on <c>TContext</c> - it only reaches through <see cref="EvaluationState"/>,
/// <see cref="ILogger"/> and <see cref="EvaluationOptions"/>, so a collection-level fault is logged and counted
/// identically to a predicate fault instead of bypassing both (ultra-review ticket 51).
/// </summary>
internal static class FaultRecorder
{
    /// <summary>
    /// Records one absorbed fault: appends it to <paramref name="state"/>'s fault list, writes the structured
    /// evaluation log entry, increments the <c>truthweaver.faults</c> metric, and aborts once
    /// <paramref name="options"/>'s fault budget is reached.
    /// </summary>
    /// <param name="state">The evaluation's shared fault list and abort flag.</param>
    /// <param name="logger">The logger the fault is written to.</param>
    /// <param name="options">The evaluation options, for <see cref="EvaluationOptions.FaultBudget"/>.</param>
    /// <param name="scope">The enclosing element scope path, or <see langword="null"/> outside any quantifier.</param>
    /// <param name="origin">What faulted, so the log message names it (a predicate, a collection selector or the element limit).</param>
    /// <param name="identity">The faulting term's or collection's identity.</param>
    /// <param name="ex">The exception that caused the fault.</param>
    public static void Record(
        EvaluationState state,
        ILogger logger,
        EvaluationOptions options,
        string? scope,
        FaultOrigin origin,
        TermIdentity identity,
        Exception ex
    )
    {
        state.Faults.Add(new Fault(identity, ex, scope));
        EvaluationLog.Faulted(logger, origin, identity.ToString(), ex.Message, ex);
        TruthWeaverMetrics.FaultRecorded();

        // The budget is the number of faults at which evaluation stops (ADR-0002): a budget of 1 aborts on the first fault.
        if (options.FaultBudget is { } budget && state.Faults.Count >= budget)
        {
            state.Aborted = true;
        }
    }
}
