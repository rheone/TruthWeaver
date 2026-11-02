namespace TruthWeaver.Tests.TestSupport;

/// <summary>
/// A minimal application context used across tests - the engine has no opinion about its shape. Reuse it instead of
/// declaring a new empty context class; <c>TestPredicates</c> registers predicates for this context.
/// </summary>
#pragma warning disable S2094 // Intentionally empty: a marker TContext, not a stand-in for an interface.
public sealed class RuleTestContext;
#pragma warning restore S2094
