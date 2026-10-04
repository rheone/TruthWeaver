namespace TruthWeaver.Predicates.Tests;

/// <summary>A date context holding the selected, possibly missing, instant a date predicate reads.</summary>
/// <param name="When">The instant a date predicate reads.</param>
internal sealed record DateContext(DateTimeOffset? When);
