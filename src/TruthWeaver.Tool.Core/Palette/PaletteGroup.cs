namespace TruthWeaver.Tool.Core.Palette;

/// <summary>A run of palette entries that a search found, under one title.</summary>
/// <param name="Title">
/// The family title of the ready-made predicates in the group (such as <c>String</c>), or <see langword="null"/> for the
/// entries of the palette itself: constants, schema file, rule and definitions.
/// </param>
/// <param name="Entries">The entries of the group, in palette order.</param>
public sealed record PaletteGroup(string? Title, IReadOnlyList<PaletteEntry> Entries);
