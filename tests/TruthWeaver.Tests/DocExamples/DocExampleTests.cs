namespace TruthWeaver.Tests.DocExamples;

/// <summary>
/// The runnable examples in README.md, CONTEXT.md and docs/data-sources.md are checked on every build by <see cref="DocExampleChecker"/>, so a
/// change that breaks a documented example, or changes documented output, fails the build. The checker itself is proven to
/// fail on a deliberately broken example. The procedure for adding an example is in docs/doc-examples.md.
/// </summary>
public sealed class DocExampleTests
{
    /// <summary>
    /// Every tagged example in a documentation file compiles, and every documented output matches what the library
    /// produces now; no checked block is left untagged.
    /// </summary>
    [Theory]
    [InlineData("README.md")]
    [InlineData("CONTEXT.md")]
    [InlineData("docs/data-sources.md")]
    public void Check_DocumentationFile_ReportsNoFailures_Test(string fileName)
    {
        string markdown = File.ReadAllText(Path.Combine(DocExampleChecker.FindRepositoryRoot(), fileName));

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, fileName);

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A variable example that names a source the checker does not declare is reported, so the data-source examples are really compiled.</summary>
    [Fact]
    public void Check_VariableExampleWithUndeclaredSource_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:rule v -->\n```text\nageAtLeast(min: from(\"nobody\", \"$.minAge\"))\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A tagged rule example whose text no longer compiles is reported, naming the example.</summary>
    [Fact]
    public void Check_RuleExampleThatDoesNotCompile_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:rule broken -->\n```text\nlovesPineapple ANDD isBanned\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        string failure = Assert.Single(failures);
        Assert.Contains("broken", failure, StringComparison.Ordinal);
    }

    /// <summary>A documented tree rendering that no longer matches the rule's real rendering is reported.</summary>
    [Fact]
    public void Check_TreeOutputThatIsStale_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n"
            + "<!-- doctest:tree r -->\n```text\nXOR\n├─ Is Dine In\n└─ Is Takeaway\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>The same rule written as JSON that differs from the DSL rule is reported.</summary>
    [Fact]
    public void Check_JsonExampleThatDiffersFromTheRuleText_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn OR isTakeout\n```\n"
            + "<!-- doctest:json r -->\n```json\n{ \"op\": \"and\", \"operands\": [{ \"predicate\": \"isDineIn\" }, { \"predicate\": \"isTakeout\" }] }\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>Documented diagnostics output that no longer matches the formatter's output is reported.</summary>
    [Fact]
    public void Check_DiagnosticsOutputThatIsStale_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:diagnostics-dsl a ANDD b -->\n```text\nTRE9999 something else\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A block in a checked language that carries no marker is reported, so new examples cannot escape the check.</summary>
    [Fact]
    public void Check_UntaggedTextBlock_ReportsAFailure_Test()
    {
        const string markdown = "```text\nlovesPineapple\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A block explicitly allow-listed with a reason is not checked and produces no failure.</summary>
    [Fact]
    public void Check_SkippedBlock_ReportsNoFailure_Test()
    {
        const string markdown = "<!-- doctest:skip pseudo-grammar, not a rule -->\n```text\nExpression = Term\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Empty(failures);
    }

    /// <summary>A skip marker without a reason is reported, so every allow-listed block explains itself.</summary>
    [Fact]
    public void Check_SkipWithoutAReason_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:skip -->\n```text\nExpression = Term\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A marker that names an unknown kind, or an example id nothing defined, is reported.</summary>
    [Theory]
    [InlineData("<!-- doctest:bogus x -->")]
    [InlineData("<!-- doctest:tree nothing-defined-this -->")]
    public void Check_MarkerThatCannotBeResolved_ReportsAFailure_Test(string marker)
    {
        string markdown = marker + "\n```text\nanything\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>Languages that are not runnable here (C# fragments, shell, diagrams outside a marker) are left alone.</summary>
    [Fact]
    public void Check_CSharpBlock_IsNotChecked_Test()
    {
        const string markdown = "```csharp\nvar x = notRunnable;\n```\n";

        IReadOnlyList<string> failures = DocExampleChecker.Check(markdown, "sample.md");

        Assert.Empty(failures);
    }
}
