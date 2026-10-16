namespace TruthWeaver.Abstractions;

using System.Diagnostics;

/// <summary>
/// Records that a predicate failed to produce an answer for one term during one evaluation
/// (ADR-0001). The evaluator absorbs the exception rather than letting it propagate, and treats the
/// term as <see cref="TruthValue.Unknown"/> for the remainder of that evaluation.
/// </summary>
/// <remarks>
/// Two faults are equal when their terms are equal and their exceptions have the same <see cref="ExceptionType"/> and
/// message; two <see cref="VariableResolutionException"/>s must also have the same reference and failure kind. The
/// exception object itself is not compared, so the same failure in two evaluations gives equal faults, and a fault read
/// back from a stored decision record (with a <see cref="RecordedFaultException"/>) equals the fault that was written.
/// </remarks>
/// <param name="Term">The identity of the term that faulted.</param>
/// <param name="Exception">The exception the predicate raised (or propagated from its own dependencies).</param>
/// <param name="Scope">
/// An optional, additive label for where inside a quantifier's element iteration this fault happened, such as
/// <c>line[734]</c>, or a dotted chain for nested quantifiers (<c>line[2]/tag[0]</c>) (ADR-0010 decision 7). <see
/// langword="null"/> for a fault outside any quantifier body, which is every fault before this field existed. It is not
/// part of equality or the hash code, so a fault recorded with a scope still equals the same fault recorded, or read
/// back from a decision record, without one.
/// </param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record Fault(TermIdentity Term, Exception Exception, string? Scope = null)
{
    /// <summary>
    /// Gets the full type name of <see cref="Exception"/>. For a <see cref="RecordedFaultException"/> it is the type name
    /// that was recorded, not <c>RecordedFaultException</c>.
    /// </summary>
    public string ExceptionType =>
        this.Exception is RecordedFaultException recorded
            ? recorded.ExceptionType
            : this.Exception.GetType().FullName ?? this.Exception.GetType().Name;

    // The default record ToString embeds Exception.ToString(), stack trace included, which buries the term and the cause.
    // ExceptionType names the recorded type for a fault read back from a decision record, not RecordedFaultException.
    private string DebuggerDisplay =>
        this.Scope is null
            ? $"{this.Term}: {this.ExceptionType}: {this.Exception.Message}"
            : $"{this.Scope} {this.Term}: {this.ExceptionType}: {this.Exception.Message}";

    /// <inheritdoc />
    public bool Equals(Fault? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.Term.Equals(other.Term)
            && string.Equals(this.ExceptionType, other.ExceptionType, StringComparison.Ordinal)
            && string.Equals(this.Exception.Message, other.Exception.Message, StringComparison.Ordinal)
            && SameVariableFailure(this.Exception, other.Exception);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Term, this.ExceptionType, this.Exception.Message);
    }

    // A variable failure is identified by its reference and kind as well as its message, so both are compared when both
    // sides carry them.
    private static bool SameVariableFailure(Exception left, Exception right)
    {
        return left is not VariableResolutionException a
            || right is not VariableResolutionException b
            || (a.Reference.Equals(b.Reference) && a.Kind == b.Kind);
    }
}
