namespace TruthWeaver.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintDot(DotOptions?)"/>: a Graphviz DOT digraph of a rule, with a node shape for each
/// role, stable node ids and labels escaped for DOT. No DOT parser is a dependency, so the tests compare expected strings
/// and run a structural check on every output.
/// </summary>
public sealed partial class DotPrinterTests
{
    /// <summary>A single term is one box-shaped node and no edge.</summary>
    [Fact]
    public void PrintDot_SingleTerm_WritesOneRoundedBoxNode_Test()
    {
        string dot = Compile("a").PrintDot();

        Assert.Equal(
            Lines("digraph TruthWeaver {", "    rankdir=TB;", "    n0 [label=\"a\", shape=box, style=\"rounded\"];", "}"),
            dot
        );
        AssertWellFormed(dot);
    }

    /// <summary>Operators, terms and constants get a hexagon, a rounded box and a circle, with the edges after each operand.</summary>
    [Fact]
    public void PrintDot_AndOrAndConstant_WritesOneShapePerRoleWithStableIds_Test()
    {
        string dot = Compile("a AND (b OR True)").PrintDot();

        Assert.Equal(
            Lines(
                "digraph TruthWeaver {",
                "    rankdir=TB;",
                "    n0 [label=\"AND\", shape=hexagon];",
                "    n1 [label=\"a\", shape=box, style=\"rounded\"];",
                "    n0 -> n1;",
                "    n2 [label=\"OR\", shape=hexagon];",
                "    n3 [label=\"b\", shape=box, style=\"rounded\"];",
                "    n2 -> n3;",
                "    n4 [label=\"True\", shape=circle];",
                "    n2 -> n4;",
                "    n0 -> n2;",
                "}"
            ),
            dot
        );
        AssertWellFormed(dot);
    }

    /// <summary>The same rule always gets the same text, so the ids are stable.</summary>
    [Fact]
    public void PrintDot_SameRuleTwice_GivesTheSameText_Test()
    {
        Assert.Equal(Compile("a XOR (b AND c)").PrintDot(), Compile("a XOR (b AND c)").PrintDot());
    }

    /// <summary>With shapes off, every node is a plain box, and the direction sets the rank direction.</summary>
    [Fact]
    public void PrintDot_ShapesOffAndLeftRight_WritesPlainBoxesAndRankdirLr_Test()
    {
        string dot = Compile("NOT a").PrintDot(new DotOptions { NodeShapes = false, Direction = MermaidDirection.LeftRight });

        Assert.Equal(
            Lines(
                "digraph TruthWeaver {",
                "    rankdir=LR;",
                "    n0 [label=\"NOT\", shape=box];",
                "    n1 [label=\"a\", shape=box];",
                "    n0 -> n1;",
                "}"
            ),
            dot
        );
        AssertWellFormed(dot);
    }

    /// <summary>Every direction maps to its Graphviz rank direction.</summary>
    [Theory]
    [InlineData(MermaidDirection.TopDown, "TB")]
    [InlineData(MermaidDirection.LeftRight, "LR")]
    [InlineData(MermaidDirection.BottomTop, "BT")]
    [InlineData(MermaidDirection.RightLeft, "RL")]
    public void PrintDot_Direction_WritesMatchingRankdir_Test(MermaidDirection direction, string rankdir)
    {
        string dot = Compile("a").PrintDot(new DotOptions { Direction = direction });

        Assert.Contains($"    rankdir={rankdir};\n", dot);
    }

    /// <summary>The operator style, argument switch and constant format change the labels.</summary>
    [Fact]
    public void PrintDot_StyleArgumentsAndConstants_ChangeTheLabels_Test()
    {
        string dot = Compile("hasCrust(crust: \"thin\") OR False")
            .PrintDot(
                new DotOptions
                {
                    OperatorStyle = OperatorStyle.Symbolic,
                    ShowArgumentValues = false,
                    Constants = new ConstantFormat { Style = ConstantStyle.Letters },
                }
            );

        Assert.Contains("label=\"∨\"", dot);
        Assert.Contains("label=\"hasCrust\"", dot);
        Assert.Contains("label=\"F\"", dot);
    }

    /// <summary>Quotes, backslashes and line breaks in a label are escaped, so the label stays one DOT string.</summary>
    [Fact]
    public void Print_QuoteBackslashAndLineBreakInLabel_AreEscaped_Test()
    {
        OutlineNode term = new("a\"b\\c\nd<e>&f", "description", []);

        string dot = DotPrinter.Print(term, DotOptions.Default);

        Assert.Contains("label=\"a\\\"b\\\\c\\nd<e>&f\"", dot);
        AssertWellFormed(dot);
    }

    /// <summary>Markup characters in an argument value reach the label unchanged, because DOT has no markup in a quoted string, and the output stays well formed.</summary>
    [Fact]
    public void PrintDot_MarkupAndQuotesInArgumentValue_StaysOneEscapedLabel_Test()
    {
        string dot = Compile("hasCrust(crust: \"<b>&'x\\\"y\") AND a").PrintDot();

        Assert.Contains("label=\"hasCrust (crust: \\\"<b>&'x\\\\\\\"y\\\")\"", dot);
        AssertWellFormed(dot);
    }

    /// <summary>A decision fills each node by its state, and a short-circuited node is gray.</summary>
    [Fact]
    public async Task PrintDot_WithDecision_FillsEachNodeByItsState_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("False AND a");
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        string dot = rule.PrintDot(decision);

        Assert.Equal(
            Lines(
                "digraph TruthWeaver {",
                "    rankdir=TB;",
                "    n0 [label=\"AND\", shape=hexagon, style=\"filled\", fillcolor=\"#ffcdd2\"];",
                "    n1 [label=\"False\", shape=circle, style=\"filled\", fillcolor=\"#ffcdd2\"];",
                "    n0 -> n1;",
                "    n2 [label=\"a\", shape=box, style=\"rounded,filled\", fillcolor=\"#eeeeee\"];",
                "    n0 -> n2;",
                "}"
            ),
            dot
        );
        AssertWellFormed(dot);
    }

    /// <summary>A decision with no trace tree is a caller error.</summary>
    [Fact]
    public void PrintDot_DecisionWithoutTraceTree_ThrowsArgumentException_Test()
    {
        Assert.Throws<ArgumentException>(() => Compile("a").PrintDot(new Decision(TruthValue.True, [])));
    }

    /// <summary>Several rule shapes all give structurally valid DOT: balanced braces and every edge endpoint declared.</summary>
    [Theory]
    [InlineData("a")]
    [InlineData("NOT (a AND b)")]
    [InlineData("a IMPLIES (b EQUIVALENT c)")]
    [InlineData("AtLeast(2, a, b, c) OR If(a, b, c)")]
    [InlineData("hasCrust(crust: \"a\\\"b\") NAND (Unknown NOR c)")]
    public void PrintDot_ManyRules_ProducesStructurallyValidDot_Test(string dsl)
    {
        AssertWellFormed(Compile(dsl).PrintDot());
    }

    private static string Lines(params string[] lines)
    {
        return string.Join("\n", lines) + "\n";
    }

    /// <summary>
    /// A structural DOT check, since no DOT parser is a dependency: the text is a <c>digraph</c>, quotes and braces balance
    /// outside quoted strings, and every edge endpoint is a declared node id.
    /// </summary>
    private static void AssertWellFormed(string dot)
    {
        Assert.StartsWith("digraph ", dot, StringComparison.Ordinal);
        Assert.EndsWith("}\n", dot, StringComparison.Ordinal);

        int depth = 0;
        bool inQuote = false;
        bool escaped = false;
        foreach (char c in dot)
        {
            if (inQuote)
            {
                Assert.NotEqual('\n', c);
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inQuote = false;
                }
            }
            else if (c == '"')
            {
                inQuote = true;
            }
            else if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                Assert.True(depth >= 0, "A closing brace has no opening brace.");
            }
        }

        Assert.False(inQuote, "A quoted string is not closed.");
        Assert.Equal(0, depth);

        HashSet<string> declared = [.. NodeRegex().Matches(dot).Select(m => m.Groups[1].Value)];
        Assert.NotEmpty(declared);
        MatchCollection edges = EdgeRegex().Matches(dot);
        foreach (Match edge in edges)
        {
            Assert.Contains(edge.Groups[1].Value, declared);
            Assert.Contains(edge.Groups[2].Value, declared);
        }

        // A tree of N nodes has N - 1 edges.
        Assert.Equal(declared.Count - 1, edges.Count);
    }

    [GeneratedRegex(@"^    (n\d+) \[", RegexOptions.Multiline)]
    private static partial Regex NodeRegex();

    [GeneratedRegex(@"^    (n\d+) -> (n\d+);$", RegexOptions.Multiline)]
    private static partial Regex EdgeRegex();

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddStringArgPredicate("hasCrust", "crust", "thin")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
