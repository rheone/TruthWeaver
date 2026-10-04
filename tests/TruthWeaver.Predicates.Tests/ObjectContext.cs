namespace TruthWeaver.Predicates.Tests;

/// <summary>A minimal application context whose selected value is an arbitrary object.</summary>
/// <param name="Value">The object value an object-selector-based predicate reads.</param>
internal sealed record ObjectContext(object? Value);
