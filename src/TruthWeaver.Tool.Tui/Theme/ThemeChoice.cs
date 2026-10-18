namespace TruthWeaver.Tool.Tui.Theme;

using TruthWeaver.Tool.Core.Configuration;

/// <summary>Picks the theme at start-up from the <c>theme</c> setting.</summary>
internal static class ThemeChoice
{
    /// <summary>
    /// Resolves the setting. <see cref="ToolTheme.Light"/> and <see cref="ToolTheme.Dark"/> are taken as they are.
    /// <see cref="ToolTheme.Auto"/> uses what the terminal reports and falls back to dark when it reports nothing.
    /// </summary>
    /// <param name="setting">The <c>theme</c> setting.</param>
    /// <param name="detect">Asks the terminal for its background; it returns <see langword="null"/> when the terminal does not say. It runs only for <see cref="ToolTheme.Auto"/>.</param>
    /// <returns>The theme.</returns>
    public static ThemeVariant Resolve(ToolTheme setting, Func<ThemeVariant?> detect)
    {
        ArgumentNullException.ThrowIfNull(detect);

        return setting switch
        {
            ToolTheme.Light => ThemeVariant.Light,
            ToolTheme.Dark => ThemeVariant.Dark,
            _ => detect() ?? ThemeVariant.Dark,
        };
    }
}
