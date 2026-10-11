namespace TruthWeaver.Tests.DocExamples;

/// <summary>
/// The runnable examples in README.md, CONTEXT.md, docs/data-sources.md, docs/architecture.md, docs/packages.md, docs/rule-text.md, docs/rule-formats.md, docs/html-and-dot.md, docs/rulebuilder.md, docs/rewriting-rules.md, docs/predicates.md, docs/predicate-conventions.md, docs/examples.md, docs/diagnostics.md and docs/benchmarks.md are checked on every build by <see cref="DocExampleChecker"/>, so a
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
    [InlineData("docs/architecture.md")]
    [InlineData("docs/packages.md")]
    [InlineData("docs/rule-text.md")]
    [InlineData("docs/rule-formats.md")]
    [InlineData("docs/html-and-dot.md")]
    [InlineData("docs/rulebuilder.md")]
    [InlineData("docs/rewriting-rules.md")]
    [InlineData("docs/predicates.md")]
    [InlineData("docs/predicate-conventions.md")]
    [InlineData("docs/examples.md")]
    [InlineData("docs/diagnostics.md")]
    [InlineData("docs/benchmarks.md")]
    public async Task CheckAsync_DocumentationFile_ReportsNoFailures_Test(string fileName)
    {
        string markdown = await File.ReadAllTextAsync(
            Path.Combine(DocExampleChecker.FindRepositoryRoot(), fileName),
            TestContext.Current.CancellationToken
        );

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, fileName);

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A variable example that names a source the checker does not declare is reported, so the data-source examples are really compiled.</summary>
    [Fact]
    public async Task CheckAsync_VariableExampleWithUndeclaredSource_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:rule v -->\n```text\nageAtLeast(min: from(\"nobody\", \"$.minAge\"))\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A tagged rule example whose text no longer compiles is reported, naming the example.</summary>
    [Fact]
    public async Task CheckAsync_RuleExampleThatDoesNotCompile_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:rule broken -->\n```text\nlovesPineapple ANDD isBanned\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        string failure = Assert.Single(failures);
        Assert.Contains("broken", failure, StringComparison.Ordinal);
    }

    /// <summary>A documented tree rendering that no longer matches the rule's real rendering is reported.</summary>
    [Fact]
    public async Task CheckAsync_TreeOutputThatIsStale_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n"
            + "<!-- doctest:tree r -->\n```text\nXOR\n├─ Is Dine In\n└─ Is Takeaway\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>The same rule written as JSON that differs from the DSL rule is reported.</summary>
    [Fact]
    public async Task CheckAsync_JsonExampleThatDiffersFromTheRuleText_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn OR isTakeout\n```\n"
            + "<!-- doctest:json r -->\n```json\n{ \"op\": \"and\", \"operands\": [{ \"predicate\": \"isDineIn\" }, { \"predicate\": \"isTakeout\" }] }\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A documented equation or legend that no longer matches the rule's real printing is reported.</summary>
    [Theory]
    [InlineData("equation r")]
    [InlineData("legend r")]
    [InlineData("equation r latex double-dollar")]
    public async Task CheckAsync_EquationOutputThatIsStale_ReportsAFailure_Test(string marker)
    {
        string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n"
            + $"<!-- doctest:{marker} -->\n```text\nisDineIn AND isTakeout\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>Documented equation output that matches the real printing passes, for the default and the LaTeX dialects.</summary>
    [Fact]
    public async Task CheckAsync_EquationOutputThatIsCurrent_ReportsNoFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n"
            + "<!-- doctest:equation r -->\n```text\nisDineIn ⊕ isTakeout\n```\n"
            + "<!-- doctest:equation r latex -->\n```text\n\\text{isDineIn} \\oplus \\text{isTakeout}\n```\n"
            + "<!-- doctest:legend r -->\n```text\np ⊕ q\np = isDineIn\nq = isTakeout\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Empty(failures);
    }

    /// <summary>The words <c>mathml</c>, <c>typst</c> and <c>plain-ascii</c> select the new equation dialects, so a block with that dialect's text passes and a stale one fails.</summary>
    [Fact]
    public async Task CheckAsync_NewEquationDialectWords_SelectTheDialectOfTheOutput_Test()
    {
        const string rule = "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n";

        IReadOnlyList<string> current = await DocExampleChecker.CheckAsync(
            rule
                + "<!-- doctest:equation r mathml -->\n```text\n<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtext>isDineIn</mtext> <mo>⊕</mo> <mtext>isTakeout</mtext></math>\n```\n"
                + "<!-- doctest:equation r typst -->\n```text\n$\"isDineIn\" xor \"isTakeout\"$\n```\n"
                + "<!-- doctest:equation r plain-ascii -->\n```text\nisDineIn (+) isTakeout\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> stale = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:equation r plain-ascii -->\n```text\nisDineIn ⊕ isTakeout\n```\n",
            "sample.md"
        );

        Assert.Empty(current);
        Assert.Single(stale);
    }

    /// <summary>A rewrite word makes the block document the rewritten rule, so rule text that matches the real CNF passes.</summary>
    [Fact]
    public async Task CheckAsync_RuleTextOfARewrittenRuleThatIsCurrent_ReportsNoFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nNOT (approvedByAlice AND (approvedByBob OR approvedByCarol))\n```\n"
            + "<!-- doctest:ruletext r cnf -->\n```text\n(NOT approvedByAlice OR NOT approvedByBob) AND (NOT approvedByAlice OR NOT approvedByCarol)\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Empty(failures);
    }

    /// <summary>Rule text that is the unrewritten rule, where the marker asks for a rewrite, is reported as stale.</summary>
    [Fact]
    public async Task CheckAsync_RuleTextThatIgnoresTheRewriteWord_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nNOT (approvedByAlice AND (approvedByBob OR approvedByCarol))\n```\n"
            + "<!-- doctest:ruletext r cnf -->\n```text\nNOT (approvedByAlice AND (approvedByBob OR approvedByCarol))\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>
    /// Ticket 10: a <c>width=N</c> word on a <c>ruletext</c> marker checks the width-aware pretty printer; documented text
    /// that does not match the real breaking is reported as stale.
    /// </summary>
    [Fact]
    public async Task CheckAsync_RuleTextWidthWord_ChecksTheWidthAwarePrinter_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\napprovedByAlice AND (approvedByBob OR NOT approvedByCarol)\n```\n"
            + "<!-- doctest:ruletext r width=20 -->\n```text\napprovedByAlice AND\n(\n  approvedByBob OR\n  NOT approvedByCarol\n)\n```\n";
        const string stale =
            "<!-- doctest:rule r -->\n```text\napprovedByAlice AND (approvedByBob OR NOT approvedByCarol)\n```\n"
            + "<!-- doctest:ruletext r width=20 -->\n```text\napprovedByAlice AND (approvedByBob OR NOT approvedByCarol)\n```\n";

        IReadOnlyList<string> current = await DocExampleChecker.CheckAsync(markdown, "sample.md");
        IReadOnlyList<string> staleResult = await DocExampleChecker.CheckAsync(stale, "sample.md");

        Assert.Empty(current);
        Assert.Single(staleResult);
    }

    /// <summary>A rewrite that is refused at its size cap is reported for a printing marker, and its diagnostics are checked by a rewrite-diagnostics marker.</summary>
    [Fact]
    public async Task CheckAsync_RewriteOverTheCap_ReportsAFailureForPrintingAndMatchesTheDiagnostics_Test()
    {
        const string rule =
            "<!-- doctest:rule r -->\n```text\n(isDineIn AND isTakeout) OR (isBanned AND isContractor) OR (hasSignedNda AND lovesPineapple)\n```\n";

        IReadOnlyList<string> printing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:ruletext r cnf cap=10 -->\n```text\nanything\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> diagnostics = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:rewrite-diagnostics r cnf cap=10 -->\n```text\nTRE9999 something else\n```\n",
            "sample.md"
        );

        Assert.Contains("did not produce a rule", Assert.Single(printing), StringComparison.Ordinal);
        Assert.Contains("documented output is stale", Assert.Single(diagnostics), StringComparison.Ordinal);
    }

    /// <summary>A metrics table must hold the real depth and node count of the rule and of each rewrite, so a wrong number is reported.</summary>
    [Fact]
    public async Task CheckAsync_MetricsTable_ReportsOnlyAWrongNumber_Test()
    {
        const string rule =
            "<!-- doctest:rule r -->\n```text\nNOT (approvedByAlice AND (approvedByBob OR approvedByCarol))\n```\n";
        const string current = "| Form | Depth | NodeCount |\n| --- | --- | --- |\n| Original | 4 | 6 |\n| CNF | 4 | 11 |\n";
        const string stale = "| Form | Depth | NodeCount |\n| --- | --- | --- |\n| Original | 4 | 6 |\n| CNF | 4 | 12 |\n";

        IReadOnlyList<string> passing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:metrics r cnf -->\n" + current,
            "sample.md"
        );
        IReadOnlyList<string> failing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:metrics r cnf -->\n" + stale,
            "sample.md"
        );

        Assert.Empty(passing);
        Assert.Single(failing);
    }

    /// <summary>Several rewrite words apply in order, so expanding to primitives and compressing back gives the derived operator.</summary>
    [Fact]
    public async Task CheckAsync_RuleTextAfterTwoRewrites_AppliesThemInOrder_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nNOT isContractor OR hasSignedNda\n```\n"
            + "<!-- doctest:ruletext r primitives compress -->\n```text\n(isContractor IMPLIES hasSignedNda)\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Empty(failures);
    }

    /// <summary>A display-math paragraph after an equation marker is checked against the printed LaTeX, so a stale formula is reported.</summary>
    [Fact]
    public async Task CheckAsync_DisplayMathParagraph_ReportsOnlyAStaleFormula_Test()
    {
        const string rule = "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n";
        const string current = "$$\\text{isDineIn} \\oplus \\text{isTakeout}$$\n";
        const string stale = "$$\\text{isDineIn} \\land \\text{isTakeout}$$\n";

        IReadOnlyList<string> passing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:equation r latex double-dollar -->\n" + current,
            "sample.md"
        );
        IReadOnlyList<string> failing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:equation r latex double-dollar -->\n" + stale,
            "sample.md"
        );

        Assert.Empty(passing);
        Assert.Single(failing);
    }

    /// <summary>The words <c>letters</c> and <c>logic-symbols</c> select the constant style, so a block with that style's text passes and a stale one fails.</summary>
    [Fact]
    public async Task CheckAsync_ConstantStyleWords_SelectTheConstantStyleOfTheOutput_Test()
    {
        const string rule = "<!-- doctest:rule r -->\n```text\nTrue AND Unknown\n```\n";

        IReadOnlyList<string> letters = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:equation r letters -->\n```text\nT ∧ U\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> symbols = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:tree r logic-symbols -->\n```text\nAND\n├─ ⊤\n└─ U\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> stale = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:equation r letters -->\n```text\nTrue ∧ Unknown\n```\n",
            "sample.md"
        );

        Assert.Empty(letters);
        Assert.Empty(symbols);
        Assert.Single(stale);
    }

    /// <summary>An html marker checks the block against <c>PrintHtml</c>, with the option words, so current output passes and stale output fails.</summary>
    [Fact]
    public async Task CheckAsync_HtmlMarker_ComparesTheBlockWithPrintHtml_Test()
    {
        const string rule = "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n";
        const string plain =
            "<span class=\"tw-rule\"><span class=\"tw-term\">Is Dine In</span> <span class=\"tw-operator\">XOR</span> <span class=\"tw-term\">Is Takeout</span></span>";
        const string symbolic =
            "<span class=\"tw-rule\"><span class=\"tw-term\">Is Dine In</span> <span class=\"tw-operator\">&oplus;</span> <span class=\"tw-term\">Is Takeout</span></span>";

        IReadOnlyList<string> passing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:html r -->\n```html\n" + plain + "\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> passingSymbolic = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:html r symbolic -->\n```html\n" + symbolic + "\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> stale = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:html r -->\n```html\n" + symbolic + "\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> untagged = await DocExampleChecker.CheckAsync("```html\n" + plain + "\n```\n", "sample.md");

        Assert.Empty(passing);
        Assert.Empty(passingSymbolic);
        Assert.Single(stale);
        Assert.Single(untagged);
    }

    /// <summary>A dot marker checks the block against <c>PrintDot</c>, with the option words, so current output passes and stale output fails.</summary>
    [Fact]
    public async Task CheckAsync_DotMarker_ComparesTheBlockWithPrintDot_Test()
    {
        const string rule = "<!-- doctest:rule r -->\n```text\nNOT isDineIn\n```\n";
        const string dot =
            "digraph TruthWeaver {\n    rankdir=TB;\n    n0 [label=\"NOT\", shape=hexagon];\n    n1 [label=\"Is Dine In\", shape=box, style=\"rounded\"];\n    n0 -> n1;\n}";
        string plain = dot.Replace("shape=hexagon", "shape=box").Replace("shape=box, style=\"rounded\"", "shape=box");

        IReadOnlyList<string> passing = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:dot r -->\n```dot\n" + dot + "\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> passingPlain = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:dot r plain -->\n```dot\n" + plain + "\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> stale = await DocExampleChecker.CheckAsync(
            rule + "<!-- doctest:dot r -->\n```dot\n" + plain + "\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> untagged = await DocExampleChecker.CheckAsync("```dot\n" + dot + "\n```\n", "sample.md");

        Assert.Empty(passing);
        Assert.Empty(passingPlain);
        Assert.Single(stale);
        Assert.Single(untagged);
    }

    /// <summary>The word <c>trace</c> on an html or dot marker prints the decision, so the uncolored output is stale.</summary>
    [Fact]
    public async Task CheckAsync_TraceWordOnHtmlAndDot_PrintsTheDecisionOutput_Test()
    {
        const string rule = "<!-- doctest:rule r -->\n```text\nisDineIn\n```\n";

        IReadOnlyList<string> html = await DocExampleChecker.CheckAsync(
            rule
                + "<!-- doctest:html r trace -->\n```html\n<span class=\"tw-rule\"><span class=\"tw-term tw-true\">Is Dine In</span></span>\n```\n",
            "sample.md"
        );
        IReadOnlyList<string> staleDot = await DocExampleChecker.CheckAsync(
            rule
                + "<!-- doctest:dot r trace -->\n```dot\ndigraph TruthWeaver {\n    rankdir=TB;\n    n0 [label=\"Is Dine In\", shape=box, style=\"rounded\"];\n}\n```\n",
            "sample.md"
        );

        Assert.Empty(html);
        Assert.Single(staleDot);
    }

    /// <summary>A documented operator-style tree that uses the word spelling where the marker asks for C-style is reported.</summary>
    [Fact]
    public async Task CheckAsync_TreeInTheWrongOperatorStyle_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n"
            + "<!-- doctest:tree r cstyle -->\n```text\nXOR\n├─ Is Dine In\n└─ Is Takeout\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A trace diagram colors the nodes of an evaluation, so the uncolored diagram is reported as stale for a trace marker.</summary>
    [Fact]
    public async Task CheckAsync_TraceDiagramWithoutColors_ReportsAFailure_Test()
    {
        const string markdown =
            "<!-- doctest:rule r -->\n```text\nisDineIn XOR isTakeout\n```\n"
            + "<!-- doctest:mermaid r trace -->\n```mermaid\nflowchart TD\n    Start([\"Start\"]) --> n0\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>Documented diagnostics output that no longer matches the formatter's output is reported.</summary>
    [Fact]
    public async Task CheckAsync_DiagnosticsOutputThatIsStale_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:diagnostics-dsl a ANDD b -->\n```text\nTRE9999 something else\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A block in a checked language that carries no marker is reported, so new examples cannot escape the check.</summary>
    [Fact]
    public async Task CheckAsync_UntaggedTextBlock_ReportsAFailure_Test()
    {
        const string markdown = "```text\nlovesPineapple\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A block explicitly allow-listed with a reason is not checked and produces no failure.</summary>
    [Fact]
    public async Task CheckAsync_SkippedBlock_ReportsNoFailure_Test()
    {
        const string markdown = "<!-- doctest:skip pseudo-grammar, not a rule -->\n```text\nExpression = Term\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Empty(failures);
    }

    /// <summary>A skip marker without a reason is reported, so every allow-listed block explains itself.</summary>
    [Fact]
    public async Task CheckAsync_SkipWithoutAReason_ReportsAFailure_Test()
    {
        const string markdown = "<!-- doctest:skip -->\n```text\nExpression = Term\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>A marker that names an unknown kind, or an example id nothing defined, is reported.</summary>
    [Theory]
    [InlineData("<!-- doctest:bogus x -->")]
    [InlineData("<!-- doctest:tree nothing-defined-this -->")]
    public async Task CheckAsync_MarkerThatCannotBeResolved_ReportsAFailure_Test(string marker)
    {
        string markdown = marker + "\n```text\nanything\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Single(failures);
    }

    /// <summary>Languages that are not runnable here (C# fragments, shell, diagrams outside a marker) are left alone.</summary>
    [Fact]
    public async Task CheckAsync_CSharpBlock_IsNotChecked_Test()
    {
        const string markdown = "```csharp\nvar x = notRunnable;\n```\n";

        IReadOnlyList<string> failures = await DocExampleChecker.CheckAsync(markdown, "sample.md");

        Assert.Empty(failures);
    }
}
