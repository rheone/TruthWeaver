namespace TruthWeaver.Tool.Tui.Sessions;

using System.Text.RegularExpressions;

/// <summary>A part of the language that the samples must show, found in a sample by a pattern on its text without the notes.</summary>
/// <param name="Name">The name of the part, such as <c>XOR</c>.</param>
/// <param name="Pattern">The pattern that finds the part in rule text.</param>
internal sealed record SampleFeature(string Name, Regex Pattern);
