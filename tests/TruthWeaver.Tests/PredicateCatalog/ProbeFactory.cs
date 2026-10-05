namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;

/// <summary>Creates one predicate registration, under the given name, over <see cref="TwinProbeContext"/>.</summary>
/// <param name="name">The name to register the predicate under.</param>
/// <returns>The schema and evaluation delegate of the predicate.</returns>
internal delegate (
    PredicateSchema Schema,
    Func<TwinProbeContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
) ProbeFactory(string name);
