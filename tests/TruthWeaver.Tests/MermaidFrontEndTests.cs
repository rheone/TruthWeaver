namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The optional-parameter overloads and the fluent <see cref="MermaidOptionsBuilder"/> are front-ends over
/// <see cref="MermaidOptions"/>. All three call styles must give the same output for the same settings.
/// </summary>
public sealed class MermaidFrontEndTests
{
    /// <summary>The structure-only overload builds the same options as the record call.</summary>
    [Fact]
    public void Print_OptionalParameters_MatchesTheOptionsRecordOutput_Test()
    {
        OutlineNode root = Chain();

        string viaParameters = MermaidTreePrinter.Print(
            root,
            OperatorStyle.Symbolic,
            true,
            MermaidDirection.LeftRight,
            nodeShapes: true,
            twoLineTermLabels: true,
            palette: MermaidPalette.Dark,
            nodeStyle: Pick,
            compactChainThreshold: 4
        );

        Assert.Equal(MermaidTreePrinter.Print(root, AllSettings()), viaParameters);
    }

    /// <summary>The fluent builder produces options that render the same as the record call.</summary>
    [Fact]
    public void Build_FluentBuilder_MatchesTheOptionsRecordOutput_Test()
    {
        OutlineNode root = Chain();

        MermaidOptions built = new MermaidOptionsBuilder()
            .WithDirection(MermaidDirection.LeftRight)
            .WithNodeShapes()
            .WithOperatorStyle(OperatorStyle.Symbolic)
            .WithArgumentValues(true)
            .WithTwoLineTermLabels()
            .WithPalette(MermaidPalette.Dark)
            .WithNodeStyle(Pick)
            .WithCompactChainThreshold(4)
            .Build();

        Assert.Equal(AllSettings(), built);
        Assert.Equal(MermaidTreePrinter.Print(root, AllSettings()), MermaidTreePrinter.Print(root, built));
    }

    /// <summary>A builder without calls builds the default options.</summary>
    [Fact]
    public void Build_NoCalls_EqualsDefaultOptions_Test()
    {
        Assert.Equal(new MermaidOptions(), new MermaidOptionsBuilder().Build());
    }

    /// <summary>The evaluated overload takes the same knobs and keeps the result coloring.</summary>
    [Fact]
    public void Print_TraceWithOptionalParameters_MatchesTheOptionsRecordOutput_Test()
    {
        OutlineNode root = Chain();
        TraceNode[] operands = [.. Enumerable.Range(0, 6).Select(i => new TraceNode($"Term {i}", TruthValue.True, false, []))];
        TraceNode trace = new("OR", TruthValue.True, false, operands);

        string viaParameters = MermaidTreePrinter.Print(
            root,
            trace,
            OperatorStyle.Symbolic,
            true,
            MermaidDirection.LeftRight,
            nodeShapes: true,
            twoLineTermLabels: true,
            palette: MermaidPalette.Dark,
            nodeStyle: Pick,
            compactChainThreshold: 4
        );

        Assert.Equal(MermaidTreePrinter.Print(root, trace, AllSettings()), viaParameters);
    }

    /// <summary>The compiled-rule overloads give the same output as the record call.</summary>
    [Fact]
    public void PrintMermaid_OptionalParametersAndBuilder_MatchTheOptionsRecordOutput_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", false)
            .Build();
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(registry)
            .Compile("a OR b OR a OR b OR a OR b")
            .CompiledRule!;
        MermaidOptions options = new()
        {
            Direction = MermaidDirection.BottomTop,
            NodeShapes = true,
            Palette = MermaidPalette.Monochrome,
            CompactChainThreshold = 4,
        };

        string viaParameters = rule.PrintMermaid(
            direction: MermaidDirection.BottomTop,
            nodeShapes: true,
            palette: MermaidPalette.Monochrome,
            compactChainThreshold: 4
        );
        string viaBuilder = rule.PrintMermaid(
            new MermaidOptionsBuilder()
                .WithDirection(MermaidDirection.BottomTop)
                .WithNodeShapes()
                .WithPalette(MermaidPalette.Monochrome)
                .WithCompactChainThreshold(4)
                .Build()
        );

        Assert.Equal(rule.PrintMermaid(options), viaParameters);
        Assert.Equal(rule.PrintMermaid(options), viaBuilder);
    }

    private static NodeStyle? Pick(OutlineNode node)
    {
        return node.Label == "OR" ? NodeStyle.Highlight : null;
    }

    private static OutlineNode Chain()
    {
        OutlineNode[] terms =
        [
            .. Enumerable
                .Range(0, 6)
                .Select(i => new OutlineNode($"Term {i}", "desc", [], $"arg: {i}", Kind: OutlineNodeKind.Term)),
        ];
        return new OutlineNode("OR", "desc", terms, Kind: OutlineNodeKind.Operator);
    }

    private static MermaidOptions AllSettings()
    {
        return new MermaidOptions
        {
            Direction = MermaidDirection.LeftRight,
            NodeShapes = true,
            OperatorStyle = OperatorStyle.Symbolic,
            ShowArgumentValues = true,
            TwoLineTermLabels = true,
            Palette = MermaidPalette.Dark,
            NodeStyle = Pick,
            CompactChainThreshold = 4,
        };
    }
}
