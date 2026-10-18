namespace TruthWeaver.Tool.Tui.Tests;

using System.Collections.Concurrent;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// Live diagnostics: the editor text compiles after a pause, on a background task, and a result is dropped when newer text
/// exists. A test replaces the thread hop of the terminal loop with a queue it drains by hand.
/// </summary>
public sealed class DebouncedDiagnosticsTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);
    private readonly BlockingCollection<Action> posted = [];

    /// <summary>The document keeps its text and the status line says checking until the background compile is applied.</summary>
    [Fact]
    public void EditText_WithPause_CompilesInTheBackgroundAndShowsChecking_Test()
    {
        Workbench workbench = this.Create("a");

        workbench.EditText("a AND b");

        Assert.True(workbench.IsChecking);
        Assert.Equal("a", workbench.CommittedDocument.Text);
        Assert.Contains("checking", WorkbenchWindow.StatusOf(workbench), StringComparison.Ordinal);

        this.RunNextPosted();

        Assert.False(workbench.IsChecking);
        Assert.Equal("a AND b", workbench.CommittedDocument.Text);
        Assert.Equal("a AND b", workbench.CommittedDocument.Rule!.CanonicalText);
        Assert.DoesNotContain("checking", WorkbenchWindow.StatusOf(workbench), StringComparison.Ordinal);
    }

    /// <summary>A result of older text never replaces newer text.</summary>
    [Fact]
    public void EditText_NewerEditBeforeTheOlderResultArrives_DropsTheOlderResult_Test()
    {
        Workbench workbench = this.Create("a");
        workbench.EditText("a AND");
        Action older = this.TakePosted();

        workbench.EditText("a AND b");
        older();

        Assert.True(workbench.IsChecking);
        Assert.Equal("a", workbench.CommittedDocument.Text);

        this.RunNextPosted();

        Assert.Equal("a AND b", workbench.CommittedDocument.Text);
        Assert.True(workbench.CommittedDocument.IsRuleCurrent);
    }

    /// <summary>An action that reads the document applies the typed text first, so it never works on replaced text.</summary>
    [Fact]
    public void Document_WhileATextWaits_AppliesTheTypedTextAtOnce_Test()
    {
        Workbench workbench = this.Create("a");
        workbench.EditText("a OR b");

        string text = workbench.Document.Text;

        Assert.Equal("a OR b", text);
        Assert.False(workbench.IsChecking);
        Assert.Equal("a OR b", workbench.CommittedDocument.Rule!.CanonicalText);
    }

    /// <summary>A rule with thousands of nodes does not compile on the calling thread; the result arrives later.</summary>
    [Fact]
    public void EditText_RuleWithThousandsOfNodes_ReturnsBeforeTheCompileEnds_Test()
    {
        Workbench workbench = this.Create("a");
        string large = string.Join(" AND ", Enumerable.Range(0, 3000).Select(i => $"t{i}"));

        workbench.EditText(large);

        Assert.Equal("a", workbench.CommittedDocument.Text);
        Assert.True(workbench.IsChecking);

        this.RunNextPosted();

        Assert.Equal(large, workbench.CommittedDocument.Text);
        Assert.False(workbench.IsChecking);
        Assert.NotEmpty(workbench.CommittedDocument.Rule!.CanonicalText);
    }

    /// <summary>With no pause the edit compiles at once on the calling thread, as before.</summary>
    [Fact]
    public void EditText_WithNoPause_CompilesAtOnce_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        workbench.EditText("a AND b");

        Assert.Equal("a AND b", workbench.CommittedDocument.Rule!.CanonicalText);
        Assert.False(workbench.IsChecking);
    }

    /// <summary>Typing in the editor leaves the document alone until the pause ends, and the status line says checking.</summary>
    [Fact]
    public void Typing_WithPause_LeavesTheDocumentUntilTheCompileIsApplied_Test()
    {
        using WorkbenchHarness harness = new("a");
        harness.Workbench.DiagnosticsDelay = TimeSpan.FromMilliseconds(5);
        harness.Workbench.Post = this.posted.Add;

        harness.Type(" and b");

        Assert.Equal("a", harness.Workbench.CommittedDocument.Text);
        Assert.Contains("checking", harness.Window.StatusText, StringComparison.Ordinal);

        // Each key restarts the pause, so only the result of the last edit comes back.
        this.RunNextPosted();

        Assert.Contains("and b", harness.Workbench.CommittedDocument.Text, StringComparison.Ordinal);
        Assert.False(harness.Workbench.IsChecking);
        Assert.DoesNotContain("checking", harness.Window.StatusText, StringComparison.Ordinal);
    }

    private Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts())
        {
            DiagnosticsDelay = TimeSpan.FromMilliseconds(5),
            Post = this.posted.Add,
        };
    }

    // Waits for the background compile to post its result, then runs it as the terminal loop would.
    private void RunNextPosted()
    {
        this.TakePosted()();
    }

    private Action TakePosted()
    {
        using CancellationTokenSource timeout = new(Wait);
        return this.posted.Take(timeout.Token);
    }
}
