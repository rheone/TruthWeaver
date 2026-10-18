namespace TruthWeaver.Tool.Tui.Tests.Panes;

using TruthWeaver.Tool.Tui.Panes;

/// <summary>The layout model of the regions: divider steps, limits and the maximised region.</summary>
public sealed class PaneLayoutTests
{
    /// <summary>A divider moves one step per call and stops at the limits.</summary>
    [Fact]
    public void MoveVerticalDivider_ManySteps_StopsAtTheLimit_Test()
    {
        PaneLayout layout = new();

        layout.MoveVerticalDivider(100);

        Assert.Equal(PaneLayout.MaximumPercent, layout.EditorWidthPercent);
    }

    /// <summary>The horizontal divider moves up in steps and stops at the lower limit.</summary>
    [Fact]
    public void MoveHorizontalDivider_ManyStepsUp_StopsAtTheLimit_Test()
    {
        PaneLayout layout = new();

        layout.MoveHorizontalDivider(-100);

        Assert.Equal(PaneLayout.MinimumPercent, layout.TopHeightPercent);
    }

    /// <summary>Maximising a region hides the others; toggling it again shows every region.</summary>
    [Fact]
    public void ToggleMaximised_SameRegionTwice_ShowsEveryRegionAgain_Test()
    {
        PaneLayout layout = new();

        layout.ToggleMaximised(PaneSlot.Bottom);
        bool editorHidden = !layout.IsVisible(PaneSlot.Editor);
        layout.ToggleMaximised(PaneSlot.Bottom);

        Assert.True(editorHidden);
        Assert.Null(layout.Maximised);
        Assert.True(layout.IsVisible(PaneSlot.Editor));
    }
}
