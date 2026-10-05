namespace TruthWeaver.Tests;

using TruthWeaver.Parsing;

/// <summary>The path syntax that locates a JSON or YAML diagnostic: <c>$</c>, <c>.name</c>, <c>[n]</c> and <c>['key']</c>.</summary>
public sealed class TreePathTests
{
    /// <summary>A key made of letters, digits and underscores, starting with a letter or underscore, uses the dot form.</summary>
    [Theory]
    [InlineData("op", "$.op")]
    [InlineData("_private", "$._private")]
    [InlineData("a1_b2", "$.a1_b2")]
    public void Property_PlainKey_UsesTheDotForm_Test(string key, string expected)
    {
        Assert.Equal(expected, TreePath.Property(TreePath.Root, key));
    }

    /// <summary>A key that is empty, starts with a digit or holds another character uses the bracket form.</summary>
    [Theory]
    [InlineData("", "$['']")]
    [InlineData("1st", "$['1st']")]
    [InlineData("a-b", "$['a-b']")]
    [InlineData("a b", "$['a b']")]
    [InlineData("é", "$['é']")]
    public void Property_KeyThatIsNotAnIdentifier_UsesTheBracketForm_Test(string key, string expected)
    {
        Assert.Equal(expected, TreePath.Property(TreePath.Root, key));
    }

    /// <summary>A quote inside a bracketed key is escaped with a backslash so the path stays unambiguous.</summary>
    [Fact]
    public void Property_KeyWithAQuote_EscapesTheQuote_Test()
    {
        Assert.Equal("$['it\\'s']", TreePath.Property(TreePath.Root, "it's"));
    }

    /// <summary>A property path extends the path of its parent.</summary>
    [Fact]
    public void Property_NestedUnderAnIndex_ExtendsTheParentPath_Test()
    {
        Assert.Equal("$.operands[1].op", TreePath.Property(TreePath.Index("$.operands", 1), "op"));
    }

    /// <summary>An item path is the parent path with the 0-based index in brackets.</summary>
    [Fact]
    public void Index_ZeroBasedIndex_AppendsTheBracketedIndex_Test()
    {
        Assert.Equal("$.operands[0]", TreePath.Index("$.operands", 0));
    }
}
