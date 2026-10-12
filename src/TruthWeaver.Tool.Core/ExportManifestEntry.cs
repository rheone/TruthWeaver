namespace TruthWeaver.Tool.Core;

/// <summary>One file of an export of every artifact, as the <see cref="ExportManifest"/> lists it.</summary>
/// <param name="Id">The artifact id, or for an image the diagram id and the image format, such as <c>mermaid-svg</c>.</param>
/// <param name="File">The file name in the export folder.</param>
/// <param name="Format">The name of the output format (see <see cref="ArtifactDefinition.Format"/>), or <c>svg</c> or <c>png</c> for an image.</param>
/// <param name="Options">The printer options, in ordinal order of the name. An image has its <c>source</c> file and its <c>renderer</c>.</param>
public sealed record ExportManifestEntry(
    string Id,
    string File,
    string Format,
    EquatableArray<KeyValuePair<string, string>> Options
);
