namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;

public sealed class RuleDocumentConversionTests
{
    private const string WithNotes = "/* Active people. */ (/* First. */ isActive AND hasRole(role: \"editor\"))";

    /// <summary>A conversion from every format to every other keeps the compiled tree and the notes.</summary>
    [Theory]
    [InlineData(RuleFormat.Text, RuleFormat.Json)]
    [InlineData(RuleFormat.Text, RuleFormat.Yaml)]
    [InlineData(RuleFormat.Json, RuleFormat.Yaml)]
    [InlineData(RuleFormat.Json, RuleFormat.Text)]
    [InlineData(RuleFormat.Yaml, RuleFormat.Text)]
    [InlineData(RuleFormat.Yaml, RuleFormat.Json)]
    public void ConvertTo_AnyFormatPair_KeepsTheTreeAndTheNotes_Test(RuleFormat from, RuleFormat to)
    {
        RuleDocument source = RuleDocument.Create(WithNotes);
        source.ConvertTo(from);
        string before = source.Rule!.PrintJson(includeNotes: true);

        ToolResult<string> result = source.ConvertTo(to);

        Assert.True(result.Succeeded);
        Assert.Equal(to, source.Format);
        Assert.Equal(before, source.Rule.PrintJson(includeNotes: true));
        Assert.Contains("Active people.", source.Text, StringComparison.Ordinal);
    }

    /// <summary>A conversion is one undo step: undo brings back the old text and format.</summary>
    [Fact]
    public void ConvertTo_AfterConversion_UndoRestoresTheOldTextAndFormat_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");

        document.ConvertTo(RuleFormat.Json);
        document.Undo();

        Assert.Equal("a AND b", document.Text);
        Assert.Equal(RuleFormat.Text, document.Format);
    }

    /// <summary>Text that does not compile is not converted, and the text stays.</summary>
    [Fact]
    public void ConvertTo_TextWithErrors_ChangesNothing_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");
        document.SetText("a AND");

        ToolResult<string> result = document.ConvertTo(RuleFormat.Yaml);

        Assert.False(result.Succeeded);
        Assert.Equal("a AND", document.Text);
        Assert.Equal(RuleFormat.Text, document.Format);
    }

    /// <summary>Converting to the format the document already has changes nothing and adds no undo step.</summary>
    [Fact]
    public void ConvertTo_SameFormat_ChangesNothing_Test()
    {
        RuleDocument document = RuleDocument.Create("a   AND b");

        ToolResult<string> result = document.ConvertTo(RuleFormat.Text);

        Assert.True(result.Succeeded);
        Assert.Equal("a   AND b", document.Text);
        Assert.False(document.CanUndo);
    }

    /// <summary>A format that is not defined is refused.</summary>
    [Fact]
    public void ConvertTo_UndefinedFormat_Throws_Test()
    {
        RuleDocument document = RuleDocument.Create("a");

        Assert.Throws<ArgumentOutOfRangeException>(() => document.ConvertTo((RuleFormat)99));
    }

    /// <summary>MarkSaved makes the current text the saved text, so the document is no longer dirty.</summary>
    [Fact]
    public void MarkSaved_EditedDocument_ClearsTheUnsavedState_Test()
    {
        RuleDocument document = RuleDocument.Create("a");
        document.SetText("a AND b");
        int changes = 0;
        document.Changed += (_, _) => changes++;

        document.MarkSaved();

        Assert.False(document.IsDirty);
        Assert.Equal(1, changes);
        document.SetText("a AND b AND c");
        Assert.True(document.IsDirty);
    }
}
