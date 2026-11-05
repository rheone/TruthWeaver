namespace TruthWeaver.Tool.Core.Tests.Documents;

using TruthWeaver.Tool.Core.Documents;

/// <summary>The union path grammar that <see cref="DocumentPath"/> reads and the <c>TreePath</c> form that it writes.</summary>
public sealed class DocumentPathTests
{
    /// <summary>
    /// Every accepted spelling parses to the keys and indexes it names, and <see cref="DocumentPath.Text"/> writes the
    /// <c>TreePath</c> form: <c>.name</c> for letters, digits and <c>_</c>, and <c>['name']</c> with <c>\'</c> for the rest.
    /// A key shows as <c>'name'</c> and an index as <c>#n</c> in the expected segments.
    /// </summary>
    [Theory]
    [InlineData("$", "$", "")]
    [InlineData("$.a", "$.a", "'a'")]
    [InlineData("a.b", "$.a.b", "'a','b'")]
    [InlineData("$.rules_1.admin", "$.rules_1.admin", "'rules_1','admin'")]
    [InlineData("$.a-b", "$['a-b']", "'a-b'")]
    [InlineData("$['a-b']", "$['a-b']", "'a-b'")]
    [InlineData("$[\"a\"]", "$.a", "'a'")]
    [InlineData("$[\"a b\"]", "$['a b']", "'a b'")]
    [InlineData("$['it\\'s']", "$['it\\'s']", "'it's'")]
    [InlineData("$[\"it\\'s\"]", "$['it\\'s']", "'it's'")]
    [InlineData("$['a]b']", "$['a]b']", "'a]b'")]
    [InlineData("$[\"a]b\"].c", "$['a]b'].c", "'a]b','c'")]
    [InlineData("$['a.b'][0]", "$['a.b'][0]", "'a.b',#0")]
    [InlineData("$['']", "$['']", "''")]
    [InlineData("$.operands[1].op", "$.operands[1].op", "'operands',#1,'op'")]
    [InlineData("$[ 3 ]", "$[3]", "#3")]
    public void TryParse_ValidPath_ReadsSegmentsAndWritesTheTreePathForm_Test(string input, string normalized, string segments)
    {
        bool parsed = DocumentPath.TryParse(input, out DocumentPath? path, out string? problem);

        Assert.True(parsed, problem);
        Assert.NotNull(path);
        Assert.Equal(normalized, path.Text);
        Assert.Equal(segments, string.Join(',', path.Segments.Select(Describe)));
    }

    /// <summary>A path that <c>TreePath</c> writes parses back to the same text, so the two grammars agree.</summary>
    [Theory]
    [InlineData("$")]
    [InlineData("$.rules.admin")]
    [InlineData("$.operands[1].op")]
    [InlineData("$['a-b']")]
    [InlineData("$['a b'].c")]
    [InlineData("$['it\\'s']")]
    [InlineData("$['a]b'][2]")]
    [InlineData("$['a.b']")]
    [InlineData("$['1a']")]
    [InlineData("$['']")]
    public void TryParse_TreePathOutput_RoundTripsToTheSameText_Test(string treePath)
    {
        bool parsed = DocumentPath.TryParse(treePath, out DocumentPath? path, out string? problem);

        Assert.True(parsed, problem);
        Assert.NotNull(path);
        Assert.Equal(treePath, path.Text);
    }

    /// <summary>A path that breaks the grammar is rejected with a problem text and no path.</summary>
    [Theory]
    [InlineData("$.")]
    [InlineData("$[")]
    [InlineData("$['a")]
    [InlineData("$['a]")]
    [InlineData("$[x]")]
    [InlineData("$[-1]")]
    [InlineData("$.1a")]
    [InlineData("$['a']x")]
    [InlineData("$['a'b']")]
    public void TryParse_BadPath_IsRejectedWithAProblem_Test(string input)
    {
        bool parsed = DocumentPath.TryParse(input, out DocumentPath? path, out string? problem);

        Assert.False(parsed);
        Assert.Null(path);
        Assert.False(string.IsNullOrEmpty(problem));
    }

    private static string Describe(DocumentPath.Segment segment)
    {
        return segment.Name is null ? $"#{segment.Position}" : $"'{segment.Name}'";
    }
}
