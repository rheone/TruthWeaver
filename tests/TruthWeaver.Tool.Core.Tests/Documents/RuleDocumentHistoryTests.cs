namespace TruthWeaver.Tool.Core.Tests.Documents;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Tree;

/// <summary>The undo limit, the format-aware edits and the refusals of <see cref="RuleDocument"/>.</summary>
public sealed class RuleDocumentHistoryTests
{
    /// <summary>The history keeps at most <see cref="RuleDocument.UndoLimit"/> steps; the oldest step is dropped first.</summary>
    [Fact]
    public void SetText_MoreEditsThanTheUndoLimit_KeepsOnlyTheNewestSteps_Test()
    {
        RuleDocument document = RuleDocument.Create("t0");
        const int Edits = RuleDocument.UndoLimit + 5;
        for (int i = 1; i <= Edits; i++)
        {
            document.SetText("t" + i);
        }

        int undone = 0;
        while (document.Undo())
        {
            undone++;
        }

        Assert.Equal(RuleDocument.UndoLimit, undone);
        Assert.Equal("t5", document.Text);
    }

    /// <summary>Setting text and format together is one undo step that gives both back.</summary>
    [Fact]
    public void SetText_WithFormat_UndoRestoresTheTextAndTheFormat_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");

        document.SetText("""{"predicate":"a"}""", RuleFormat.Json);
        Assert.Equal(RuleFormat.Json, document.Format);
        Assert.True(document.IsRuleCurrent);
        document.Undo();

        Assert.Equal("a AND b", document.Text);
        Assert.Equal(RuleFormat.Text, document.Format);
    }

    /// <summary>Setting a format that is not defined throws and leaves the document as it was.</summary>
    [Fact]
    public void SetText_UndefinedFormat_ThrowsAndChangesNothing_Test()
    {
        RuleDocument document = RuleDocument.Create("a");

        Assert.Throws<ArgumentOutOfRangeException>(() => document.SetText("b", (RuleFormat)99));

        Assert.Equal("a", document.Text);
        Assert.False(document.CanUndo);
    }

    /// <summary>Checked text from another format is compiled again in the format of the document, not trusted.</summary>
    [Fact]
    public void SetText_CheckedTextOfAnotherFormat_CompilesAgainInTheDocumentFormat_Test()
    {
        RuleDocument document = RuleDocument.Create("a");
        CheckedText json = document.Check("""{"predicate":"a"}""", RuleFormat.Json);

        document.SetText(json);

        Assert.Equal(RuleFormat.Text, document.Format);
        Assert.False(document.IsRuleCurrent);
        Assert.NotEmpty(document.Diagnostics);
    }

    /// <summary>Saving a document that has no file and gets no path is a usage error.</summary>
    [Fact]
    public void Save_NoPathAndNoFile_Throws_Test()
    {
        RuleDocument document = RuleDocument.Create("a");

        Assert.Throws<InvalidOperationException>(() => document.Save());
    }

    /// <summary>An edit that was refused has no rule, so putting it in is an argument error.</summary>
    [Fact]
    public void ApplyEdit_RefusedEdit_Throws_Test()
    {
        RuleDocument document = RuleDocument.Create("a");

        Assert.Throws<ArgumentException>(() => document.ApplyEdit(RuleTreeEdit.Refused("no")));
        Assert.Equal("a", document.Text);
    }
}
