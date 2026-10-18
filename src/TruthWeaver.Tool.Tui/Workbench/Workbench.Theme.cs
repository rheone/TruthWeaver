namespace TruthWeaver.Tool.Tui.Workbench;

using TruthWeaver.Tool.Tui.Theme;

/// <summary>The theme of the workbench: the color set, the <c>NO_COLOR</c> choice and the key that switches the color set.</summary>
internal sealed partial class Workbench
{
    /// <summary>Occurs after <see cref="ToggleTheme"/> changes <see cref="Theme"/>.</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>Gets the color set in use. It starts from the <c>theme</c> setting and the key switches it for the session.</summary>
    public ThemeVariant Theme { get; private set; }

    /// <summary>Gets a value indicating whether the author asked for no color (<c>NO_COLOR</c>).</summary>
    public bool NoColor { get; }

    /// <summary>Gets the colors of the current theme, or the palette with no color when <see cref="NoColor"/> is set.</summary>
    public ThemePalette Palette => ThemePalette.For(this.Theme, this.NoColor);

    /// <summary>
    /// Switches between the light and the dark color set. The <c>theme</c> setting is not written, but the choice goes to
    /// <see cref="Preferences"/>, so the next run starts with it.
    /// </summary>
    public void ToggleTheme()
    {
        this.Theme = this.Theme == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light;
        this.Preferences?.SaveTheme(this.Theme);
        this.ThemeChanged?.Invoke(this, EventArgs.Empty);
        string name = this.Theme.ToString().ToLowerInvariant();
        this.SetStatus(this.NoColor ? $"Theme: {name} (NO_COLOR is set, so the screen has no color)." : $"Theme: {name}.");
    }
}
