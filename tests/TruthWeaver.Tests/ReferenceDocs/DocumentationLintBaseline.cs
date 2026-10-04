namespace TruthWeaver.Tests.ReferenceDocs;

/// <summary>
/// In-scope Markdown files that do not yet meet the documentation standard. <see cref="DocumentationLint"/> skips them and
/// fails once one is clean, so each cleanup removes its files from this list and the list ends empty.
/// </summary>
internal static class DocumentationLintBaseline
{
    internal static readonly IReadOnlySet<string> Files = new HashSet<string>(StringComparer.Ordinal)
    {
        "AGENTS.md",
        "CONTEXT.md",
        "README.md",
        "docs/data-sources.md",
    };
}
