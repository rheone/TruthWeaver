namespace TruthWeaver.Tool.Tui.Tests;

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// Runs the real Terminal.Gui application loop once, headless, with the ANSI driver: the driver needs no console window,
/// so the test sees the screen the author would see.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class TerminalRunTests
{
    /// <summary>One iteration of the application draws the regions, the rule and its canonical text, and the status line.</summary>
    [Fact]
    public void Run_OneIteration_DrawsTheRegionsAndTheRule_Test()
    {
        using IApplication app = Application.Create();
        app.Init(DriverRegistry.Names.ANSI);
        app.StopAfterFirstIteration = true;
        Workbench workbench = new(RuleDocument.Create("a   and b"), ToolSettings.Default, new ScriptedPrompts());
        using WorkbenchWindow window = new(workbench);

        app.Run(window);
        string screen = ScreenText(app);

        Assert.Contains("Rule", screen, StringComparison.Ordinal);
        Assert.Contains("Canonical", screen, StringComparison.Ordinal);
        Assert.Contains("Diagnostics", screen, StringComparison.Ordinal);
        Assert.Contains("a   and b", screen, StringComparison.Ordinal);
        Assert.Contains("a AND b", screen, StringComparison.Ordinal);
        Assert.Contains("F1 keys", screen, StringComparison.Ordinal);
    }

    private static string ScreenText(IApplication app)
    {
        Cell[,] cells = app.Driver!.Contents!;
        StringBuilder text = new();
        for (int row = 0; row < cells.GetLength(0); row++)
        {
            for (int column = 0; column < cells.GetLength(1); column++)
            {
                text.Append(cells[row, column].Grapheme);
            }

            text.Append('\n');
        }

        return text.ToString();
    }
}
