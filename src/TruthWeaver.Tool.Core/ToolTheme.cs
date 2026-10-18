namespace TruthWeaver.Tool.Core;

/// <summary>The color set of the terminal workbench (the <c>theme</c> setting of <c>truthweaver.json</c>).</summary>
public enum ToolTheme
{
    /// <summary>Asks the terminal for its background at start-up and uses the dark set when the terminal does not answer.</summary>
    Auto,

    /// <summary>The colors for a light terminal background.</summary>
    Light,

    /// <summary>The colors for a dark terminal background.</summary>
    Dark,
}
