namespace TruthWeaver.Tests.TestSupport;

/// <summary>A context with an identity, so a test of many evaluations can tell the contexts apart.</summary>
/// <param name="Id">The position or identity of the context in a test batch.</param>
public sealed record BatchItem(int Id);
