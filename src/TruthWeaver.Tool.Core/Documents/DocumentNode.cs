namespace TruthWeaver.Tool.Core.Documents;

using TruthWeaver.Tool.Core.Loading;

/// <summary>One value of a JSON or YAML document that <see cref="DocumentOutline"/> lists: a place where a rule can be.</summary>
/// <param name="Path">The normalized path of the value, such as <c>$.rules.publish</c>. It is the input of <see cref="RuleLoadOptions.Path"/>.</param>
/// <param name="Depth">The number of segments of the path; the document root has 0.</param>
/// <param name="Kind">What the value is.</param>
/// <param name="Label">A short description of the value for a list: its first keys, its size, or the start of its text.</param>
public sealed record DocumentNode(string Path, int Depth, DocumentNodeKind Kind, string Label)
{
    /// <summary>Gets the segments of the path, from the document root down.</summary>
    internal IReadOnlyList<DocumentPath.Segment> Segments { get; init; } = [];
}
