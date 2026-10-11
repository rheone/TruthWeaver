namespace TruthWeaver.Testing;

/// <summary>The result of one <see cref="PredicateHarnessOutcome"/>.</summary>
public enum PredicateHarnessStatus
{
    /// <summary>The check holds.</summary>
    Passed,

    /// <summary>The check does not hold. <see cref="PredicateHarnessOutcome.Detail"/> gives the reason.</summary>
    Failed,

    /// <summary>
    /// The predicate threw an exception that the author allow-lists in <see cref="PredicateHarnessOptions.ExpectedFaults"/>.
    /// The engine still records a <c>Fault</c> for it, but the harness does not count it as a failure.
    /// </summary>
    ExpectedFault,

    /// <summary>The harness records what happened and makes no claim. The cancellation check always has this status.</summary>
    Observed,
}
