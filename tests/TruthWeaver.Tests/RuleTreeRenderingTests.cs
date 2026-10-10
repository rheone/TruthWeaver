namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintMermaid(MermaidOptions)"/>/<see cref="CompiledRule{TContext}.PrintPlainText(bool)"/>
/// — structure-only and evaluation-colored tree rendering, for delivery to a diagram UI or a log.
/// </summary>
public sealed class RuleTreeRenderingTests
{
    /// <summary>Structural Mermaid output has no evaluation coloring.</summary>
    [Fact]
    public void Mermaid_Structural_HasNoEvaluationColoring_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string mermaid = rule.PrintMermaid();

        Assert.StartsWith("flowchart TD", mermaid);
        Assert.Contains("AND", mermaid);
        Assert.Contains("-->", mermaid);
        Assert.DoesNotContain("classDef", mermaid);
    }

    /// <summary>Mermaid output always includes a start marker pointing at the root.</summary>
    [Fact]
    public void Mermaid_Output_IncludesStartMarkerAtRoot_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string mermaid = rule.PrintMermaid();

        Assert.Contains("Start([\"Start\"]) --> n0", mermaid);
    }

    /// <summary>Mermaid output includes a term's argument values by default.</summary>
    [Fact]
    public void Mermaid_Output_IncludesArgumentValuesByDefault_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string mermaid = rule.PrintMermaid();

        Assert.Contains("crust: #quot;thin#quot;", mermaid);
    }

    /// <summary>Mermaid output omits argument values when disabled.</summary>
    [Fact]
    public void Mermaid_Output_OmitsArgumentValuesWhenDisabled_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string mermaid = rule.PrintMermaid(showArgumentValues: false);

        Assert.DoesNotContain("crust", mermaid);
    }

    /// <summary>Plain text output includes a term's argument values by default.</summary>
    [Fact]
    public void PlainText_Output_IncludesArgumentValuesByDefault_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string text = rule.PrintPlainText();

        Assert.Contains("crust: \"thin\"", text);
    }

    /// <summary>Plain text output omits argument values when disabled.</summary>
    [Fact]
    public void PlainText_Output_OmitsArgumentValuesWhenDisabled_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string text = rule.PrintPlainText(showArgumentValues: false);

        Assert.DoesNotContain("crust: \"thin\"", text);
    }

    /// <summary>Argument values are absent for zero argument terms.</summary>
    [Fact]
    public void Arguments_ZeroArgumentTerms_AreAbsent_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a", registry => registry.AddConstant("a", true));

        string mermaid = rule.PrintMermaid();
        string text = rule.PrintPlainText();

        Assert.DoesNotContain("(", mermaid.Replace("Start([\"Start\"])", string.Empty));
        Assert.DoesNotContain("(", text);
    }

    /// <summary>Evaluated Mermaid output colors true, false and skipped nodes.</summary>
    [Fact]
    public async Task Mermaid_Evaluated_ColorsTrueFalseAndSkippedNodes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", false).AddConstant("b", true)
        );

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string mermaid = rule.PrintMermaid(decision);

        Assert.Contains("classDef brTrue", mermaid);
        Assert.Contains("classDef brFalse", mermaid);
        Assert.Contains("classDef brSkipped", mermaid);
        Assert.Contains("brFalse", mermaid);
        Assert.Contains("brSkipped", mermaid);
    }

    /// <summary>PrintMermaid rejects a decision with no evaluated tree.</summary>
    [Fact]
    public void PrintMermaid_DecisionWithoutEvaluatedTree_Throws_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a", registry => registry.AddConstant("a", true));
        Decision decisionWithoutTree = new(TruthValue.True, []);

        Assert.Throws<ArgumentException>(() => rule.PrintMermaid(decisionWithoutTree));
    }

    /// <summary>Structural plain text output has no evaluation suffixes.</summary>
    [Fact]
    public void PlainText_Structural_HasNoEvaluationSuffixes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string text = rule.PrintPlainText();

        Assert.Contains("AND", text);
        Assert.DoesNotContain("[true]", text);
        Assert.DoesNotContain("[skipped]", text);
    }

    /// <summary>Evaluated plain text output marks the short circuited operand as skipped.</summary>
    [Fact]
    public async Task PlainText_Evaluated_MarksShortCircuitedOperandSkipped_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", false).AddConstant("b", true)
        );

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string text = rule.PrintPlainText(decision);

        Assert.Contains("[false]", text);
        Assert.Contains("[skipped]", text);
        Assert.DoesNotContain("[true]", text);
    }

    /// <summary>Plain text renders operators in the requested style.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "AND", "OR", "NOT", "XOR", "EQUIVALENT")]
    [InlineData(OperatorStyle.Symbolic, "∧", "∨", "¬", "⊕", "↔")]
    [InlineData(OperatorStyle.CStyle, "&&", "||", "!", "^", "==")]
    public void PlainText_Style_RendersOperatorsInRequestedStyle_Test(
        OperatorStyle style,
        string and,
        string or,
        string not,
        string xor,
        string xnor
    )
    {
        Assert.Contains(and, PlainTextTreePrinter.Print(BinaryNode("AND"), style));
        Assert.Contains(or, PlainTextTreePrinter.Print(BinaryNode("OR"), style));
        Assert.Contains(not, PlainTextTreePrinter.Print(UnaryNode("NOT"), style));
        Assert.Contains(xor, PlainTextTreePrinter.Print(BinaryNode("XOR"), style));
        Assert.Contains(xnor, PlainTextTreePrinter.Print(BinaryNode("EQUIVALENT"), style));
    }

    /// <summary>Mermaid renders operators in the requested style.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "AND", "OR", "NOT", "XOR", "EQUIVALENT")]
    [InlineData(OperatorStyle.Symbolic, "∧", "∨", "¬", "⊕", "↔")]
    [InlineData(OperatorStyle.CStyle, "&&", "||", "!", "^", "==")]
    public void Mermaid_Style_RendersOperatorsInRequestedStyle_Test(
        OperatorStyle style,
        string and,
        string or,
        string not,
        string xor,
        string xnor
    )
    {
        Assert.Contains(and, MermaidTreePrinter.Print(BinaryNode("AND"), style));
        Assert.Contains(or, MermaidTreePrinter.Print(BinaryNode("OR"), style));
        Assert.Contains(not, MermaidTreePrinter.Print(UnaryNode("NOT"), style));
        Assert.Contains(xor, MermaidTreePrinter.Print(BinaryNode("XOR"), style));
        Assert.Contains(xnor, MermaidTreePrinter.Print(BinaryNode("EQUIVALENT"), style));
    }

    /// <summary>Plain text and Mermaid tree printers render Nand and Nor using the requested operator style.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "NAND", "NOR")]
    [InlineData(OperatorStyle.Symbolic, "↑", "↓")]
    [InlineData(OperatorStyle.CStyle, "NAND", "NOR")]
    public void Render_NandAndNor_PlainTextAndMermaidUseRequestedStyle_Test(OperatorStyle style, string nand, string nor)
    {
        Assert.Contains(nand, PlainTextTreePrinter.Print(BinaryNode("NAND"), style));
        Assert.Contains(nand, MermaidTreePrinter.Print(BinaryNode("NAND"), style));
        Assert.Contains(nor, PlainTextTreePrinter.Print(BinaryNode("NOR"), style));
        Assert.Contains(nor, MermaidTreePrinter.Print(BinaryNode("NOR"), style));
    }

    /// <summary>Plain text and Mermaid tree printers render Implies using the requested operator style.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "IMPLIES")]
    [InlineData(OperatorStyle.Symbolic, "→")]
    [InlineData(OperatorStyle.CStyle, "IMPLIES")]
    public void Render_Implies_PlainTextAndMermaidUseRequestedStyle_Test(OperatorStyle style, string expected)
    {
        Assert.Contains(expected, PlainTextTreePrinter.Print(BinaryNode("IMPLIES"), style));
        Assert.Contains(expected, MermaidTreePrinter.Print(BinaryNode("IMPLIES"), style));
    }

    /// <summary>Plain text keeps ExactlyOne and threshold labels in word form in every style.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void PlainText_WordLabels_KeptInWordFormInEveryStyle_Test(OperatorStyle style)
    {
        OutlineNode exactlyOne = new("ExactlyOne", "desc", [Leaf("a"), Leaf("b")]);
        OutlineNode atLeast = new("AtLeast(3)", "desc", [Leaf("a"), Leaf("b"), Leaf("c")]);

        Assert.Contains("ExactlyOne", PlainTextTreePrinter.Print(exactlyOne, style));
        Assert.Contains("AtLeast(3)", PlainTextTreePrinter.Print(atLeast, style));
    }

    /// <summary>Mermaid keeps ExactlyOne and threshold labels in word form in every style.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Mermaid_WordLabels_KeptInWordFormInEveryStyle_Test(OperatorStyle style)
    {
        OutlineNode exactlyOne = new("ExactlyOne", "desc", [Leaf("a"), Leaf("b")]);
        OutlineNode atLeast = new("AtLeast(3)", "desc", [Leaf("a"), Leaf("b"), Leaf("c")]);

        Assert.Contains("ExactlyOne", MermaidTreePrinter.Print(exactlyOne, style));
        Assert.Contains("AtLeast(3)", MermaidTreePrinter.Print(atLeast, style));
    }

    /// <summary>
    /// Under <see cref="OperatorStyle.CStyle"/> the tree printers label <c>If</c> as <c>?:</c>, mirroring the DSL
    /// ternary; <see cref="OperatorStyle.Word"/> and <see cref="OperatorStyle.Symbolic"/> keep the label <c>If</c>.
    /// </summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "If")]
    [InlineData(OperatorStyle.Symbolic, "If")]
    [InlineData(OperatorStyle.CStyle, "?:")]
    public void PlainText_and_Mermaid_label_If_in_the_requested_style_Test(OperatorStyle style, string expected)
    {
        OutlineNode ifNode = new("If", "desc", [Leaf("a"), Leaf("b"), Leaf("c")]);

        string plain = PlainTextTreePrinter.Print(ifNode, style);
        string mermaid = MermaidTreePrinter.Print(ifNode, style);

        Assert.StartsWith(expected, plain);
        Assert.Contains($"[\"{expected}\"]", mermaid);
    }

    /// <summary>The plain text default style is Word when no style argument is given.</summary>
    [Fact]
    public void PlainText_DefaultStyle_IsWordWithoutStyleArgument_Test()
    {
        string text = PlainTextTreePrinter.Print(BinaryNode("AND"));

        Assert.Contains("AND", text);
    }

    /// <summary>The Mermaid default style is Word when no style argument is given.</summary>
    [Fact]
    public void Mermaid_DefaultStyle_IsWordWithoutStyleArgument_Test()
    {
        string mermaid = MermaidTreePrinter.Print(BinaryNode("AND"));

        Assert.Contains("AND", mermaid);
    }

    /// <summary>The evaluated plain text overload accepts an operator style.</summary>
    [Fact]
    public async Task PlainText_EvaluatedOverload_AcceptsOperatorStyle_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string text = PlainTextTreePrinter.Print(rule.Outline(), decision.TraceTree!, OperatorStyle.Symbolic);

        Assert.Contains("∧", text);
    }

    /// <summary>The evaluated Mermaid overload accepts an operator style.</summary>
    [Fact]
    public async Task Mermaid_EvaluatedOverload_AcceptsOperatorStyle_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string mermaid = MermaidTreePrinter.Print(rule.Outline(), decision.TraceTree!, OperatorStyle.CStyle);

        Assert.Contains("&&", mermaid);
    }

    /// <summary>Mermaid output escapes double quotes in labels.</summary>
    [Fact]
    public void Mermaid_Output_EscapesDoubleQuotesInLabels_Test()
    {
        OutlineNode node = Leaf("has \"quotes\" inside");

        string mermaid = MermaidTreePrinter.Print(node);

        Assert.Contains("#quot;", mermaid);
        Assert.Contains("n0[\"has #quot;quotes#quot; inside\"]", mermaid);
        Assert.DoesNotContain("[\"has \"quotes\" inside\"]", mermaid);
    }

    /// <summary>Mermaid output sanitizes carriage returns and newlines in labels.</summary>
    [Theory]
    [InlineData("line one\r\nline two")]
    [InlineData("line one\rline two")]
    [InlineData("line one\nline two")]
    public void Mermaid_Output_SanitizesNewlinesInLabels_Test(string label)
    {
        OutlineNode node = Leaf(label);

        string mermaid = MermaidTreePrinter.Print(node);

        Assert.DoesNotContain('\r', mermaid);
        Assert.DoesNotContain("line one\nline two", mermaid);
        Assert.Matches(@"n0\[""line one +line two""\]", mermaid);
    }

    private static OutlineNode BinaryNode(string label)
    {
        return new OutlineNode(label, "desc", [Leaf("a"), Leaf("b")]);
    }

    private static OutlineNode UnaryNode(string label)
    {
        return new OutlineNode(label, "desc", [Leaf("a")]);
    }

    private static OutlineNode Leaf(string label)
    {
        return new OutlineNode(label, "desc", []);
    }

    private static CompiledRule<RuleTestContext> Compile(
        string dsl,
        Func<PredicateRegistryBuilder<RuleTestContext>, PredicateRegistryBuilder<RuleTestContext>> configure
    )
    {
        PredicateRegistry<RuleTestContext> registry = configure(PredicateRegistry<RuleTestContext>.CreateBuilder()).Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
