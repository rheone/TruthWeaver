namespace TruthWeaver.Generators.Model;

/// <summary>The outcome of reading one marked method: a valid method, or the diagnostics that make it invalid.</summary>
/// <param name="Method">The method to register, or <see langword="null"/> when the method is not valid.</param>
/// <param name="Diagnostics">The diagnostics for the method.</param>
internal sealed record MethodResult(PredicateMethod? Method, EquatableArray<DiagnosticInfo> Diagnostics);
