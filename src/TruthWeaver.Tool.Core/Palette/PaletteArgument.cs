namespace TruthWeaver.Tool.Core.Palette;

/// <summary>One argument of a predicate template in a <see cref="RulePalette"/>.</summary>
/// <param name="Name">The argument name, as it appears in rule text.</param>
/// <param name="Kind">The literal kind of the value.</param>
/// <param name="Required">Whether the call must give the argument.</param>
/// <param name="Default">The value to start the form with, as JSON, or <see langword="null"/>.</param>
/// <param name="Description">What the argument means, or an empty string.</param>
public sealed record PaletteArgument(string Name, LiteralKind Kind, bool Required, string? Default, string Description);
