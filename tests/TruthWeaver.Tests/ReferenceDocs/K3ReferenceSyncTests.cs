namespace TruthWeaver.Tests.ReferenceDocs;

using TruthWeaver.Tests.DocExamples;

/// <summary>
/// The Strong Kleene (K3) reference must name exactly the operators and predicates the engine has.
/// <see cref="K3ReferenceSyncChecker"/> compares the engine's operator table, the <c>Decision</c> result transformations and
/// the predicate factories of the predicates package with the documents under docs/strong-k3. The checker is proven to fail
/// on fixtures; the real tree is checked against the real engine.
/// </summary>
public sealed class K3ReferenceSyncTests
{
    private static readonly string[] Operators = ["not", "and", "atleast"];

    private static readonly string[] Transformations = ["project", "collapse"];

    private static readonly string[] Predicates = ["string-equals", "regex-matches"];

    private static readonly string[] CompleteDocuments =
    [
        "docs/strong-k3/gates/not.md",
        "docs/strong-k3/gates/and.md",
        "docs/strong-k3/gates/README.md",
        "docs/strong-k3/cardinality/atleast.md",
        "docs/strong-k3/result-transformations/project.md",
        "docs/strong-k3/result-transformations/collapse.md",
        "docs/strong-k3/predicates/string-equals.md",
        "docs/strong-k3/predicates/README.md",
    ];

    /// <summary>The real reference matches the real engine, with the allow-list as the only gap.</summary>
    [Fact]
    public void CheckTree_RealReference_ReportsNoFailures_Test()
    {
        IReadOnlyList<string> failures = K3ReferenceSyncChecker.CheckTree(DocExampleChecker.FindRepositoryRoot());

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A reference that covers every engine item and holds the undocumented predicate passes.</summary>
    [Fact]
    public void Check_CompleteFixture_ReportsNoFailures_Test()
    {
        Assert.Empty(Check(CompleteDocuments));
    }

    /// <summary>An engine operator with no document is reported by name.</summary>
    [Fact]
    public void Check_OperatorWithoutDocument_ReportsTheOperator_Test()
    {
        string failure = Assert.Single(Check(CompleteDocuments.Where(d => !d.EndsWith("/and.md", StringComparison.Ordinal))));

        Assert.Contains("operator 'and'", failure, StringComparison.Ordinal);
    }

    /// <summary>A document for an operator the engine lacks is reported with its path.</summary>
    [Fact]
    public void Check_DocumentForUnknownOperator_ReportsTheDocument_Test()
    {
        string failure = Assert.Single(Check([.. CompleteDocuments, "docs/strong-k3/derived/nxor.md"]));

        Assert.StartsWith("docs/strong-k3/derived/nxor.md", failure, StringComparison.Ordinal);
    }

    /// <summary>A result-transformation document that names no <c>Decision</c> method is reported.</summary>
    [Fact]
    public void Check_DocumentForUnknownTransformation_ReportsTheDocument_Test()
    {
        string failure = Assert.Single(Check([.. CompleteDocuments, "docs/strong-k3/result-transformations/flatten.md"]));

        Assert.StartsWith("docs/strong-k3/result-transformations/flatten.md", failure, StringComparison.Ordinal);
    }

    /// <summary>A predicate with no document and no hold entry is reported and the message points at the hold list.</summary>
    [Fact]
    public void Check_PredicateWithoutDocumentOrHold_ReportsThePredicateAndTheHoldList_Test()
    {
        string failure = Assert.Single(Check(CompleteDocuments, hold: []));

        Assert.Contains("'regex-matches'", failure, StringComparison.Ordinal);
        Assert.Contains(nameof(K3PredicateDocumentationHold), failure, StringComparison.Ordinal);
    }

    /// <summary>A hold entry for a predicate that now has a document is stale and reported.</summary>
    [Fact]
    public void Check_HeldPredicateWithDocument_ReportsTheStaleHold_Test()
    {
        string failure = Assert.Single(Check(CompleteDocuments, hold: ["regex-matches", "string-equals"]));

        Assert.Contains("'string-equals'", failure, StringComparison.Ordinal);
    }

    /// <summary>A hold entry for a predicate the engine lacks is reported.</summary>
    [Fact]
    public void Check_HoldEntryForUnknownPredicate_ReportsTheEntry_Test()
    {
        string failure = Assert.Single(Check(CompleteDocuments, hold: ["regex-matches", "string-nothing"]));

        Assert.Contains("'string-nothing'", failure, StringComparison.Ordinal);
    }

    /// <summary>A predicate document for a predicate the engine lacks is reported with its path.</summary>
    [Fact]
    public void Check_DocumentForUnknownPredicate_ReportsTheDocument_Test()
    {
        string failure = Assert.Single(Check([.. CompleteDocuments, "docs/strong-k3/predicates/string-nothing.md"]));

        Assert.StartsWith("docs/strong-k3/predicates/string-nothing.md", failure, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> Check(
        IEnumerable<string> documents,
        string[]? predicates = null,
        string[]? hold = null
    )
    {
        return K3ReferenceSyncChecker.Check(
            Operators,
            Transformations,
            predicates ?? Predicates,
            hold ?? ["regex-matches"],
            [.. documents]
        );
    }
}
