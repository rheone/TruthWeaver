namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>Settings for one <see cref="PredicateHarness.RunAsync{TContext}"/> run.</summary>
public sealed record PredicateHarnessOptions
{
    /// <summary>Gets the default settings: generated baseline arguments and no expected faults.</summary>
    public static PredicateHarnessOptions Default { get; } = new();

    /// <summary>
    /// Gets the baseline argument values, keyed by argument name. A declared argument that is not in this set takes its
    /// schema default, or else a typical value of its <see cref="LiteralKind"/>. Each boundary-value case replaces one
    /// argument of the baseline set, so set a value here when the generated one makes the predicate fail for an unrelated
    /// reason, for example a <c>lower</c> bound that must stay below <c>upper</c>.
    /// </summary>
    public IReadOnlyDictionary<string, LiteralValue> Arguments { get; init; } = new Dictionary<string, LiteralValue>();

    /// <summary>
    /// Gets the exceptions that the predicate may throw. A matching exception reports as
    /// <see cref="PredicateHarnessStatus.ExpectedFault"/>, and every other exception is a failure.
    /// </summary>
    public IReadOnlyList<PredicateHarnessExpectedFault> ExpectedFaults { get; init; } = [];
}
