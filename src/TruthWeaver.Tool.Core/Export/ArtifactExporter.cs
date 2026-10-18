namespace TruthWeaver.Tool.Core.Export;

using System.Security.Cryptography;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// Makes the artifacts of the <see cref="ArtifactCatalog"/> from a loaded rule, and exports one artifact to a string, a
/// writer (such as standard output) or a file, or every artifact to a folder with a <c>manifest.json</c>. The output is
/// deterministic: the same rule and options give the same bytes on each run.
/// </summary>
public static class ArtifactExporter
{
    /// <summary>Makes the text of one artifact.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="artifactId">The artifact id, such as <c>equation-latex-letters</c>.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>.</param>
    /// <returns>
    /// The text as the library printer writes it, or no text and the diagnostics: the compile diagnostics when the rule does
    /// not compile, <c>TRE0035</c> for a refused truth table, or <see cref="ToolDiagnosticCodes.UnknownArtifact"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="artifactId"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> Render(LoadedRule rule, string artifactId, ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(artifactId);

        return ArtifactCatalog.TryFind(artifactId, out ArtifactDefinition? artifact)
            ? Render(rule, artifact, options)
            : UnknownArtifact<string>(artifactId);
    }

    /// <summary>Makes the text of one artifact.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="artifact">The artifact.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>.</param>
    /// <returns>The text as the library printer writes it, or no text and the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="artifact"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> Render(LoadedRule rule, ArtifactDefinition artifact, ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(artifact);

        return artifact.Render(new ArtifactSource(rule, options ?? ExportOptions.Default));
    }

    /// <summary>
    /// Writes one artifact to a writer, such as standard output, with line feeds and a final newline (see
    /// <see cref="OutputWriter.Write"/>). Nothing is written when the artifact cannot be made.
    /// </summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="artifact">The artifact.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>.</param>
    /// <returns>The text written, or no text and the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/>, <paramref name="artifact"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> Export(
        LoadedRule rule,
        ArtifactDefinition artifact,
        TextWriter writer,
        ExportOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);

        ToolResult<string> result = Render(rule, artifact, options);
        if (result.Value is { } text)
        {
            OutputWriter.Write(writer, text);
        }

        return result;
    }

    /// <summary>
    /// Writes one artifact to a file through <see cref="OutputWriter.WriteFile"/>, and creates the directory. Nothing is
    /// written when the artifact cannot be made.
    /// </summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="artifact">The artifact.</param>
    /// <param name="path">The file path.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>.</param>
    /// <returns>The full path of the file, or no path and the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/>, <paramref name="artifact"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> Export(
        LoadedRule rule,
        ArtifactDefinition artifact,
        string path,
        ExportOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(path);

        ToolResult<string> result = Render(rule, artifact, options);
        if (result.Value is not { } text)
        {
            return result;
        }

        ToolResult<string> written = OutputWriter.WriteFile(path, text);
        return new ToolResult<string>(
            written.Value,
            new EquatableArray<Diagnostic>([.. result.Diagnostics, .. written.Diagnostics])
        );
    }

    /// <summary>
    /// Makes the text of every artifact and the manifest, without writing them. The rule is compiled and validated once.
    /// A truth table over the term cap is left out with a <see cref="ToolDiagnosticCodes.ArtifactSkipped"/> warning and a
    /// <see cref="SkippedArtifact"/> entry, so a large rule still exports.
    /// </summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>. <see cref="ExportOptions.Images"/> is not used here.</param>
    /// <returns>The files and the manifest, or no value and the compile diagnostics when the rule does not compile.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static ToolResult<ExportBundle> RenderAll(LoadedRule rule, ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ArtifactSource source = new(rule, options ?? ExportOptions.Default);
        if (source.Compiled is null)
        {
            return ToolResults.Failure<ExportBundle>(source.CompileDiagnostics);
        }

        List<ExportedFile> files = [];
        List<ExportManifestEntry> entries = [];
        List<SkippedArtifact> skipped = [];
        List<Diagnostic> diagnostics = [];
        foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
        {
            ToolResult<string> result = artifact.Render(source);
            if (result.Value is { } text)
            {
                files.Add(new ExportedFile(artifact.FileName, text));
                entries.Add(new ExportManifestEntry(artifact.Id, artifact.FileName, artifact.Format, new(artifact.Options)));
                diagnostics.AddRange(result.Diagnostics);
                continue;
            }

            // The rule compiles, so the only artifact it cannot give is a truth table over the cap.
            string reason = string.Join(" ", result.Diagnostics.Select(d => d.Message));
            skipped.Add(new SkippedArtifact(artifact.Id, reason));
            diagnostics.Add(
                ToolResults.Warning(
                    ToolDiagnosticCodes.ArtifactSkipped,
                    $"The artifact '{artifact.Id}' is left out: {reason} The truth-table term limit of this export is {source.Options.EffectiveTruthTableTermLimit}."
                )
            );
        }

        ExportManifest manifest = new(rule.Format, rule.Path, HashOf(rule), new(entries), new(skipped));
        files.Add(new ExportedFile(ExportManifest.FileName, manifest.ToJson()));
        return ToolResults.Success(new ExportBundle(new(files), manifest), diagnostics);
    }

    /// <summary>
    /// Exports every artifact to a folder, one file each (see <see cref="ArtifactDefinition.FileName"/>), and writes
    /// <see cref="ExportManifest.FileName"/> last. With <see cref="ExportOptions.Images"/>, it also renders the Mermaid and
    /// DOT diagrams as images (<c>mermaid.svg</c>, <c>dot.svg</c>) with the external renderers; an image that cannot be
    /// rendered is an error, and the manifest lists only the images that were written. Files of the folder that are not in
    /// the export stay.
    /// </summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="folder">The export folder. It is created when it does not exist.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ExportOptions.Default"/>.</param>
    /// <returns>The manifest, or no manifest and the diagnostics when the rule does not compile or a file cannot be written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="folder"/> is <see langword="null"/>.</exception>
    public static ToolResult<ExportManifest> ExportAll(LoadedRule rule, string folder, ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(folder);
        options ??= ExportOptions.Default;

        ToolResult<ExportBundle> bundle = RenderAll(rule, options);
        if (bundle.Value is not { } value)
        {
            return ToolResults.Failure<ExportManifest>(bundle.Diagnostics);
        }

        List<Diagnostic> diagnostics = [.. bundle.Diagnostics];
        foreach (ExportedFile file in value.Files.Where(f => f.FileName != ExportManifest.FileName))
        {
            ToolResult<string> written = OutputWriter.WriteFile(Path.Combine(folder, file.FileName), file.Text);
            if (!written.Succeeded)
            {
                return ToolResults.Failure<ExportManifest>([.. diagnostics, .. written.Diagnostics]);
            }
        }

        ExportManifest manifest = value.Manifest;
        if (options.Images is { } images)
        {
            List<ExportManifestEntry> rendered = [];
            foreach (
                (string id, DiagramLanguage language) in new[]
                {
                    ("mermaid", DiagramLanguage.Mermaid),
                    ("dot", DiagramLanguage.Dot),
                }
            )
            {
                ExportedFile diagram = value.Files.First(f =>
                    f.FileName == ArtifactCatalog.All.First(a => a.Id == id).FileName
                );
                string format = ImageRenderer.Extension(images.Format)[1..];
                string fileName = id + ImageRenderer.Extension(images.Format);
                ToolResult<string> image = ImageRenderer.Render(diagram.Text, language, Path.Combine(folder, fileName), images);
                diagnostics.AddRange(image.Diagnostics);
                if (image.Succeeded)
                {
                    rendered.Add(
                        new ExportManifestEntry(
                            id + "-" + format,
                            fileName,
                            format,
                            new EquatableArray<KeyValuePair<string, string>>([
                                new KeyValuePair<string, string>("renderer", ImageRenderer.ProgramName(language)),
                                new KeyValuePair<string, string>("source", diagram.FileName),
                            ])
                        )
                    );
                }
            }

            manifest = manifest with { Artifacts = new([.. manifest.Artifacts, .. rendered]) };
        }

        ToolResult<string> manifestFile = OutputWriter.WriteFile(
            Path.Combine(folder, ExportManifest.FileName),
            manifest.ToJson()
        );
        diagnostics.AddRange(manifestFile.Diagnostics);
        return manifestFile.Succeeded
            ? ToolResults.Success(manifest, diagnostics)
            : ToolResults.Failure<ExportManifest>(diagnostics);
    }

    /// <summary>
    /// Gets the hash of a source rule for the manifest: SHA-256 of the rule text (<see cref="LoadedRule.Text"/>) with its
    /// line endings made LF, in UTF-8, as <c>sha256:</c> and 64 lower-case hexadecimal digits. A rule checked out with CRLF
    /// line endings has the same hash.
    /// </summary>
    /// <param name="rule">The loaded rule.</param>
    /// <returns>The hash.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static string HashOf(LoadedRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rule.Text.ReplaceLineEndings("\n")));
        return "sha256:" + Convert.ToHexStringLower(hash);
    }

    private static ToolResult<T> UnknownArtifact<T>(string id)
        where T : class
    {
        return ToolResults.Failure<T>(
            ToolDiagnosticCodes.UnknownArtifact,
            $"'{id}' is not an artifact id. The ids are: {string.Join(", ", ArtifactCatalog.All.Select(a => a.Id))}."
        );
    }
}
