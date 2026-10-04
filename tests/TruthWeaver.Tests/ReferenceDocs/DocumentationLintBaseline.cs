namespace TruthWeaver.Tests.ReferenceDocs;

/// <summary>
/// In-scope Markdown files exempt from <see cref="DocumentationLint"/> because they do not yet meet the documentation
/// standard. The list is empty now and may only shrink: add a file only with a written reason, and remove its entry as soon
/// as the file is clean (the lint fails on a clean entry).
/// </summary>
internal static class DocumentationLintBaseline
{
    // Deliberately empty: Roslynator raises RCS1259 on an empty collection initializer, so none is written.
    internal static readonly IReadOnlySet<string> Files = new HashSet<string>(StringComparer.Ordinal);
}
