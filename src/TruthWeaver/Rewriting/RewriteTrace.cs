namespace TruthWeaver.Rewriting;

using TruthWeaver.Ast;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;

/// <summary>
/// Collects the steps of a rewrite. A rewrite that does not report passes <see langword="null"/> instead, so the plain
/// <c>Simplify</c> and <c>Canonicalize</c> pay for no printing.
/// </summary>
/// <param name="steps">The list that receives the steps.</param>
internal sealed class RewriteTrace(List<RewriteStep> steps)
{
    private int pausedDepth;

    /// <summary>Records one step, unless the trace is paused.</summary>
    /// <param name="law">The law applied.</param>
    /// <param name="before">The subtree before the step.</param>
    /// <param name="after">The subtree after the step.</param>
    public void Report(RewriteLaw law, Expression before, Expression after)
    {
        if (this.pausedDepth == 0)
        {
            steps.Add(new RewriteStep(law, CanonicalPrinter.Print(before), CanonicalPrinter.Print(after)));
        }
    }

    /// <summary>
    /// Pauses reporting while the caller tries a rewrite that it can still discard. Disposing the result resumes reporting,
    /// so a <see langword="using"/> block keeps an exception from leaving the trace paused. Pauses nest.
    /// </summary>
    /// <returns>The scope to dispose when the trial ends.</returns>
    public PauseScope Pause()
    {
        this.pausedDepth++;
        return new PauseScope(this);
    }

    /// <summary>Discards every recorded step, for a result that falls back to the original rule.</summary>
    public void Clear()
    {
        steps.Clear();
    }

    /// <summary>The scope of one <see cref="Pause"/>; disposing it resumes reporting.</summary>
    /// <param name="owner">The trace that was paused.</param>
    internal readonly struct PauseScope(RewriteTrace owner) : IDisposable
    {
        /// <summary>Ends the pause.</summary>
        public void Dispose()
        {
            owner.pausedDepth--;
        }
    }
}
