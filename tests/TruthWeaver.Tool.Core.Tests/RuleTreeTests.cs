namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;

/// <summary>The node list of a rule tree and the text of a node.</summary>
public sealed class RuleTreeTests
{
    /// <summary>The nodes come in tree order with their paths, depths, kinds and labels.</summary>
    [Fact]
    public void Nodes_RuleWithOperatorTermAndConstant_ListsEachNodeInTreeOrder_Test()
    {
        RuleTree tree = RuleTree.Of(Compile("a AND NOT hasRole(role: \"admin\") AND True"));

        Assert.Equal(
            [
                ("$", 0, RuleTreeNodeKind.Operator, "and"),
                ("$.operands[0]", 1, RuleTreeNodeKind.Term, "a"),
                ("$.operands[1]", 1, RuleTreeNodeKind.Operator, "not"),
                ("$.operands[1].operands[0]", 2, RuleTreeNodeKind.Term, "hasRole(role: \"admin\")"),
                ("$.operands[2]", 1, RuleTreeNodeKind.Constant, "True"),
            ],
            tree.Nodes.Select(n => (n.Path, n.Depth, n.Kind, n.Label))
        );
    }

    /// <summary>A term lists its arguments as literals, as JSON.</summary>
    [Fact]
    public void Find_Term_ListsItsArgumentsAsLiterals_Test()
    {
        RuleTree tree = RuleTree.Of(Compile("hasRole(role: \"admin\")"));

        RuleTreeNode? node = tree.Find("$");

        Assert.NotNull(node);
        Assert.Equal([new KeyValuePair<string, string>("role", "\"admin\"")], node.Literals);
    }

    /// <summary>The text of a node is rule text that compiles back to the same subtree.</summary>
    [Fact]
    public void TextOf_Operand_GivesRuleTextOfTheSubtree_Test()
    {
        RuleTree tree = RuleTree.Of(Compile("x AND (a OR b)"));

        ToolResult<string> text = tree.TextOf("$.operands[1]");

        Assert.True(text.Succeeded);
        Assert.Equal(Compile("a OR b").PrintJson(includeNotes: false), Compile(text.Value).PrintJson(includeNotes: false));
    }

    /// <summary>A path that names no node gives a diagnostic and no text.</summary>
    [Fact]
    public void TextOf_PathThatNamesNoNode_GivesADiagnostic_Test()
    {
        RuleTree tree = RuleTree.Of(Compile("a AND b"));

        ToolResult<string> text = tree.TextOf("$.operands[9]");

        Assert.False(text.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.PathNotFound, Assert.Single(text.Diagnostics).Code);
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        CompilationResult<StubAssignment> result = RuleConverter.CreateCompiler().CompileAs(text, RuleFormat.Text);
        Assert.NotNull(result.Rule);
        return result.Rule;
    }
}
