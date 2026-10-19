namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

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
    }

    /// <summary>
    /// The key reference lists every command of the workbench with its key, under the name of its menu, then one line about
    /// the mouse.
    /// </summary>
    [Fact]
    public void HelpText_DefaultCommands_ListsEveryKeyUnderItsMenu_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        string[] lines = help.Split('\n');
        int file = Array.IndexOf(lines, "File");
        int edit = Array.IndexOf(lines, "Edit");
        int save = Array.FindIndex(
            lines,
            line =>
                line.TrimStart().StartsWith("Ctrl+S", StringComparison.Ordinal)
                && line.EndsWith("Save", StringComparison.Ordinal)
        );
        Assert.Equal(WorkbenchCommands.Default.Count, lines.Count(line => line.StartsWith("  ", StringComparison.Ordinal)));
        Assert.InRange(save, file + 1, edit - 1);
        Assert.StartsWith("Mouse:", lines[^1], StringComparison.Ordinal);
    }

    /// <summary>The hint line shows the most common keys, the menu key first.</summary>
    [Fact]
    public void HintText_DefaultCommands_ShowsTheCommonKeys_Test()
    {
        string hint = WorkbenchWindow.HintText(WorkbenchCommands.Default);

        Assert.StartsWith("F1 Menus", hint, StringComparison.Ordinal);
        Assert.Contains("Ctrl+O Open", hint, StringComparison.Ordinal);
        Assert.Contains("Ctrl+S Save", hint, StringComparison.Ordinal);
        Assert.Contains("F7 Validate", hint, StringComparison.Ordinal);
        Assert.Contains("Ctrl+Q Quit", hint, StringComparison.Ordinal);
    }

    /// <summary>No two commands share a key, and no command takes the key that opens the menu bar.</summary>
    [Fact]
    public void WorkbenchCommands_Default_HaveDistinctKeys_Test()
    {
        string[] keys = [.. WorkbenchCommands.Default.Where(c => c.Key != Key.Empty).Select(c => c.Key.ToString())];

        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(WorkbenchMenus.MenuBarKey.ToString(), keys);
    }

    private static Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
    }
}
