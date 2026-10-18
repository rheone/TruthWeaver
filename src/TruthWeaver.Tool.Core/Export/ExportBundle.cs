namespace TruthWeaver.Tool.Core.Export;

using TruthWeaver.Tool.Core.Output;

/// <summary>
/// The text files of an export of every artifact, made but not written: <see cref="ArtifactExporter.RenderAll"/> gives
/// it, and <see cref="ArtifactExporter.ExportAll"/> writes it. A front end compares the files with a folder through
/// <see cref="OutputWriter.Check"/> for a <c>--check</c> option.
/// </summary>
/// <param name="Files">The files in catalog order, and <see cref="ExportManifest.FileName"/> last.</param>
/// <param name="Manifest">The manifest, which lists the files except itself.</param>
public sealed record ExportBundle(EquatableArray<ExportedFile> Files, ExportManifest Manifest);
