namespace TruthWeaver.Tool.Tui.Predicates;

using TruthWeaver.Tool.Core.Palette;

/// <summary>A ready-made predicate of the <c>TruthWeaver.Predicates</c> package, as the palette offers it.</summary>
/// <param name="Family">The family title, such as <c>String</c> or <c>Date and time</c>.</param>
/// <param name="Label">The short display name of the predicate, such as <c>Starts With</c>.</param>
/// <param name="Entry">The template: the name, the description and the arguments with their kinds.</param>
internal sealed record CatalogPredicate(string Family, string Label, PaletteEntry Entry);
