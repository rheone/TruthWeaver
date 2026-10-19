namespace TruthWeaver.Tool.Tui.Tests.Theme;

using TruthWeaver.Tool.Tui.Theme;

/// <summary>The syntax kind of each character of the rule text, which the editor colors, and the kind of a palette line.</summary>
public sealed class SyntaxMarksTests
{
    /// <summary>A predicate call, an operator word and a constant each get their own kind, and the arguments get none.</summary>
    [Fact]
    public void Compute_PredicateOperatorAndConstant_GetTheirOwnKinds_Test()
    {
        const string Text = "isAdult(age: 18) AND NOT True";

        SyntaxMarks marks = SyntaxMarks.Compute(Text);

        Assert.Equal(SyntaxKind.Predicate, marks.At(Text.IndexOf("isAdult", StringComparison.Ordinal)));
        Assert.Null(marks.At(Text.IndexOf("age", StringComparison.Ordinal)));
        Assert.Null(marks.At(Text.IndexOf("18", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Operator, marks.At(Text.IndexOf("AND", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Operator, marks.At(Text.IndexOf("NOT", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Constant, marks.At(Text.IndexOf("True", StringComparison.Ordinal)));
    }

    /// <summary>The symbolic operators get the operator kind, and a bare name that is not a keyword is a predicate.</summary>
    [Fact]
    public void Compute_SymbolicOperators_AreOperators_Test()
    {
        const string Text = "a && !b || c";

        SyntaxMarks marks = SyntaxMarks.Compute(Text);

        Assert.Equal(SyntaxKind.Predicate, marks.At(0));
        Assert.Equal(SyntaxKind.Operator, marks.At(2));
        Assert.Equal(SyntaxKind.Operator, marks.At(3));
        Assert.Equal(SyntaxKind.Operator, marks.At(5));
        Assert.Equal(SyntaxKind.Predicate, marks.At(6));
        Assert.Equal(SyntaxKind.Operator, marks.At(8));
        Assert.Equal(SyntaxKind.Operator, marks.At(9));
    }

    /// <summary>A <c>from(...)</c> read is a variable, and the text of its strings has no kind.</summary>
    [Fact]
    public void Compute_FromCall_IsAVariable_Test()
    {
        const string Text = "isSet(value: from(\"user\", \"$.role\"))";

        SyntaxMarks marks = SyntaxMarks.Compute(Text);

        Assert.Equal(SyntaxKind.Predicate, marks.At(0));
        Assert.Null(marks.At(Text.IndexOf("value", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Variable, marks.At(Text.IndexOf("from", StringComparison.Ordinal)));
        Assert.Null(marks.At(Text.IndexOf("user", StringComparison.Ordinal)));
        Assert.Null(marks.At(Text.IndexOf("role", StringComparison.Ordinal)));
    }

    /// <summary>The parameters of a definition and the variable of a quantifier are variables, in the head and in every use.</summary>
    [Fact]
    public void Compute_DefinitionParametersAndQuantifierVariable_AreVariables_Test()
    {
        const string Text = "let both(x, y) = x AND y; FORALL(item IN items, isOk(item))";

        SyntaxMarks marks = SyntaxMarks.Compute(Text);

        Assert.Equal(SyntaxKind.Predicate, marks.At(Text.IndexOf("both", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Variable, marks.At(Text.IndexOf("x,", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Variable, marks.At(Text.IndexOf("y)", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Variable, marks.At(Text.IndexOf("x AND", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Operator, marks.At(Text.IndexOf("let", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Variable, marks.At(Text.IndexOf("item IN", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Operator, marks.At(Text.IndexOf("IN items", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Variable, marks.At(Text.IndexOf("item))", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Predicate, marks.At(Text.IndexOf("isOk", StringComparison.Ordinal)));
    }

    /// <summary>Both kinds of comment get the description kind, and a word inside a comment does not read as a predicate.</summary>
    [Fact]
    public void Compute_Comments_AreDescriptions_Test()
    {
        const string Text = "a // AND b\n/* c */ d";

        SyntaxMarks marks = SyntaxMarks.Compute(Text);

        Assert.Equal(SyntaxKind.Predicate, marks.At(0));
        Assert.All(Enumerable.Range(2, 8), i => Assert.Equal(SyntaxKind.Description, marks.At(i)));
        Assert.Null(marks.At(10));
        Assert.Equal(SyntaxKind.Description, marks.At(Text.IndexOf("/* c */", StringComparison.Ordinal)));
        Assert.Equal(SyntaxKind.Predicate, marks.At(Text.Length - 1));
    }

    /// <summary>A rule that does not compile is still classified, so an error does not turn the coloring off.</summary>
    [Theory]
    [InlineData("a AND (b")]
    [InlineData("a AND \"open")]
    [InlineData("/* open")]
    [InlineData("(((")]
    [InlineData("")]
    public void Compute_TextWithErrors_DoesNotThrowAndKeepsWhatItCanRead_Test(string text)
    {
        SyntaxMarks marks = SyntaxMarks.Compute(text);

        Assert.Null(marks.At(text.Length));
        Assert.Null(marks.At(-1));
        if (text.StartsWith("a AND", StringComparison.Ordinal))
        {
            Assert.Equal(SyntaxKind.Operator, marks.At(2));
        }
    }

    /// <summary>The marks of a text of 20000 characters come quickly, so the classification after a pause does not stall the editor.</summary>
    [Fact]
    public void Compute_LargeText_FinishesInUnderASecond_Test()
    {
        string text = string.Concat(Enumerable.Repeat("isAdult(age: 18) AND NOT True OR ", 600));
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        SyntaxMarks marks = SyntaxMarks.Compute(text);

        watch.Stop();
        Assert.Equal(SyntaxKind.Predicate, marks.At(0));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), watch.Elapsed.ToString());
    }

    /// <summary>The first word of a palette line names its kind; a group line has none.</summary>
    /// <param name="line">The line.</param>
    /// <param name="expected">The name of the kind, or <see langword="null"/>.</param>
    [Theory]
    [InlineData("operator and(left, right)", "Operator")]
    [InlineData("constant True", "Constant")]
    [InlineData("catalog  isEven(value: Int64)", "Predicate")]
    [InlineData("schema   isAdult(age: Int64)", "Predicate")]
    [InlineData("rule     isOk", "Predicate")]
    [InlineData("let      both(x, y)", "Predicate")]
    [InlineData("--- Numbers ---", null)]
    [InlineData("Type an expression", null)]
    public void OfPaletteLine_Line_GivesTheKindOfItsSource_Test(string line, string? expected)
    {
        Assert.Equal(expected, SyntaxKinds.OfPaletteLine(line)?.ToString());
    }

    /// <summary>The legend names every kind once, in lower case.</summary>
    [Fact]
    public void LegendText_EveryKind_IsNamedInTheLegend_Test()
    {
        Assert.Equal("predicate  operator  variable  constant  description", SyntaxKinds.LegendText());
    }
}
