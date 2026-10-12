namespace TruthWeaver.Tool.Core;

using TruthWeaver.Printing;

/// <summary>The options of <see cref="ArtifactExporter"/> and <see cref="RuleReport"/>.</summary>
public sealed record ExportOptions
{
    /// <summary>Gets the default options: the default settings, the truth-table cap kept, and no images.</summary>
    public static ExportOptions Default { get; } = new();

    /// <summary>
    /// Gets the settings. The compile limits and the analysis limit apply to every artifact, and the schema file applies to
    /// the diagnostics artifacts. The printer options of an artifact are fixed by its <see cref="ArtifactDefinition"/>.
    /// The default is <see cref="ToolSettings.Default"/>.
    /// </summary>
    public ToolSettings Settings { get; init; } = ToolSettings.Default;

    /// <summary>
    /// Gets a value indicating whether a truth table with more terms than <see cref="TruthTableOptions.DefaultTermCap"/> is
    /// printed anyway, up to <see cref="TruthTableOptions.HardTermLimit"/>, with a <c>TRE0034</c> warning. The default is
    /// <see langword="false"/>.
    /// </summary>
    public bool AllowLargerTruthTable { get; init; }

    /// <summary>
    /// Gets the image options for <see cref="ArtifactExporter.ExportAll"/>: when set, the export also renders the Mermaid
    /// and DOT diagrams as images with the external renderers. The default is <see langword="null"/>, no images.
    /// </summary>
    public ImageRenderOptions? Images { get; init; }
}
