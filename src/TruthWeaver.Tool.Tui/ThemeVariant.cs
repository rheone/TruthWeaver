namespace TruthWeaver.Tool.Tui;

/// <summary>The color set that the workbench draws with. <see cref="Core.ToolTheme.Auto"/> resolves to one of these at start-up.</summary>
internal enum ThemeVariant
{
    /// <summary>The colors for a light terminal background.</summary>
    Light,

    /// <summary>The colors for a dark terminal background.</summary>
    Dark,
}
