namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;

/// <summary>
/// The per-node style callback and the chain compaction of <see cref="MermaidOptions"/>.
/// </summary>
public sealed class MermaidNodeStyleTests
{
    /// <summary>A callback that highlights an operator node assigns the highlight class and defines it from the palette.</summary>
    [Fact]
    public void Print_CallbackHighlightsAnOperator_AssignsAndDefinesTheHighlightClass_Test()
    {
        string mermaid = MermaidTreePrinter.Print(
            Tree(),
            new MermaidOptions { NodeStyle = node => node.Label == "OR" ? NodeStyle.Highlight : null }
        );

        Assert.Contains("class n2 brHighlight\n", mermaid);
        Assert.Contains($"classDef brHighlight {MermaidPalette.Light.Highlight};\n", mermaid);
        Assert.DoesNotContain("brMute", mermaid);
        Assert.DoesNotContain("brTrue", mermaid);
    }

    /// <summary>A callback that mutes a subtree assigns the mute class to every node in it and to no other node.</summary>
    [Fact]
    public void Print_CallbackMutesASubtree_AssignsTheMuteClassToEachNodeOfIt_Test()
    {
        OutlineNode root = Tree();
        OutlineNode subtree = root.Operands[1];
        HashSet<OutlineNode> muted = [subtree, .. subtree.Operands];

        string mermaid = MermaidTreePrinter.Print(
            root,
            new MermaidOptions { NodeStyle = node => muted.Contains(node) ? NodeStyle.Mute : null }
        );

        Assert.Contains("class n2 brMute\n", mermaid);
        Assert.Contains("class n3 brMute\n", mermaid);
        Assert.Contains("class n4 brMute\n", mermaid);
        Assert.DoesNotContain("class n0", mermaid);
        Assert.DoesNotContain("class n1", mermaid);
        Assert.Contains($"classDef brMute {MermaidPalette.Light.Mute};\n", mermaid);
    }

    /// <summary>On a node that evaluation colored, the callback style wins.</summary>
    [Fact]
    public void Print_CallbackAndDecisionColoringConflict_CallbackWins_Test()
    {
        OutlineNode root = new("a", "d", []);
        TraceNode trace = new("a", TruthValue.True, false, []);

        string mermaid = MermaidTreePrinter.Print(root, trace, new MermaidOptions { NodeStyle = _ => NodeStyle.Mute });

        Assert.Contains("class n0 brMute\n", mermaid);
        Assert.DoesNotContain("class n0 brTrue", mermaid);
    }

    /// <summary>A node that the callback skips keeps its evaluation color.</summary>
    [Fact]
    public void Print_CallbackReturnsNull_KeepsDecisionColoring_Test()
    {
        OutlineNode root = new("a", "d", []);
        TraceNode trace = new("a", TruthValue.True, false, []);

        string mermaid = MermaidTreePrinter.Print(root, trace, new MermaidOptions { NodeStyle = _ => null });

        Assert.Contains("class n0 brTrue\n", mermaid);
        Assert.DoesNotContain("brHighlight", mermaid);
    }

    /// <summary>A custom class name appears as a Mermaid class, and the printer does not define it.</summary>
    [Fact]
    public void Print_CallbackReturnsCustomClass_AssignsItWithoutDefiningIt_Test()
    {
        string mermaid = MermaidTreePrinter.Print(
            new OutlineNode("a", "d", []),
            new MermaidOptions { NodeStyle = _ => NodeStyle.Custom("review-me") }
        );

        Assert.Contains("class n0 review-me\n", mermaid);
        Assert.DoesNotContain("classDef", mermaid);
    }

    /// <summary>A custom class name cannot carry a character that starts another Mermaid statement.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("a;b")]
    [InlineData("a\nclick n0 call x()")]
    public void Custom_InvalidClassName_Throws_Test(string name)
    {
        Assert.Throws<ArgumentException>(() => NodeStyle.Custom(name));
    }

    /// <summary>Without a callback the output has no highlight or mute definition.</summary>
    [Fact]
    public void Print_NoCallback_WritesNoHighlightOrMuteDefinition_Test()
    {
        string mermaid = MermaidTreePrinter.Print(
            new OutlineNode("a", "d", []),
            new TraceNode("a", TruthValue.True, false, [])
        );

        Assert.DoesNotContain("brHighlight", mermaid);
        Assert.DoesNotContain("brMute", mermaid);
    }

    /// <summary>The default options have compaction off, so a long flat chain stays as it is.</summary>
    [Fact]
    public void Print_DefaultCompaction_IsOff_Test()
    {
        string mermaid = MermaidTreePrinter.Print(Chain("AND", 8), new MermaidOptions());

        Assert.Null(new MermaidOptions().CompactChainThreshold);
        Assert.DoesNotContain("subgraph", mermaid);
    }

    /// <summary>A flat chain of leaf terms above the threshold sits in a subgraph, and every operand stays.</summary>
    [Theory]
    [InlineData("AND")]
    [InlineData("OR")]
    public void Print_ChainAboveThreshold_WrapsInASubgraphWithEveryOperand_Test(string op)
    {
        string mermaid = MermaidTreePrinter.Print(Chain(op, 5), new MermaidOptions { CompactChainThreshold = 4 });

        Assert.Contains($"subgraph g0 [\"{op} (5 operands)\"]\n", mermaid);
        Assert.Equal(1, CountOf(mermaid, "    end\n"));
        for (int i = 1; i <= 5; i++)
        {
            Assert.Contains($"n0 --> n{i}\n", mermaid);
            Assert.Contains($"n{i}[\"t{i}\"]", mermaid);
        }

        Assert.True(mermaid.IndexOf("subgraph", StringComparison.Ordinal) < mermaid.IndexOf("n0[", StringComparison.Ordinal));
        Assert.True(mermaid.IndexOf("n5[", StringComparison.Ordinal) < mermaid.IndexOf("    end\n", StringComparison.Ordinal));
    }

    /// <summary>A chain with as many operands as the threshold stays unchanged.</summary>
    [Fact]
    public void Print_ChainAtThreshold_IsUnchanged_Test()
    {
        OutlineNode chain = Chain("AND", 4);

        Assert.Equal(
            MermaidTreePrinter.Print(chain, new MermaidOptions()),
            MermaidTreePrinter.Print(chain, new MermaidOptions { CompactChainThreshold = 4 })
        );
    }

    /// <summary>A chain that holds an operator operand is a mixed chain and stays unchanged.</summary>
    [Fact]
    public void Print_MixedChainAboveThreshold_IsUnchanged_Test()
    {
        OutlineNode mixed = new(
            "AND",
            "d",
            [Leaf("a"), Leaf("b"), new OutlineNode("NOT", "d", [Leaf("c")], Kind: OutlineNodeKind.Operator), Leaf("d")],
            Kind: OutlineNodeKind.Operator
        );

        Assert.DoesNotContain("subgraph", MermaidTreePrinter.Print(mixed, new MermaidOptions { CompactChainThreshold = 2 }));
    }

    /// <summary>Evaluation coloring still applies to the nodes inside the group.</summary>
    [Fact]
    public void Print_CompactedChainWithTrace_StillColorsTheNodesInside_Test()
    {
        TraceNode trace = new(
            "AND",
            TruthValue.False,
            false,
            [
                new("t1", TruthValue.True, false, []),
                new("t2", TruthValue.False, false, []),
                new("t3", TruthValue.True, true, []),
            ]
        );

        string mermaid = MermaidTreePrinter.Print(Chain("AND", 3), trace, new MermaidOptions { CompactChainThreshold = 2 });

        Assert.Contains("subgraph", mermaid);
        Assert.Contains("class n0 brFalse\n", mermaid);
        Assert.Contains("class n1 brTrue\n", mermaid);
        Assert.Contains("class n2 brFalse\n", mermaid);
        Assert.Contains("class n3 brSkipped\n", mermaid);
    }

    /// <summary>Compaction applies to a nested chain and leaves the parent edge outside the group.</summary>
    [Fact]
    public void Print_NestedChain_KeepsTheParentEdgeOutsideTheGroup_Test()
    {
        OutlineNode root = new("NOT", "d", [Chain("OR", 3)], Kind: OutlineNodeKind.Operator);

        string mermaid = MermaidTreePrinter.Print(root, new MermaidOptions { CompactChainThreshold = 2 });

        Assert.True(
            mermaid.IndexOf("n0 --> n1", StringComparison.Ordinal) > mermaid.IndexOf("    end\n", StringComparison.Ordinal)
        );
    }

    private static int CountOf(string text, string part)
    {
        return text.Split(part).Length - 1;
    }

    private static OutlineNode Leaf(string label)
    {
        return new OutlineNode(label, "d", [], Kind: OutlineNodeKind.Term);
    }

    private static OutlineNode Chain(string op, int count)
    {
        return new OutlineNode(
            op,
            "d",
            [.. Enumerable.Range(1, count).Select(i => Leaf($"t{i}"))],
            Kind: OutlineNodeKind.Operator
        );
    }

    /// <summary>AND(a, OR(b, c)); the node ids are n0 AND, n1 a, n2 OR, n3 b, n4 c.</summary>
    private static OutlineNode Tree()
    {
        return new OutlineNode(
            "AND",
            "d",
            [Leaf("a"), new OutlineNode("OR", "d", [Leaf("b"), Leaf("c")], Kind: OutlineNodeKind.Operator)],
            Kind: OutlineNodeKind.Operator
        );
    }
}
