namespace TruthWeaver.Abstractions;

using System.Diagnostics;

/// <summary>
/// Records that a predicate failed to produce an answer for one term during one evaluation
/// (ADR-0001). The evaluator absorbs the exception rather than letting it propagate, and treats the
/// term as <see cref="TruthValue.Unknown"/> for the remainder of that evaluation.
/// </summary>
/// <param name="Term">The identity of the term that faulted.</param>
/// <param name="Exception">The exception the predicate raised (or propagated from its own dependencies).</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record Fault(TermIdentity Term, Exception Exception)
{
    // The default record ToString embeds Exception.ToString(), stack trace included, which buries the term and the cause.
    private string DebuggerDisplay => $"{this.Term}: {this.Exception.GetType().Name}: {this.Exception.Message}";
}
