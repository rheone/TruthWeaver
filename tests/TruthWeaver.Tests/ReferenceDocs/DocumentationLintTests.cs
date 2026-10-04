namespace TruthWeaver.Tests.ReferenceDocs;

using TruthWeaver.Tests.DocExamples;

/// <summary>
/// The in-scope Markdown files meet the documentation standard in <c>CLAUDE.md</c>. <see cref="DocumentationLint"/> is
/// proven to fail on a deliberately wrong fixture for each rule.
/// </summary>
public sealed class DocumentationLintTests
{
    private const string ReferencePage = "docs/strong-k3/gates/and.md";

    /// <summary>Every in-scope Markdown file is clean or on the baseline, and no baseline entry is clean.</summary>
    [Fact]
    public void CheckTree_RealRepository_ReportsNoFailures_Test()
    {
        string root = DocExampleChecker.FindRepositoryRoot();

        IReadOnlyList<string> failures = DocumentationLint.CheckTree(root, DocumentationLintBaseline.Files);

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A page with plain present-tense prose and links inside the reference has no violations.</summary>
    [Fact]
    public void Check_CleanReferencePage_ReportsNoFailures_Test()
    {
        const string markdown = "# AND\n\nSee [OR](or.md) and the [values](../specification/values.md).\n";

        Assert.Empty(DocumentationLint.Check(ReferencePage, markdown));
    }

    /// <summary>An em dash is reported with its line.</summary>
    [Fact]
    public void Check_EmDash_ReportsFileAndLine_Test()
    {
        string failure = Assert.Single(DocumentationLint.Check("docs/guide.md", "# Guide\n\nOne — two.\n"));

        Assert.StartsWith("docs/guide.md:3:", failure, StringComparison.Ordinal);
    }

    /// <summary>Ticket, ADR and open-question wording is reported outside code spans.</summary>
    [Theory]
    [InlineData("This follows the ticket.")]
    [InlineData("This follows ADR-0005.")]
    [InlineData("This is an open question.")]
    public void Check_HistoryWording_ReportsFileAndLine_Test(string sentence)
    {
        string failure = Assert.Single(DocumentationLint.Check("docs/guide.md", "# Guide\n\n" + sentence + "\n"));

        Assert.StartsWith("docs/guide.md:3:", failure, StringComparison.Ordinal);
    }

    /// <summary>Wording inside a code span or a fenced block is not prose and is not reported.</summary>
    [Fact]
    public void Check_HistoryWordingInCode_ReportsNothing_Test()
    {
        const string markdown = "# Guide\n\nThe `ticket` field.\n\n```text\nADR-0005 — ticket\n```\n";

        Assert.Empty(DocumentationLint.Check("docs/guide.md", markdown));
    }

    /// <summary>The agent files keep their working vocabulary, and the em dash rule still applies to them.</summary>
    [Fact]
    public void Check_AgentFile_SkipsHistoryWordsButNotEmDashes_Test()
    {
        const string markdown = "# Rules\n\nSee the ticket and ADR-0003.\n\nA — dash.\n";

        string failure = Assert.Single(DocumentationLint.Check("CLAUDE.md", markdown));

        Assert.StartsWith("CLAUDE.md:5:", failure, StringComparison.Ordinal);
    }

    /// <summary>A link into the stop list is reported outside the root navigation files and allowed inside them.</summary>
    [Fact]
    public void Check_LinkToHistoryDocument_IsReportedOnlyOutsideNavigationFiles_Test()
    {
        const string markdown = "# Guide\n\nSee [the decision](adr/0001-kleene-failure-model.md).\n";

        string failure = Assert.Single(DocumentationLint.Check("docs/guide.md", markdown));

        Assert.StartsWith("docs/guide.md:3:", failure, StringComparison.Ordinal);
        Assert.Empty(
            DocumentationLint.Check("CONTEXT.md", "# C\n\nSee [the decision](docs/adr/0001-kleene-failure-model.md).\n")
        );
    }

    /// <summary>A reference page that links outside docs/strong-k3 is reported, including a link to the root README.</summary>
    [Theory]
    [InlineData("../../../README.md")]
    [InlineData("../../doc-examples.md")]
    [InlineData("../../../CONTEXT.md")]
    public void Check_ReferencePageLinkingOutsideTheReference_ReportsFileAndLine_Test(string target)
    {
        string failure = Assert.Single(DocumentationLint.Check(ReferencePage, $"# AND\n\nSee [home]({target}).\n"));

        Assert.StartsWith(ReferencePage + ":3:", failure, StringComparison.Ordinal);
    }

    /// <summary>The in-scope set follows links recursively, stops at the stop list and honors the opt markers.</summary>
    [Fact]
    public void InScope_FollowsLinksStopsAtStopListAndHonorsMarkers_Test()
    {
        string root = Path.Combine(Path.GetTempPath(), "tw-lint-" + Guid.NewGuid().ToString("N"));
        try
        {
            Write(root, "README.md", "[guide](docs/guide.md) [adr](docs/adr/0001-x.md)");
            Write(root, "CHANGELOG.md", "history");
            Write(root, "docs/guide.md", "[deep](deep.md)");
            Write(root, "docs/deep.md", "end");
            Write(root, "docs/adr/0001-x.md", "[linked](../hidden.md)");
            Write(root, "docs/hidden.md", "only the ADR links here");
            Write(root, "docs/unlinked.md", "nothing links here");
            Write(root, "docs/optin.md", "<!-- docs-lint: on -->");
            Write(root, "docs/optout.md", "<!-- docs-lint: off -->");
            Write(root, "docs/sub/README.md", "[optout](../optout.md)");
            Write(root, ".agents/skills/x/README.md", "third party");
            Write(root, "src/bin/Debug/README.md", "build output");

            IReadOnlyList<string> found = DocumentationLint.InScope(root);

            Assert.Equal(["README.md", "docs/deep.md", "docs/guide.md", "docs/optin.md", "docs/sub/README.md"], found);
        }
        finally
        {
            System.IO.Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>A baseline file is skipped while it breaks a rule, and reported once it is clean.</summary>
    [Fact]
    public void CheckTree_BaselineFile_IsSkippedUntilCleanThenReported_Test()
    {
        string root = Path.Combine(Path.GetTempPath(), "tw-lint-" + Guid.NewGuid().ToString("N"));
        try
        {
            Write(root, "README.md", "# R\n\nOne — two.\n");
            HashSet<string> baseline = ["README.md", "docs/gone.md"];

            Assert.Equal(
                ["docs/gone.md: the baseline entry is not an in-scope file, so remove it from DocumentationLintBaseline"],
                DocumentationLint.CheckTree(root, baseline)
            );

            Write(root, "README.md", "# R\n\nOne, two.\n");

            Assert.Contains(
                "README.md: the file meets the standard, so remove it from DocumentationLintBaseline",
                DocumentationLint.CheckTree(root, baseline)
            );
        }
        finally
        {
            System.IO.Directory.Delete(root, recursive: true);
        }
    }

    private static void Write(string root, string relative, string content)
    {
        string full = Path.Combine(root, relative);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }
}
