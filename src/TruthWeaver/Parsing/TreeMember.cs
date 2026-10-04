namespace TruthWeaver.Parsing;

/// <summary>One entry of a mapping node: its name and value, or, when the format allows keys that are not strings, the offending key.</summary>
/// <param name="Name">The entry's name, or <see langword="null"/> when the key is not a string.</param>
/// <param name="Value">The entry's value.</param>
/// <param name="Key">The key node when <paramref name="Name"/> is <see langword="null"/>, so the reader can locate and describe it; otherwise <see langword="null"/>.</param>
internal readonly record struct TreeMember(string? Name, ITreeNodeCursor Value, ITreeNodeCursor? Key = null);
