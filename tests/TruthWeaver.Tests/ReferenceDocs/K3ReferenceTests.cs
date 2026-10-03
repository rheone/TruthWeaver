namespace TruthWeaver.Tests.ReferenceDocs;

using System.Globalization;
using TruthWeaver.Tests.DocExamples;

/// <summary>
/// The Strong Kleene (K3) reference under docs/strong-k3 is verified on every build by <see cref="K3ReferenceChecker"/>:
/// tables and canonical forms are compared with the independent <c>K3Oracle</c>, every operation document is checked for its
/// required structure, and relative links must resolve. The checker is proven to fail on deliberately wrong fixtures. The
/// marker conventions are in docs/doc-examples.md and the checker's remarks.
/// </summary>
public sealed class K3ReferenceTests
{
    private const string NotPath = "docs/strong-k3/gates/not.md";

    private const string NotesPath = "docs/strong-k3/notes.md";

    // Normalised to LF so Replace calls below behave the same whatever line endings the source file has.
    private static readonly string ValidNot = """
        # NOT

        ## Name

        `NOT`

        ## Classification

        - Category: Gates / Operators
        - Strong Kleene connective

        ## Kind

        Primitive

        ## Arity

        One operand.

        ## Input Domain

        `{T, F, U}`

        ## Output Domain

        `{T, F, U}`

        ## Definition

        Swaps True and False and leaves Unknown.

        ## Syntax

        `NOT a`

        ## Aliases

        `!`

        ## Formal Semantics

        Reverses the truth order.

        ## Truth Table

        <!-- k3:truth NOT -->
        | a | NOT a |
        | --- | --- |
        | T | F |
        | U | U |
        | F | T |
        """.ReplaceLineEndings("\n");

    /// <summary>Every Markdown file under docs/strong-k3 passes the reference checks (all tables, forms and links).</summary>
    [Fact]
    public void CheckTree_RealReference_ReportsNoFailures_Test()
    {
        string root = DocExampleChecker.FindRepositoryRoot();

        IReadOnlyList<string> failures = K3ReferenceChecker.CheckTree(root);

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A complete, correct operation document produces no failures.</summary>
    [Fact]
    public void Check_ValidOperationDocument_ReportsNoFailures_Test()
    {
        IReadOnlyList<string> failures = Check(NotPath, ValidNot);

        Assert.Empty(failures);
    }

    /// <summary>A truth-table cell that disagrees with the oracle is reported with its file and line.</summary>
    [Fact]
    public void Check_TruthTableWithWrongCell_ReportsFileAndLine_Test()
    {
        string wrong = ValidNot.Replace("| U | U |", "| U | T |", StringComparison.Ordinal);

        string failure = Assert.Single(Check(NotPath, wrong));

        Assert.StartsWith(NotPath + ":" + LineOf(wrong, "| U | T |") + ":", failure, StringComparison.Ordinal);
    }

    /// <summary>A truth table that omits an input combination is reported.</summary>
    [Fact]
    public void Check_TruthTableMissingARow_ReportsTheMissingAssignment_Test()
    {
        string incomplete = ValidNot.Replace("| U | U |\n", string.Empty, StringComparison.Ordinal);

        string failure = Assert.Single(Check(NotPath, incomplete));

        Assert.Contains("missing", failure, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>An evaluation table is checked against the oracle over the definitely-true and possibly-true counts.</summary>
    [Fact]
    public void Check_EvaluationTableWithWrongResult_ReportsFileAndLine_Test()
    {
        const string markdown = """
            <!-- k3:eval ATLEAST k=2 n=3 -->
            | Definitely true | Possibly true | Result |
            | --- | --- | --- |
            | 0 | 0 | F |
            | 0 | 1 | F |
            | 0 | 2 | U |
            | 0 | 3 | U |
            | 1 | 1 | F |
            | 1 | 2 | U |
            | 1 | 3 | U |
            | 2 | 2 | T |
            | 2 | 3 | T |
            | 3 | 3 | F |
            """;

        string failure = Assert.Single(Check(NotesPath, markdown));

        Assert.StartsWith(NotesPath + ":13:", failure, StringComparison.Ordinal);
    }

    /// <summary>A canonical form that does not compute the operation is reported at the marker's line.</summary>
    [Fact]
    public void Check_CanonicalFormThatIsWrong_ReportsFileAndLine_Test()
    {
        const string markdown = "<!-- k3:canonical IMPLIES vars=a,b -->\n```text\nOR(a, NOT(b))\n```\n";

        string failure = Assert.Single(Check(NotesPath, markdown));

        Assert.StartsWith(NotesPath + ":1:", failure, StringComparison.Ordinal);
    }

    /// <summary>A form that holds for two operands but not for three is caught by the sweep over operand counts.</summary>
    [Fact]
    public void Check_ParityFormThatOnlyHoldsForTwoOperands_ReportsAFailure_Test()
    {
        const string markdown = "<!-- k3:canonical PARITY n=2..4 -->\n```text\nXOR(a, b)\n```\n";

        string failure = Assert.Single(Check(NotesPath, markdown));

        Assert.Contains("PARITY", failure, StringComparison.Ordinal);
    }

    /// <summary>A correct variadic, parameterised canonical form passes for every operand count and parameter.</summary>
    [Fact]
    public void Check_VariadicParameterisedCanonicalForm_ReportsNoFailures_Test()
    {
        const string markdown = "<!-- k3:canonical GREATERTHAN n=1..4 -->\n```text\nATLEAST(k + 1, ...)\n```\n";

        Assert.Empty(Check(NotesPath, markdown));
    }

    /// <summary>A relative link to a file that does not exist is reported with file and line.</summary>
    [Fact]
    public void Check_LinkToMissingFile_ReportsFileAndLine_Test()
    {
        const string markdown = "# Title\n\nSee [values](../specification/values.md).\n";

        string failure = Assert.Single(Check("docs/strong-k3/gates/README.md", markdown));

        Assert.StartsWith("docs/strong-k3/gates/README.md:3:", failure, StringComparison.Ordinal);
        Assert.Contains("docs/strong-k3/specification/values.md", failure, StringComparison.Ordinal);
    }

    /// <summary>A link to a heading anchor that does not exist in the target is reported.</summary>
    [Fact]
    public void Check_LinkToMissingAnchor_ReportsFileAndLine_Test()
    {
        const string markdown = "# Title\n\n## Real heading\n\n[ok](#real-heading) [bad](#no-such-heading)\n";

        string failure = Assert.Single(Check("docs/strong-k3/README.md", markdown));

        Assert.StartsWith("docs/strong-k3/README.md:5:", failure, StringComparison.Ordinal);
        Assert.Contains("no-such-heading", failure, StringComparison.Ordinal);
    }

    /// <summary>Links inside code spans and fenced blocks are examples, not links, and are not checked.</summary>
    [Fact]
    public void Check_LinkInsideCode_IsIgnored_Test()
    {
        const string markdown = "Use `[a](missing.md)`.\n\n```text\n[b](missing.md)\n```\n";

        Assert.Empty(Check("docs/strong-k3/README.md", markdown));
    }

    /// <summary>An operation document without a Kind section is reported.</summary>
    [Fact]
    public void Check_OperationDocumentWithoutKind_ReportsMissingSection_Test()
    {
        string noKind = ValidNot.Replace("## Kind\n\nPrimitive\n\n", string.Empty, StringComparison.Ordinal);

        string failure = Assert.Single(Check(NotPath, noKind));

        Assert.Contains("Kind", failure, StringComparison.Ordinal);
    }

    /// <summary>A Kind that contradicts the approved inventory is reported.</summary>
    [Fact]
    public void Check_OperationDocumentWithWrongKind_IsReported_Test()
    {
        string wrongKind = ValidNot.Replace("## Kind\n\nPrimitive", "## Kind\n\nDerived", StringComparison.Ordinal);

        string failure = Assert.Single(Check(NotPath, wrongKind));

        Assert.Contains("Kind", failure, StringComparison.Ordinal);
    }

    /// <summary>A category that does not match the directory the document lives in is reported.</summary>
    [Fact]
    public void Check_OperationDocumentInWrongCategory_IsReported_Test()
    {
        string wrongCategory = ValidNot.Replace("Gates / Operators", "Functions", StringComparison.Ordinal);

        string failure = Assert.Single(Check(NotPath, wrongCategory));

        Assert.Contains("Category", failure, StringComparison.Ordinal);
    }

    /// <summary>A document whose name is not in the approved inventory is reported.</summary>
    [Fact]
    public void Check_OperationDocumentNotInInventory_IsReported_Test()
    {
        string failure = Assert.Single(Check("docs/strong-k3/gates/mystery.md", ValidNot));

        Assert.Contains("inventory", failure, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A Truth Table section with no verified table marker cannot slip through unchecked.</summary>
    [Fact]
    public void Check_TruthTableSectionWithoutMarker_IsReported_Test()
    {
        string noMarker = ValidNot.Replace("<!-- k3:truth NOT -->\n", string.Empty, StringComparison.Ordinal);

        string failure = Assert.Single(Check(NotPath, noMarker));

        Assert.Contains("k3:truth", failure, StringComparison.Ordinal);
    }

    /// <summary>Checks <paramref name="markdown"/> as if it lived at <paramref name="path"/> in a tree holding only itself.</summary>
    private static IReadOnlyList<string> Check(string path, string markdown)
    {
        return K3ReferenceChecker.Check(
            path,
            markdown,
            exists: candidate => candidate == path,
            read: candidate => candidate == path ? markdown : null
        );
    }

    private static string LineOf(string text, string fragment)
    {
        string[] lines = text.Split('\n');
        int index = Array.FindIndex(lines, line => line.Contains(fragment, StringComparison.Ordinal));
        return (index + 1).ToString(CultureInfo.InvariantCulture);
    }
}
