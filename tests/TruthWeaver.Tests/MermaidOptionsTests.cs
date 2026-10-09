namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="MermaidOptions"/> — the single options record behind <see cref="MermaidTreePrinter"/> and
/// <c>CompiledRule.PrintMermaid</c>: diagram direction and per-role node shapes.
/// </summary>
public sealed class MermaidOptionsTests
{
    /// <summary>With default options the output equals the output of the option-less overload.</summary>
    [Fact]
    public void Print_DefaultOptions_EqualsOptionlessOutput_Test()
    {
        OutlineNode root = Sample();

        Assert.Equal(MermaidTreePrinter.Print(root), MermaidTreePrinter.Print(root, new MermaidOptions()));
    }

    /// <summary>Default options start with the top-down direction and draw plain rectangles.</summary>
    [Fact]
    public void Print_DefaultOptions_UsesTopDownAndPlainRectangles_Test()
    {
        string mermaid = MermaidTreePrinter.Print(Sample(), new MermaidOptions());

        Assert.StartsWith("flowchart TD\n", mermaid);
        Assert.Contains("n0[\"AND\"]", mermaid);
        Assert.Contains("n1[\"Has Crust\"]", mermaid);
        Assert.Contains("n2[\"True\"]", mermaid);
    }

    /// <summary>The chosen direction appears in the flowchart header.</summary>
    [Theory]
    [InlineData(MermaidDirection.TopDown, "flowchart TD\n")]
    [InlineData(MermaidDirection.LeftRight, "flowchart LR\n")]
    [InlineData(MermaidDirection.BottomTop, "flowchart BT\n")]
    [InlineData(MermaidDirection.RightLeft, "flowchart RL\n")]
    public void Print_Direction_WritesMatchingFlowchartHeader_Test(MermaidDirection direction, string header)
    {
        string mermaid = MermaidTreePrinter.Print(Sample(), new MermaidOptions { Direction = direction });

        Assert.StartsWith(header, mermaid);
    }

    /// <summary>With shapes on, operators, terms and constants each get a different shape.</summary>
    [Fact]
    public void Print_NodeShapesOn_GivesEachRoleADistinctShape_Test()
    {
        string mermaid = MermaidTreePrinter.Print(Sample(), new MermaidOptions { NodeShapes = true });

        Assert.Contains("n0{{\"AND\"}}", mermaid);
        Assert.Contains("n1(\"Has Crust\")", mermaid);
        Assert.Contains("n2((\"True\"))", mermaid);
    }

    /// <summary>The shape comes from the node kind, not from the label text.</summary>
    [Fact]
    public void Print_NodeShapesOn_TermNamedLikeAnOperatorStaysATerm_Test()
    {
        OutlineNode term = new("AND", "desc", [], Kind: OutlineNodeKind.Term);

        string mermaid = MermaidTreePrinter.Print(term, new MermaidOptions { NodeShapes = true });

        Assert.Contains("n0(\"AND\")", mermaid);
    }

    /// <summary>Direction and shapes combine with the operator style and the argument-value switch.</summary>
    [Fact]
    public void Print_AllOptions_AreHonoredTogether_Test()
    {
        OutlineNode root = new(
            "AND",
            "desc",
            [new OutlineNode("Has Crust", "desc", [], "crust: x")],
            Kind: OutlineNodeKind.Operator
        );

        string mermaid = MermaidTreePrinter.Print(
            root,
            new MermaidOptions
            {
                Direction = MermaidDirection.LeftRight,
                NodeShapes = true,
                OperatorStyle = OperatorStyle.Symbolic,
                ShowArgumentValues = false,
            }
        );

        Assert.StartsWith("flowchart LR\n", mermaid);
        Assert.Contains("n0{{\"∧\"}}", mermaid);
        Assert.Contains("n1(\"Has Crust\")", mermaid);
    }

    /// <summary>The evaluated overload takes options and keeps the result coloring.</summary>
    [Fact]
    public void Print_WithTraceAndOptions_KeepsColoringAndAppliesDirection_Test()
    {
        OutlineNode root = new("Has Crust", "desc", []);
        TraceNode trace = new("Has Crust", TruthValue.True, false, []);

        string mermaid = MermaidTreePrinter.Print(root, trace, new MermaidOptions { Direction = MermaidDirection.BottomTop });

        Assert.StartsWith("flowchart BT\n", mermaid);
        Assert.Contains("class n0 brTrue", mermaid);
    }

    /// <summary>A compiled rule marks its operators, terms and constants, so the shapes follow the real node kinds.</summary>
    [Fact]
    public void PrintMermaid_CompiledRuleWithShapes_ShapesOperatorsTermsAndConstants_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .Build();
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(registry).Compile("a AND TRUE").CompiledRule!;

        string mermaid = rule.PrintMermaid(new MermaidOptions { NodeShapes = true });

        Assert.Contains("{{\"AND\"}}", mermaid);
        Assert.Contains("((\"True\"))", mermaid);
        Assert.Matches(@"n\d\(""[^""]+""\)", mermaid);
    }

    private static OutlineNode Sample()
    {
        return new OutlineNode(
            "AND",
            "desc",
            [
                new OutlineNode("Has Crust", "desc", [], Kind: OutlineNodeKind.Term),
                new OutlineNode("True", "desc", [], Kind: OutlineNodeKind.Constant),
            ],
            Kind: OutlineNodeKind.Operator
        );
    }
}
