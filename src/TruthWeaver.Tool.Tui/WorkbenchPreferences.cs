namespace TruthWeaver.Tool.Tui;

using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Tool.Core.Configuration;

/// <summary>
/// The choices of the author that the workbench keeps between runs: the theme after the author switched it, and the sizes of
/// the regions. The choices live in one small JSON file of the user, outside any project. The file holds these values and no
/// other: no rule text and no file path. A file that is missing or cannot be read gives no choices, and the workbench starts
/// with the setting and the default layout. A file that cannot be written is ignored too: a lost choice is not worth an error.
/// </summary>
internal sealed class WorkbenchPreferences
{
    /// <summary>The name of the file in the folder of <see cref="DefaultPath"/>.</summary>
    public const string FileName = "workbench.json";

    private const string ThemeKey = "theme";
    private const string LayoutKey = "layout";
    private const string EditorWidthKey = "editorWidthPercent";
    private const string TopHeightKey = "topHeightPercent";

    private readonly string path;

    private WorkbenchPreferences(string path)
    {
        this.path = path;
    }

    /// <summary>
    /// Gets the path of the file of the user: the <c>TruthWeaver</c> folder in the application-data folder of the user
    /// (<c>%APPDATA%</c> on Windows; <c>$XDG_CONFIG_HOME</c> or <c>~/.config</c> on Linux and macOS).
    /// </summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TruthWeaver", FileName);

    /// <summary>
    /// Gets the theme that the author chose with the key, or <see langword="null"/> when the author has not switched the theme.
    /// It is never <see cref="ToolTheme"/> <c>Auto</c>: only <c>light</c> and <c>dark</c> are kept.
    /// </summary>
    public ThemeVariant? Theme { get; private set; }

    /// <summary>Gets the saved width of the editor region in percent, or <see langword="null"/> for the default.</summary>
    public int? EditorWidthPercent { get; private set; }

    /// <summary>Gets the saved height of the top regions in percent, or <see langword="null"/> for the default.</summary>
    public int? TopHeightPercent { get; private set; }

    /// <summary>Reads the choices from a file.</summary>
    /// <param name="path">The path of the file, such as <see cref="DefaultPath"/>.</param>
    /// <returns>The choices. A missing, unreadable or corrupt file gives none, and a value that is not valid is left out.</returns>
    public static WorkbenchPreferences Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        WorkbenchPreferences preferences = new(path);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return preferences;
        }

        if (root is not JsonObject values)
        {
            return preferences;
        }

        if (values[ThemeKey] is JsonValue theme && theme.TryGetValue(out string? name))
        {
            preferences.Theme = name.ToUpperInvariant() switch
            {
                "LIGHT" => ThemeVariant.Light,
                "DARK" => ThemeVariant.Dark,
                _ => null,
            };
        }

        if (values[LayoutKey] is JsonObject layout)
        {
            preferences.EditorWidthPercent = PercentOf(layout[EditorWidthKey]);
            preferences.TopHeightPercent = PercentOf(layout[TopHeightKey]);
        }

        return preferences;
    }

    /// <summary>Keeps the theme that the author switched to.</summary>
    /// <param name="theme">The theme.</param>
    public void SaveTheme(ThemeVariant theme)
    {
        this.Theme = theme;
        this.Write();
    }

    /// <summary>Keeps the sizes of the regions. The maximised region is not kept: it is a temporary view.</summary>
    /// <param name="layout">The layout.</param>
    public void SaveLayout(PaneLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        this.EditorWidthPercent = layout.EditorWidthPercent;
        this.TopHeightPercent = layout.TopHeightPercent;
        this.Write();
    }

    // A percent is valid when it is a whole number in the range that the layout allows.
    private static int? PercentOf(JsonNode? node)
    {
        return
            node is JsonValue value
            && value.TryGetValue(out int percent)
            && percent is >= PaneLayout.MinimumPercent and <= PaneLayout.MaximumPercent
            ? percent
            : null;
    }

    private void Write()
    {
        JsonObject values = [];

        // A choice that the author did not make stays out of the file, so the setting still applies for it.
        if (this.Theme is { } theme)
        {
            values[ThemeKey] = theme.ToString().ToLowerInvariant();
        }

        if (this.EditorWidthPercent is { } width && this.TopHeightPercent is { } height)
        {
            values[LayoutKey] = new JsonObject { [EditorWidthKey] = width, [TopHeightKey] = height };
        }

        try
        {
            string? folder = Path.GetDirectoryName(this.path);
            if (!string.IsNullOrEmpty(folder))
            {
                _ = Directory.CreateDirectory(folder);
            }

            File.WriteAllText(this.path, values.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The choice stays for this session. A read-only folder is not a reason to interrupt the author.
        }
    }
}
