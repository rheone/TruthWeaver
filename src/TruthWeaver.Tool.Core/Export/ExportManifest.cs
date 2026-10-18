namespace TruthWeaver.Tool.Core.Export;

using System.Text.Json;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// The manifest of an export of every artifact, written as <see cref="FileName"/> in the export folder: the source rule
/// and its hash, every file written, and every artifact left out with the reason. <see cref="ToJson"/> writes it.
/// </summary>
/// <param name="Format">The format of the source rule.</param>
/// <param name="RulePath">The path of the rule inside its document, or <see langword="null"/> when the whole input is the rule.</param>
/// <param name="SourceHash">The hash of the source rule, <c>sha256:</c> and 64 lower-case hexadecimal digits (see <see cref="ArtifactExporter.HashOf"/>).</param>
/// <param name="Artifacts">The files written, in catalog order, and then the images.</param>
/// <param name="Skipped">The artifacts left out, in catalog order.</param>
public sealed record ExportManifest(
    TruthWeaver.Compilation.RuleFormat Format,
    string? RulePath,
    string SourceHash,
    EquatableArray<ExportManifestEntry> Artifacts,
    EquatableArray<SkippedArtifact> Skipped
)
{
    /// <summary>The file name of the manifest in the export folder: <c>manifest.json</c>.</summary>
    public const string FileName = "manifest.json";

    /// <summary>
    /// Writes the manifest as JSON: an object with <c>source</c> (<c>format</c>, <c>path</c> when the rule is embedded,
    /// <c>hash</c>), <c>artifacts</c> (<c>id</c>, <c>file</c>, <c>format</c>, <c>options</c>) and <c>skipped</c>
    /// (<c>id</c>, <c>reason</c>). The manifest names no absolute path and no time, so it is the same on each run and on
    /// each machine.
    /// </summary>
    /// <returns>The JSON text, indented, with line feeds and no final line feed. Write it with <see cref="OutputWriter"/>.</returns>
    public string ToJson()
    {
        return DiagnosticWriter.WriteJson(json =>
        {
            json.WriteStartObject();
            json.WriteStartObject("source");
            json.WriteString("format", this.Format.ToString().ToLowerInvariant());
            if (this.RulePath is not null)
            {
                json.WriteString("path", this.RulePath);
            }

            json.WriteString("hash", this.SourceHash);
            json.WriteEndObject();
            json.WriteStartArray("artifacts");
            foreach (ExportManifestEntry entry in this.Artifacts)
            {
                WriteEntry(json, entry);
            }

            json.WriteEndArray();
            json.WriteStartArray("skipped");
            foreach (SkippedArtifact skipped in this.Skipped)
            {
                json.WriteStartObject();
                json.WriteString("id", skipped.Id);
                json.WriteString("reason", skipped.Reason);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        });
    }

    private static void WriteEntry(Utf8JsonWriter json, ExportManifestEntry entry)
    {
        json.WriteStartObject();
        json.WriteString("id", entry.Id);
        json.WriteString("file", entry.File);
        json.WriteString("format", entry.Format);
        json.WriteStartObject("options");
        foreach (KeyValuePair<string, string> option in entry.Options)
        {
            json.WriteString(option.Key, option.Value);
        }

        json.WriteEndObject();
        json.WriteEndObject();
    }
}
