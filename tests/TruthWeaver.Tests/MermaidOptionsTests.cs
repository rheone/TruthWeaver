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

    /// <summary><see cref="MermaidOptions.Default"/> equals a new instance, so it produces the output of the option-less overload.</summary>
    [Fact]
    public void Default_ComparedWithANewInstance_IsEqual_Test()
    {
        Assert.Equal(new MermaidOptions(), MermaidOptions.Default);
        Assert.Equal(MermaidTreePrinter.Print(Sample()), MermaidTreePrinter.Print(Sample(), MermaidOptions.Default));
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

    /// <summary>With two-line labels on, a term with arguments renders a bold label and a plain argument line.</summary>
    [Fact]
    public void Print_TwoLineTermLabels_RendersBoldLabelAndPlainArgumentLine_Test()
    {
        string mermaid = MermaidTreePrinter.Print(WithArguments(), new MermaidOptions { TwoLineTermLabels = true });

        Assert.Contains("n1[\"`**Has Crust**\ncrust: thin`\"]", mermaid);
    }

    /// <summary>Two-line labels use Mermaid markdown strings and never raw HTML.</summary>
    [Fact]
    public void Print_TwoLineTermLabels_UsesNoRawHtml_Test()
    {
        string mermaid = MermaidTreePrinter.Print(WithArguments(), new MermaidOptions { TwoLineTermLabels = true });

        Assert.DoesNotContain("<", mermaid);
    }

    /// <summary>With argument values hidden, two-line labels show only the bold label.</summary>
    [Fact]
    public void Print_TwoLineTermLabelsWithoutArgumentValues_RendersOnlyTheBoldLabel_Test()
    {
        string mermaid = MermaidTreePrinter.Print(
            WithArguments(),
            new MermaidOptions { TwoLineTermLabels = true, ShowArgumentValues = false }
        );

        Assert.Contains("n1[\"`**Has Crust**`\"]", mermaid);
        Assert.DoesNotContain("crust: thin", mermaid);
    }

    /// <summary>Operators and constants keep their plain labels when two-line labels are on.</summary>
    [Fact]
    public void Print_TwoLineTermLabels_LeavesOperatorsAndConstantsPlain_Test()
    {
        string mermaid = MermaidTreePrinter.Print(Sample(), new MermaidOptions { TwoLineTermLabels = true });

        Assert.Contains("n0[\"AND\"]", mermaid);
        Assert.Contains("n2[\"True\"]", mermaid);
    }

    /// <summary>Markdown and quote characters in a term's text cannot break the markdown string.</summary>
    [Fact]
    public void Print_TwoLineTermLabels_EscapesMarkdownAndQuoteCharacters_Test()
    {
        OutlineNode term = new("A*b", "desc", [], "x: `q` \"v\" _w_", Kind: OutlineNodeKind.Term);

        string mermaid = MermaidTreePrinter.Print(term, new MermaidOptions { TwoLineTermLabels = true });

        int start = mermaid.IndexOf("n0[", StringComparison.Ordinal);
        string nodeLine = mermaid[start..mermaid.IndexOf("]\n", start, StringComparison.Ordinal)];
        Assert.Equal(2, nodeLine.Count(c => c == '`'));
        Assert.Equal(2, nodeLine.Count(c => c == '"'));
        Assert.DoesNotContain("A*b", mermaid);
        Assert.DoesNotContain("_w_", mermaid);
    }

    /// <summary>Two-line labels combine with node shapes and keep the evaluation coloring.</summary>
    [Fact]
    public void Print_TwoLineTermLabelsWithShapesAndTrace_ComposesAllThree_Test()
    {
        OutlineNode root = WithArguments();
        TraceNode trace = new("AND", TruthValue.True, false, [new("Has Crust", TruthValue.True, false, [])]);

        string mermaid = MermaidTreePrinter.Print(
            root,
            trace,
            new MermaidOptions { TwoLineTermLabels = true, NodeShapes = true }
        );

        Assert.Contains("n1(\"`**Has Crust**\ncrust: thin`\")", mermaid);
        Assert.Contains("class n1 brTrue", mermaid);
    }

    /// <summary>The default palette writes today's class definitions.</summary>
    [Fact]
    public void Print_DefaultPalette_WritesTheLightClassDefinitions_Test()
    {
        string mermaid = MermaidTreePrinter.Print(
            new OutlineNode("a", "d", []),
            new TraceNode("a", TruthValue.True, false, [])
        );

        Assert.Contains("classDef brTrue fill:#d4edda,stroke:#28a745,color:#155724;\n", mermaid);
        Assert.Contains("classDef brSkipped fill:#e9ecef,stroke:#adb5bd,color:#6c757d,stroke-dasharray: 4 3;\n", mermaid);
        Assert.Equal(MermaidPalette.Light, new MermaidOptions().Palette);
    }

    /// <summary>Each preset palette changes the class definitions of a decision diagram.</summary>
    [Fact]
    public void Print_PresetPalette_WritesItsOwnClassDefinitions_Test()
    {
        OutlineNode root = new("a", "d", []);
        TraceNode trace = new("a", TruthValue.False, false, []);

        foreach (
            MermaidPalette palette in new[] { MermaidPalette.ColorblindSafe, MermaidPalette.Monochrome, MermaidPalette.Dark }
        )
        {
            string mermaid = MermaidTreePrinter.Print(root, trace, new MermaidOptions { Palette = palette });

            Assert.Contains($"classDef brFalse {palette.False};\n", mermaid);
            Assert.NotEqual(MermaidPalette.Light.False, palette.False);
        }
    }

    /// <summary>The monochrome preset separates the states by stroke style, so no two states share one.</summary>
    [Fact]
    public void Monochrome_StatesDifferInStrokeStyle_Test()
    {
        MermaidPalette p = MermaidPalette.Monochrome;
        string[] states = [p.True, p.False, p.Unknown, p.Skipped];

        Assert.Equal(4, states.Select(StrokeStyle).Distinct().Count());

        static string StrokeStyle(string style)
        {
            return string.Join(
                ",",
                style
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x =>
                        x.StartsWith("stroke-width", StringComparison.Ordinal)
                        || x.StartsWith("stroke-dasharray", StringComparison.Ordinal)
                    )
            );
        }
    }

    /// <summary>Every preset defines all six classes.</summary>
    [Fact]
    public void Presets_DefineAllSixClasses_Test()
    {
        foreach (
            MermaidPalette p in new[]
            {
                MermaidPalette.Light,
                MermaidPalette.ColorblindSafe,
                MermaidPalette.Monochrome,
                MermaidPalette.Dark,
            }
        )
        {
            Assert.All([p.True, p.False, p.Unknown, p.Skipped, p.Highlight, p.Mute], s => Assert.Contains("fill:", s));
        }
    }

    /// <summary>A caller-supplied palette drives the class definitions.</summary>
    [Fact]
    public void Print_CustomPalette_WritesTheCallerStyles_Test()
    {
        MermaidPalette custom = MermaidPalette.Light with { True = "fill:#000,stroke:#fff,color:#fff" };

        string mermaid = MermaidTreePrinter.Print(
            new OutlineNode("a", "d", []),
            new TraceNode("a", TruthValue.True, false, []),
            new MermaidOptions { Palette = custom }
        );

        Assert.Contains("classDef brTrue fill:#000,stroke:#fff,color:#fff;\n", mermaid);
    }

    /// <summary>A palette style cannot inject a new statement through a line break or semicolon.</summary>
    [Fact]
    public void Print_CustomPaletteWithLineBreak_StaysOnOneStatement_Test()
    {
        MermaidPalette custom = MermaidPalette.Light with { True = "fill:#000;\nclick n0 call x()" };

        string mermaid = MermaidTreePrinter.Print(
            new OutlineNode("a", "d", []),
            new TraceNode("a", TruthValue.True, false, []),
            new MermaidOptions { Palette = custom }
        );

        Assert.DoesNotContain("\nclick", mermaid);
    }

    private static OutlineNode WithArguments()
    {
        return new OutlineNode(
            "AND",
            "desc",
            [new OutlineNode("Has Crust", "desc", [], "crust: thin", Kind: OutlineNodeKind.Term)],
            Kind: OutlineNodeKind.Operator
        );
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
