namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Tool.Core.Export;

/// <summary>What an entry of the export menu does.</summary>
internal enum ExportEntryKind
{
    /// <summary>Makes one artifact of the tool core catalog.</summary>
    Artifact,

    /// <summary>Renders the Mermaid or DOT diagram as an image with an external program.</summary>
    Image,
}

/// <summary>One entry of the export dialog.</summary>
/// <param name="Kind">What the entry does.</param>
/// <param name="Label">The text of the entry in the list.</param>
/// <param name="Artifact">The artifact of the catalog, or <see langword="null"/> for an image entry.</param>
/// <param name="Language">The diagram language of an image entry.</param>
/// <param name="Format">The image format of an image entry.</param>
/// <param name="Group">The heading of the dialog that holds the entry, one of <see cref="ExportGroups"/>.</param>
/// <param name="FileName">The file name that the dialog suggests for the entry.</param>
/// <param name="Hint">A line that the dialog shows under the image note while the entry is chosen, such as what to install; empty for none.</param>
internal sealed record ExportEntry(
    ExportEntryKind Kind,
    string Label,
    ArtifactDefinition? Artifact = null,
    DiagramLanguage Language = DiagramLanguage.Dot,
    ImageFormat Format = ImageFormat.Svg,
    string Group = "",
    string FileName = "",
    string Hint = ""
);
