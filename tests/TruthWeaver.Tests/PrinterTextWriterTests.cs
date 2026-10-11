namespace TruthWeaver.Tests;

using System.Text;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The <see cref="TextWriter"/> overloads of the <see cref="CompiledRule{TContext}"/> printers and of
/// <see cref="RuleDiffPrinter"/>: each writes exactly the text of its string-returning form, rejects a null writer, and lets
/// a failing writer's exception reach the caller unchanged.
/// </summary>
public sealed class PrinterTextWriterTests
{
    /// <summary>Gets every printer case name. Each name is a key of the table that <see cref="BuildCasesAsync"/> returns.</summary>
    public static TheoryData<string> CaseNames { get; } =
    [
        "RuleText.Grouping",
        "RuleText.Options",
        "RuleText.OptionsWidth",
        "Json",
        "PlainText",
        "PlainText.Decision",
        "Mermaid",
        "Mermaid.Options",
        "Mermaid.Decision",
        "Mermaid.DecisionOptions",
        "Equation",
        "Equation.Latex",
        "EquationWithLegend",
        "Html",
        "Html.Options",
        "Html.Decision",
        "Dot",
        "Dot.Options",
        "Dot.Decision",
        "RuleDiff",
        "RuleDiff.Empty",
    ];

    /// <summary>The writer overload writes exactly the text that the string form returns.</summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task Print_ToStringWriter_WritesExactlyTheStringForm_Test(string caseName)
    {
        PrinterCase printerCase = (await BuildCasesAsync())[caseName];
        await using StringWriter writer = new();

        printerCase.Write(writer);

        Assert.Equal(printerCase.Expected, writer.ToString());
        Assert.NotEmpty(printerCase.Expected);
    }

    /// <summary>A writer that throws surfaces its exception to the caller, unwrapped and unchanged.</summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task Print_ToFailingWriter_PropagatesTheWriterExceptionUnchanged_Test(string caseName)
    {
        PrinterCase printerCase = (await BuildCasesAsync())[caseName];
        IOException failure = new("disk full");
        await using ThrowingWriter writer = new(failure);

        IOException thrown = Assert.Throws<IOException>(() => printerCase.Write(writer));

        Assert.Same(failure, thrown);
    }

    /// <summary>A null writer throws <see cref="ArgumentNullException"/> that names the writer parameter.</summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task Print_NullWriter_ThrowsArgumentNullException_Test(string caseName)
    {
        PrinterCase printerCase = (await BuildCasesAsync())[caseName];

        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => printerCase.Write(null!));

        Assert.Equal("writer", thrown.ParamName);
    }

    /// <summary>A rule with no term writes only the equation, with no newline and no legend.</summary>
    [Fact]
    public void PrintEquationWithLegend_RuleWithoutTerms_WritesOnlyTheEquation_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True AND False");
        using StringWriter writer = new();

        rule.PrintEquationWithLegend(writer);

        Assert.Equal(rule.PrintEquationWithLegend().Equation, writer.ToString());
    }

    /// <summary>A rule with terms writes the equation, a newline, then the legend text, with no trailing newline.</summary>
    [Fact]
    public void PrintEquationWithLegend_RuleWithTerms_WritesEquationNewlineLegend_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"x\")");
        EquationWithLegend expected = rule.PrintEquationWithLegend();
        using StringWriter writer = new();

        rule.PrintEquationWithLegend(writer);

        Assert.Equal(expected.Equation + "\n" + expected.LegendText, writer.ToString());
        Assert.DoesNotContain("\n", expected.Equation, StringComparison.Ordinal);
        Assert.False(writer.ToString().EndsWith('\n'));
    }

    /// <summary>A bad argument throws before the writer receives any text.</summary>
    [Fact]
    public void PrintRuleText_UnsupportedOperatorStyle_ThrowsAndWritesNothing_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");
        using StringWriter writer = new();

        Assert.Throws<ArgumentException>(() => rule.PrintRuleText(writer, operatorStyle: OperatorStyle.CStyle));

        Assert.Empty(writer.ToString());
    }

    /// <summary>A decision with no trace tree throws before the writer receives any text.</summary>
    [Fact]
    public void PrintHtml_DecisionWithoutTraceTree_ThrowsAndWritesNothing_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");
        using StringWriter writer = new();

        Assert.Throws<ArgumentException>(() => rule.PrintHtml(writer, new Decision(TruthValue.True, [])));

        Assert.Empty(writer.ToString());
    }

    /// <summary>A null diff throws <see cref="ArgumentNullException"/> that names the diff parameter.</summary>
    [Fact]
    public void RuleDiffPrinterPrint_NullDiff_ThrowsNamingDiffParameter_Test()
    {
        using StringWriter writer = new();

        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => RuleDiffPrinter.Print(writer, null!));

        Assert.Equal("diff", thrown.ParamName);
    }

    private static async Task<Dictionary<string, PrinterCase>> BuildCasesAsync()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND (b OR hasCrust(crust: \"thin\")) AND NOT c AND AtLeast(2, a, b, c)"
        );
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        MermaidOptions mermaid = new() { Direction = MermaidDirection.LeftRight, NodeShapes = true };
        RuleTextOptions wide = new() { Width = 20 };
        EquationOptions latex = new() { Dialect = EquationDialect.LaTeX };
        HtmlOptions html = new() { SimpleVariables = true };
        DotOptions dot = new() { NodeShapes = false };
        RuleDiffResult diff = RuleDiff.Compare(Compile("a AND b"), Compile("a AND b AND c"));
        EquationWithLegend legend = rule.PrintEquationWithLegend();

        List<PrinterCase> cases =
        [
            new(
                "RuleText.Grouping",
                rule.PrintRuleText(GroupingStyle.DepthCycling, OperatorStyle.Symbolic),
                w => rule.PrintRuleText(w, GroupingStyle.DepthCycling, OperatorStyle.Symbolic)
            ),
            new(
                "RuleText.Options",
                rule.PrintRuleText(new RuleTextOptions()),
                w => rule.PrintRuleText(w, new RuleTextOptions())
            ),
            new("RuleText.OptionsWidth", rule.PrintRuleText(wide), w => rule.PrintRuleText(w, wide)),
            new("Json", rule.PrintJson(), w => rule.PrintJson(w)),
            new("PlainText", rule.PrintPlainText(false), w => rule.PrintPlainText(w, false)),
            new("PlainText.Decision", rule.PrintPlainText(decision), w => rule.PrintPlainText(w, decision)),
            new(
                "Mermaid",
                rule.PrintMermaid(direction: MermaidDirection.LeftRight, nodeShapes: true),
                w => rule.PrintMermaid(w, direction: MermaidDirection.LeftRight, nodeShapes: true)
            ),
            new("Mermaid.Options", rule.PrintMermaid(mermaid), w => rule.PrintMermaid(w, mermaid)),
            new("Mermaid.Decision", rule.PrintMermaid(decision), w => rule.PrintMermaid(w, decision)),
            new("Mermaid.DecisionOptions", rule.PrintMermaid(decision, mermaid), w => rule.PrintMermaid(w, decision, mermaid)),
            new("Equation", rule.PrintEquation(), w => rule.PrintEquation(w)),
            new("Equation.Latex", rule.PrintEquation(latex), w => rule.PrintEquation(w, latex)),
            new("EquationWithLegend", legend.Equation + "\n" + legend.LegendText, w => rule.PrintEquationWithLegend(w)),
            new("Html", rule.PrintHtml(), w => rule.PrintHtml(w)),
            new("Html.Options", rule.PrintHtml(html), w => rule.PrintHtml(w, html)),
            new("Html.Decision", rule.PrintHtml(decision), w => rule.PrintHtml(w, decision)),
            new("Dot", rule.PrintDot(), w => rule.PrintDot(w)),
            new("Dot.Options", rule.PrintDot(dot), w => rule.PrintDot(w, dot)),
            new("Dot.Decision", rule.PrintDot(decision), w => rule.PrintDot(w, decision)),
            new("RuleDiff", RuleDiffPrinter.Print(diff), w => RuleDiffPrinter.Print(w, diff)),
            new(
                "RuleDiff.Empty",
                RuleDiffPrinter.Print(new RuleDiffResult([])),
                w => RuleDiffPrinter.Print(w, new RuleDiffResult([]))
            ),
        ];
        return cases.ToDictionary(c => c.Name);
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", false)
            .AddStringArgPredicate("hasCrust", "crust", "thin")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }

    private sealed record PrinterCase(string Name, string Expected, Action<TextWriter> Write);

    /// <summary>A writer whose every write fails with a preset exception.</summary>
    private sealed class ThrowingWriter(Exception failure) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            throw failure;
        }

        public override void Write(string? value)
        {
            throw failure;
        }
    }
}
