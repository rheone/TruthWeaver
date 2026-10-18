namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>The text that the canonical pane, the diagnostics pane, the status line and the key help show.</summary>
public sealed class PaneTextTests
{
    /// <summary>A document whose text never compiled has no tree to show.</summary>
    [Fact]
    public void CanonicalTextOf_NoValidTree_SaysSo_Test()
    {
        Workbench workbench = Create("AND AND");

        Assert.Equal(CanonicalTextPane.NoTreeNotice, CanonicalTextPane.TextOf(workbench));
    }

    /// <summary>The diagnostics pane shows each diagnostic with its severity, code, line and column.</summary>
    [Fact]
    public void DiagnosticLinesOf_SyntaxError_ShowsSeverityCodeAndLocation_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND\n(b");

        IReadOnlyList<string> lines = DiagnosticsPane.LinesOf(document.Text, document.Diagnostics);

        Assert.Matches(@"^error TRE\d{4} \d+:\d+: ", lines[0]);
    }

    /// <summary>The status line names the file, the format, unsaved changes and the counts of errors and warnings.</summary>
    [Fact]
    public void StatusOf_EditedNewDocument_ShowsNameFormatAndCounts_Test()
    {
        Workbench workbench = Create("a");
        workbench.Document.SetText("a AND");

        string status = WorkbenchWindow.StatusOf(workbench);

        Assert.StartsWith("(new rule) [rule text] * | 1 error, 0 warnings", status, StringComparison.Ordinal);
        Assert.EndsWith("F1 keys", status, StringComparison.Ordinal);
    }

    /// <summary>The key help lists every command of the workbench with its key.</summary>
    [Fact]
    public void HelpText_DefaultCommands_ListsEveryKey_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        string[] lines = help.Split('\n');
        Assert.Equal(WorkbenchCommands.Default.Count, lines.Length);
        Assert.Contains(
            lines,
            line => line.StartsWith("Ctrl+S", StringComparison.Ordinal) && line.EndsWith("Save", StringComparison.Ordinal)
        );
    }

    /// <summary>No two commands share a key.</summary>
    [Fact]
    public void WorkbenchCommands_Default_HaveDistinctKeys_Test()
    {
        Assert.Equal(
            WorkbenchCommands.Default.Count,
            WorkbenchCommands.Default.Select(c => c.Key.ToString()).Distinct(StringComparer.Ordinal).Count()
        );
    }

    private static Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
    }
}
