namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The cycling, the refused overrides, the file extension and the failed save of the view pane.</summary>
public sealed class ViewControllerSettingsTests
{
    /// <summary>Cycling the kind backwards from the first kind wraps to the last kind, and each cycle raises the change event once.</summary>
    [Fact]
    public void CycleKind_BackwardsFromTheFirstKind_WrapsToTheLastAndRaisesOnce_Test()
    {
        (ViewController controller, _, _) = Create("a");
        ViewKind[] kinds = Enum.GetValues<ViewKind>();
        controller.Settings.Kind = kinds[0];
        int raised = 0;
        controller.SettingsChanged += (_, _) => raised++;

        controller.CycleKind(-1);

        Assert.Equal(kinds[^1], controller.Settings.Kind);
        Assert.Equal(1, raised);
    }

    /// <summary>Cycling the operator style walks the default and the three styles, then returns to the default.</summary>
    [Fact]
    public void CycleOperatorStyle_FourTimes_ReturnsToTheDefault_Test()
    {
        (ViewController controller, _, _) = Create("a");

        controller.CycleOperatorStyle();
        Assert.Equal(OperatorStyle.Word, controller.Settings.OperatorStyle);
        for (int i = 0; i < 3; i++)
        {
            controller.CycleOperatorStyle();
        }

        Assert.Null(controller.Settings.OperatorStyle);
    }

    /// <summary>Each view kind saves with its own file extension.</summary>
    [Fact]
    public void Extension_ViewKind_GivesTheFileExtension_Test()
    {
        (ViewController controller, _, _) = Create("a");
        (ViewKind Kind, string Extension)[] expected =
        [
            (ViewKind.Json, ".json"),
            (ViewKind.Yaml, ".yaml"),
            (ViewKind.Mermaid, ".mmd"),
            (ViewKind.Dot, ".dot"),
            (ViewKind.Html, ".html"),
            (ViewKind.RuleText, ".rule"),
            (ViewKind.Tree, ".txt"),
        ];

        foreach ((ViewKind kind, string extension) in expected)
        {
            controller.Settings.Kind = kind;
            Assert.Equal(extension, controller.Extension);
        }
    }

    /// <summary>The equation dialects save as LaTeX, MathML, Typst or plain text.</summary>
    [Theory]
    [InlineData(EquationDialect.LaTeX, ".tex")]
    [InlineData(EquationDialect.MathML, ".mml")]
    [InlineData(EquationDialect.Typst, ".typ")]
    [InlineData(EquationDialect.Unicode, ".txt")]
    public void Extension_EquationDialect_GivesTheFileExtension_Test(EquationDialect dialect, string extension)
    {
        (ViewController controller, _, _) = Create("a");
        controller.Settings.Kind = ViewKind.Equation;
        controller.Settings.Dialect = dialect;

        Assert.Equal(extension, controller.Extension);
    }

    /// <summary>A save to a path that cannot be written says so and does not claim success.</summary>
    [Fact]
    public void Save_PathIsADirectory_ReportsTheFailure_Test()
    {
        (ViewController controller, _, Workbench workbench) = Create("a AND b");
        string directory = Path.Combine(Path.GetTempPath(), "truthweaver-view-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        ((ScriptedPrompts)workbench.Prompts).SavePath = directory;
        try
        {
            bool saved = controller.Save();

            Assert.False(saved);
            Assert.DoesNotContain("Saved the", workbench.Status, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    /// <summary>Cycling the dialect from another view first switches to the equation view and keeps the dialect.</summary>
    [Fact]
    public void CycleDialect_FromTheTreeView_SwitchesToTheEquationViewFirst_Test()
    {
        (ViewController controller, _, _) = Create("a");
        EquationDialect before = controller.Settings.Dialect;

        controller.CycleDialect();

        Assert.Equal(ViewKind.Equation, controller.Settings.Kind);
        Assert.Equal(before, controller.Settings.Dialect);
    }

    /// <summary>The header of a tree view says the decision is "none yet" before there is an evaluation, and "off" when it is hidden.</summary>
    [Fact]
    public void Header_TreeViewWithoutEvaluation_SaysTheDecisionIsNoneYetThenOff_Test()
    {
        (ViewController controller, _, _) = Create("a");

        Assert.Contains("decision: none yet", controller.Header, StringComparison.Ordinal);
        controller.ToggleDecision();
        Assert.Contains("decision: off", controller.Header, StringComparison.Ordinal);
    }

    private static (ViewController Controller, ScriptedServices Services, Workbench Workbench) Create(string text)
    {
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        ScriptedServices services = new();
        return (new ViewController(workbench, services.Services), services, workbench);
    }
}
