namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>The result of one check, or of one case of a check, in a <see cref="PredicateHarnessReport"/>.</summary>
/// <param name="Check">The check that produced this outcome.</param>
/// <param name="Status">Whether the check passed, failed, met an expected fault or was only observed.</param>
/// <param name="Case">The arguments or the subject of the check, for example <c>min = Int64.MaxValue</c>.</param>
/// <param name="Detail">What happened, and for a failure, the reason.</param>
/// <param name="Value">
/// The value that the engine records for the call: the predicate's answer, or <see cref="TruthValue.Unknown"/> when the
/// predicate threw. <see langword="null"/> for an outcome that does not come from one call, such as schema conformance.
/// </param>
/// <param name="Exception">
/// The exception that the predicate threw, which the engine records as a <c>Fault</c>; <see langword="null"/> when the
/// predicate threw nothing.
/// </param>
public sealed record PredicateHarnessOutcome(
    PredicateHarnessCheck Check,
    PredicateHarnessStatus Status,
    string Case,
    string Detail,
    TruthValue? Value = null,
    Exception? Exception = null
)
{
    /// <summary>
    /// Gets a value indicating whether the engine records a <c>Fault</c> for this call. An <see cref="TruthValue.Unknown"/>
    /// value without a fault is a valid answer: the predicate decided that it cannot answer.
    /// </summary>
    public bool HasFault => this.Exception is not null;

    /// <summary>Returns the outcome as one line: check, status, case and detail.</summary>
    /// <returns>The outcome text.</returns>
    public override string ToString()
    {
        return $"{this.Check} {this.Status}: {this.Case}: {this.Detail}";
    }
}
