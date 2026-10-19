namespace TruthWeaver.Tool.Tui.Theme;

using TruthWeaver.Tool.Core.Configuration;

/// <summary>The color set that the workbench draws with. <see cref="ToolTheme.Auto"/> resolves to one of these at start-up.</summary>
internal enum ThemeVariant
{
    /// <summary>The light color set: dark text on a light background.</summary>
    Light,

    /// <summary>The dark color set: light text on a dark background.</summary>
    Dark,
}
