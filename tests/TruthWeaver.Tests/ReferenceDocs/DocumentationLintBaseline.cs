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
        "benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md",
        "docs/data-sources.md",
        "docs/strong-k3/cardinality/all.md",
        "docs/strong-k3/cardinality/any.md",
        "docs/strong-k3/cardinality/atleast.md",
        "docs/strong-k3/cardinality/atmost.md",
        "docs/strong-k3/cardinality/between.md",
        "docs/strong-k3/cardinality/exactly.md",
        "docs/strong-k3/cardinality/exactlyone.md",
        "docs/strong-k3/cardinality/greaterthan.md",
        "docs/strong-k3/cardinality/lessthan.md",
        "docs/strong-k3/cardinality/none.md",
        "docs/strong-k3/derived/equivalent.md",
        "docs/strong-k3/derived/implies.md",
        "docs/strong-k3/derived/nand.md",
        "docs/strong-k3/derived/nor.md",
        "docs/strong-k3/derived/parity.md",
        "docs/strong-k3/derived/xor.md",
        "docs/strong-k3/functions/coalesce.md",
        "docs/strong-k3/functions/if.md",
        "docs/strong-k3/functions/isfalse.md",
        "docs/strong-k3/functions/isknown.md",
        "docs/strong-k3/functions/istrue.md",
        "docs/strong-k3/functions/isunknown.md",
        "docs/strong-k3/gates/and.md",
        "docs/strong-k3/gates/not.md",
        "docs/strong-k3/gates/or.md",
        "docs/strong-k3/result-transformations/collapse.md",
        "docs/strong-k3/result-transformations/project.md",
    };
}
