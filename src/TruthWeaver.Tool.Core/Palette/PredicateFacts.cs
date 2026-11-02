namespace TruthWeaver.Tool.Core.Palette;

/// <summary>What the palette knows about a predicate name.</summary>
/// <param name="Entry">The template: the schema file or the rule when it has the predicate, otherwise the catalog.</param>
/// <param name="Family">The catalog family of the predicate, or <see langword="null"/> when the catalog has no such predicate.</param>
/// <param name="TakesNullBehavior">
/// Whether the catalog factory takes a <c>NullBehavior</c>, so the predicate answers for a null selected value as the host
/// chose.
/// </param>
public sealed record PredicateFacts(PaletteEntry Entry, string? Family, bool TakesNullBehavior);
