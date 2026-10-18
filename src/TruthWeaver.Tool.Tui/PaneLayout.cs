namespace TruthWeaver.Tool.Tui;

/// <summary>
/// The sizes of the workbench regions and the maximised region, apart from any view. The window reads it to place its
/// regions; keys change it in steps.
/// </summary>
internal sealed class PaneLayout
{
    /// <summary>The smallest share, in percent, that a region keeps when the author resizes.</summary>
    public const int MinimumPercent = 15;

    /// <summary>The largest share, in percent, that a region takes when the author resizes.</summary>
    public const int MaximumPercent = 85;

    /// <summary>The change, in percent, of one resize step.</summary>
    public const int Step = 5;

    /// <summary>Gets the width of the editor region, in percent of the window width. The side region takes the rest.</summary>
    public int EditorWidthPercent { get; private set; } = 55;

    /// <summary>Gets the height of the editor and side regions, in percent of the window height. The bottom region takes the rest.</summary>
    public int TopHeightPercent { get; private set; } = 70;

    /// <summary>Gets the maximised region, or <see langword="null"/> when every region shows.</summary>
    public PaneSlot? Maximised { get; private set; }

    /// <summary>Moves the line between the editor and side regions by <paramref name="steps"/> steps (negative is left).</summary>
    /// <param name="steps">The number of steps.</param>
    public void MoveVerticalDivider(int steps)
    {
        this.EditorWidthPercent = Clamp(this.EditorWidthPercent + (steps * Step));
    }

    /// <summary>Moves the line above the bottom region by <paramref name="steps"/> steps (negative is up).</summary>
    /// <param name="steps">The number of steps.</param>
    public void MoveHorizontalDivider(int steps)
    {
        this.TopHeightPercent = Clamp(this.TopHeightPercent + (steps * Step));
    }

    /// <summary>Maximises <paramref name="slot"/>, or shows every region again when <paramref name="slot"/> is already maximised.</summary>
    /// <param name="slot">The region.</param>
    public void ToggleMaximised(PaneSlot slot)
    {
        this.Maximised = this.Maximised == slot ? null : slot;
    }

    /// <summary>Gets a value indicating whether <paramref name="slot"/> shows in the current layout.</summary>
    /// <param name="slot">The region.</param>
    /// <returns><see langword="true"/> when no region is maximised or <paramref name="slot"/> is the maximised one.</returns>
    public bool IsVisible(PaneSlot slot)
    {
        return this.Maximised is null || this.Maximised == slot;
    }

    private static int Clamp(int percent)
    {
        return Math.Clamp(percent, MinimumPercent, MaximumPercent);
    }
}
