namespace TruthWeaver.Tool.Tui.Tests.Panes;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>Smoke test of the validation panel inside the workbench window.</summary>
[Collection(TerminalCollection.Name)]
public sealed class ValidationPaneTests
{
    /// <summary>The r key in the panel validates every level and fills the panel.</summary>
    [Fact]
    public void RunKey_InTheValidationPanel_ShowsEveryLevel_Test()
    {
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new DiagnosticsPane(), new ValidationPane()]);
        harness.Window.FocusRegion(PaneSlot.Bottom);
        harness.Press(Key.PageDown.WithCtrl);

        harness.Press(new Key('r'));

        ValidationController controller = ValidationController.For(harness.Workbench);
        Assert.NotNull(controller.Report);
        Assert.Equal("Validation", harness.Window.ActivePaneTitle(PaneSlot.Bottom));
    }

    /// <summary>The workbench registers the validation panel in the bottom region.</summary>
    [Fact]
    public void Default_Panes_IncludeTheValidationPanel_Test()
    {
        Assert.Contains(WorkbenchPanes.Default, p => p is ValidationPane { Slot: PaneSlot.Bottom });
    }
}
