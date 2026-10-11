namespace TruthWeaver.Tool.Core;

using YamlDotNet.Core;

/// <summary>
/// Where one value of a JSON or YAML document is written: its character span in the document text and how it is written.
/// </summary>
/// <param name="Start">The offset of the first character of the value.</param>
/// <param name="End">
/// The offset just past the value. For a YAML block mapping or sequence, this is the end of its last value, so blank lines
/// and comments after the value are outside the span.
/// </param>
/// <param name="Kind">The shape of the value.</param>
/// <param name="IsFlow"><see langword="true"/> for a JSON value or a YAML flow (<c>{}</c> or <c>[]</c>) container.</param>
/// <param name="Style">The YAML scalar style; <see cref="ScalarStyle.Any"/> for JSON and for containers.</param>
/// <param name="IsShared">
/// <see langword="true"/> when the value cannot change by itself: it is a YAML alias, it has an anchor that an alias can
/// name, it is inside an anchored container, or it has an explicit tag.
/// </param>
internal sealed record SourceNode(int Start, int End, SourceNodeKind Kind, bool IsFlow, ScalarStyle Style, bool IsShared);
