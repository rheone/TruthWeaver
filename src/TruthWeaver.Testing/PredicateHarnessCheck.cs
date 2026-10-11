namespace TruthWeaver.Testing;

/// <summary>The checks that <see cref="PredicateHarness.RunAsync{TContext}"/> runs against a predicate.</summary>
public enum PredicateHarnessCheck
{
    /// <summary>The predicate answers for the baseline arguments without an unexpected exception.</summary>
    Baseline,

    /// <summary>Two calls with the same arguments and context give the same answer, for every argument set the harness uses.</summary>
    Determinism,

    /// <summary>The predicate answers for one generated boundary value of one argument without an unexpected exception.</summary>
    BoundaryValue,

    /// <summary>The predicate reads every declared argument and reads no undeclared argument.</summary>
    SchemaConformance,

    /// <summary>What the predicate does with a cancelled token. The harness reports it and never fails it.</summary>
    Cancellation,
}
