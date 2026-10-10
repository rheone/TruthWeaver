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

    /// <summary>Pauses reporting while the caller tries a rewrite that it can still discard.</summary>
    public void Pause()
    {
        this.pausedDepth++;
    }

    /// <summary>Resumes reporting after <see cref="Pause"/>.</summary>
    public void Resume()
    {
        this.pausedDepth--;
    }

    /// <summary>Discards every recorded step, for a result that falls back to the original rule.</summary>
    public void Clear()
    {
        steps.Clear();
    }
}
