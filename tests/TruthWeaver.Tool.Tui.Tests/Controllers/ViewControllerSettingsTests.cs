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

    /// <summary>Each view kind saves with its own file extension. The kind is given by name because the enum is internal.</summary>
    [Theory]
    [InlineData("Json", ".json")]
    [InlineData("Yaml", ".yaml")]
    [InlineData("Mermaid", ".mmd")]
    [InlineData("Dot", ".dot")]
    [InlineData("Html", ".html")]
    [InlineData("RuleText", ".rule")]
    [InlineData("Tree", ".txt")]
    public void Extension_ViewKind_GivesTheFileExtension_Test(string kind, string extension)
    {
        (ViewController controller, _, _) = Create("a");
        controller.Settings.Kind = Enum.Parse<ViewKind>(kind);

        Assert.Equal(extension, controller.Extension);
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

    /// <summary>The header of a tree view says the decision is "none yet" before there is an evaluation.</summary>
    [Fact]
    public void Header_TreeViewWithoutEvaluation_SaysTheDecisionIsNoneYet_Test()
    {
        (ViewController controller, _, _) = Create("a");

        Assert.Contains("decision: none yet", controller.Header, StringComparison.Ordinal);
    }

    /// <summary>The header of a tree view says the decision is "off" when the decision is hidden.</summary>
    [Fact]
    public void Header_DecisionToggledOff_SaysTheDecisionIsOff_Test()
    {
        (ViewController controller, _, _) = Create("a");

        controller.ToggleDecision();

        Assert.Contains("decision: off", controller.Header, StringComparison.Ordinal);
    }

    /// <summary>The constant style moves to the next style and tells the panes.</summary>
    [Fact]
    public void CycleConstantStyle_Once_ChangesTheStyleAndRaisesSettingsChanged_Test()
    {
        (ViewController controller, _, _) = Create("a");
        ConstantStyle? before = controller.Settings.ConstantStyle;
        int raised = 0;
        controller.SettingsChanged += (_, _) => raised++;

        controller.CycleConstantStyle();

        Assert.NotEqual(before, controller.Settings.ConstantStyle);
        Assert.Equal(1, raised);
    }

    /// <summary>The simple-variables toggle flips the setting and tells the panes.</summary>
    [Fact]
    public void ToggleSimpleVariables_Once_FlipsTheSettingAndRaisesSettingsChanged_Test()
    {
        (ViewController controller, _, _) = Create("a");
        bool before = controller.Settings.SimpleVariables;
        int raised = 0;
        controller.SettingsChanged += (_, _) => raised++;

        controller.ToggleSimpleVariables();

        Assert.Equal(!before, controller.Settings.SimpleVariables);
        Assert.Equal(1, raised);
    }

    /// <summary>The header of a LaTeX equation names the envelope, and a constant override is marked.</summary>
    [Fact]
    public void Header_LatexEquationWithAConstantOverride_NamesTheEnvelopeAndTheOverride_Test()
    {
        (ViewController controller, _, _) = Create("a");
        controller.Settings.Kind = ViewKind.Equation;
        controller.Settings.Dialect = EquationDialect.LaTeX;
        controller.Settings.TrueText = "yes";

        string header = controller.Header;

        Assert.Contains("envelope: ", header, StringComparison.Ordinal);
        Assert.Contains("(overrides)", header, StringComparison.Ordinal);
    }

    /// <summary>The header of the Mermaid view starts with its name.</summary>
    [Fact]
    public void Header_MermaidView_StartsWithItsName_Test()
    {
        (ViewController controller, _, _) = Create("a");
        controller.Settings.Kind = ViewKind.Mermaid;

        Assert.StartsWith("Mermaid", controller.Header, StringComparison.Ordinal);
    }

    /// <summary>The header of the YAML view starts with its name.</summary>
    [Fact]
    public void Header_YamlView_StartsWithItsName_Test()
    {
        (ViewController controller, _, _) = Create("a");
        controller.Settings.Kind = ViewKind.Yaml;

        Assert.StartsWith("YAML", controller.Header, StringComparison.Ordinal);
    }

    /// <summary>Saving a view that has no text because the rule has errors says so and writes nothing.</summary>
    [Fact]
    public void Save_RuleWithErrors_SaysThereIsNothingToSave_Test()
    {
        (ViewController controller, _, Workbench workbench) = Create("a AND");

        bool saved = controller.Save();

        Assert.False(saved);
        Assert.StartsWith("Nothing to save", workbench.Status, StringComparison.Ordinal);
    }

    private static (ViewController Controller, ScriptedServices Services, Workbench Workbench) Create(string text)
    {
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        ScriptedServices services = new();
        return (new ViewController(workbench, services.Services), services, workbench);
    }
}
