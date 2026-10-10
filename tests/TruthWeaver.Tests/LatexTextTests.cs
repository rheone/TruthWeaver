namespace TruthWeaver.Tests;

using TruthWeaver.Printing;

/// <summary>The LaTeX text escaping that the equation printer applies to predicate names and argument values.</summary>
public sealed class LatexTextTests
{
    private const string Specials = "_%&#{}\\^~$";

    /// <summary>Raw mode escapes every LaTeX special character inside one <c>\text</c> group.</summary>
    [Fact]
    public void Text_RawModeWithEverySpecialCharacter_EscapesEachOne_Test()
    {
        string text = LatexText.Text(Specials, mathJaxSafe: false);

        Assert.Equal(@"\text{\_\%\&\#\{\}\textbackslash{}\textasciicircum{}\textasciitilde{}\$}", text);
    }

    /// <summary>MathJax-safe mode leaves the text group for each special character and uses a math command.</summary>
    [Fact]
    public void Text_MathJaxSafeModeWithEverySpecialCharacter_UsesOnlyMathCommands_Test()
    {
        string text = LatexText.Text(Specials, mathJaxSafe: true);

        Assert.Equal(@"\_\%\&\#\{\}\backslash\hat{\ }\sim\$", text);
    }

    /// <summary>MathJax-safe mode keeps the plain runs of text in <c>\text</c> groups around an escaped character.</summary>
    [Fact]
    public void Text_MathJaxSafeModeWithUnderscore_SplitsTheTextGroup_Test()
    {
        string text = LatexText.Text("has_crust", mathJaxSafe: true);

        Assert.Equal(@"\text{has}\_\text{crust}", text);
    }

    /// <summary>MathJax-safe mode replaces a backtick, which would end the code span around the math.</summary>
    [Fact]
    public void Text_MathJaxSafeModeWithBacktick_UsesTheUnicodeCommand_Test()
    {
        string text = LatexText.Text("a`b", mathJaxSafe: true);

        Assert.Equal(@"\text{a}\unicode{x60}\text{b}", text);
    }

    /// <summary>Text with no special character is one <c>\text</c> group in both modes.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Text_PlainText_IsOneTextGroup_Test(bool mathJaxSafe)
    {
        Assert.Equal(@"\text{crust: ""thin""}", LatexText.Text("crust: \"thin\"", mathJaxSafe));
    }

    /// <summary>Empty text is an empty <c>\text</c> group, so the output stays valid LaTeX.</summary>
    [Fact]
    public void Text_EmptyText_IsAnEmptyTextGroup_Test()
    {
        Assert.Equal(@"\text{}", LatexText.Text(string.Empty, mathJaxSafe: true));
    }
}
