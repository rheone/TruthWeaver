namespace TruthWeaver.Parsing;

/// <summary>One entry of a mapping node: its name and value, or, when the format allows keys that are not strings, the offending key.</summary>
/// <param name="Name">The entry's name, or <see langword="null"/> when the key is not a string.</param>
/// <param name="Value">The entry's value.</param>
/// <param name="Key">
/// The key node, when the format tracks one: always <see langword="null"/> when <paramref name="Name"/> is not
/// <see langword="null"/> (a format with no key positions of its own, such as JSON), the offending key when
/// <paramref name="Name"/> is <see langword="null"/> (so the reader can locate and describe it), or — for a format that
/// tracks key positions, such as YAML — the real key node even when <paramref name="Name"/> is not <see langword="null"/>, so a
/// construct keyed by name (a document's <c>definitions</c> entry) can locate a diagnostic about the name itself.
/// </param>
internal readonly record struct TreeMember(string? Name, ITreeNodeCursor Value, ITreeNodeCursor? Key = null);
