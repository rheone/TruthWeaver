namespace TruthWeaver.Tool.Core.Tests;

/// <summary>Compiling text off the document (<c>Check</c>) and applying the result without a second compile.</summary>
public sealed class RuleDocumentCheckTests
{
    /// <summary>A check compiles the text and leaves the document as it was.</summary>
    [Fact]
    public void Check_ValidText_ReturnsTheRuleAndDoesNotChangeTheDocument_Test()
    {
        RuleDocument document = RuleDocument.Create("a");

        CheckedText result = document.Check("a AND b", document.Format);

        Assert.NotNull(result.Result.Rule);
        Assert.Equal("a", document.Text);
        Assert.False(document.CanUndo);
    }

    /// <summary>Applying a checked text gives the same state as <c>SetText(string)</c>, as one undo step.</summary>
    [Fact]
    public void SetText_CheckedText_MatchesSetTextOfTheString_Test()
    {
        RuleDocument viaCheck = RuleDocument.Create("a");
        RuleDocument direct = RuleDocument.Create("a");

        viaCheck.SetText(viaCheck.Check("a AND", viaCheck.Format));
        direct.SetText("a AND");

        Assert.Equal(direct.Text, viaCheck.Text);
        Assert.Equal(direct.Diagnostics, viaCheck.Diagnostics);
        Assert.Equal(direct.IsRuleCurrent, viaCheck.IsRuleCurrent);
        Assert.Equal("a", viaCheck.Rule!.CanonicalText);
        Assert.True(viaCheck.Undo());
        Assert.Equal("a", viaCheck.Text);
    }
}
