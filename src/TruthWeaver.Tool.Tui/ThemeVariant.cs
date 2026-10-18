namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Tool.Core.Configuration;

/// <summary>The color set that the workbench draws with. <see cref="ToolTheme.Auto"/> resolves to one of these at start-up.</summary>
internal enum ThemeVariant
{
    /// <summary>The colors for a light terminal background.</summary>
    Light,

    /// <summary>The colors for a dark terminal background.</summary>
    Dark,
}
