namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using System.Text.Json.Nodes;
using TruthWeaver.Tool.Core;

/// <summary>
/// <c>truthweaver export</c>: one artifact to a file (optionally rendered as an image), or every artifact to a folder
/// with a manifest, or a check that the files are up to date.
/// </summary>
internal static class ExportCommand
{
    /// <summary>Creates the command.</summary>
    /// <param name="console">The console.</param>
    /// <returns>The command.</returns>
    public static Command Create(CliConsole console)
    {
        Argument<string> input = RuleInputOptions.InputArgument(
            "input",
            "The rule file, or - for standard input (the default)."
        );
        RuleInputOptions options = new();
        ArtifactOptions artifacts = new(defaultArtifact: null);
        OutputTarget target = new();
        Option<string?> all = new("--all")
        {
            Description = "Export every artifact to this folder, with a manifest.json.",
            HelpName = "folder",
        };
        Option<ImageFormat?> render = new("--render")
        {
            Description =
                "Render the Mermaid and DOT diagrams as images (svg or png) with Graphviz dot or the Mermaid CLI, when installed.",
            HelpName = "format",
        };

        Command command = new("export", "Export one artifact to a file, or every artifact to a folder with a manifest.")
        {
            input,
        };
        options.AddTo(command);
        artifacts.AddTo(command);
        command.Add(all);
        command.Add(render);
        target.AddTo(command, checkNeedsOutput: false);
        command.Validators.Add(result =>
        {
            bool hasAll = result.GetValue(all) is not null;
            if (hasAll == (result.GetValue(target.Output) is not null))
            {
                result.AddError("Give either --artifact with --output, or --all with a folder.");
            }
            else if (hasAll == (result.GetValue(artifacts.Artifact) is not null))
            {
                result.AddError(hasAll ? "--all exports every artifact; leave out --artifact." : "--output needs --artifact.");
            }

            if (result.GetValue(render) is not null && result.GetValue(target.Check))
            {
                result.AddError("--check compares text files only; leave out --render.");
            }

            if (result.GetValue(render) is not null && result.GetValue(artifacts.Artifact) is { } id && LanguageOf(id) is null)
            {
                result.AddError($"Only the 'mermaid' and 'dot' artifacts can be rendered as an image, not '{id}'.");
            }
        });

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("export");
            string path = parse.GetValue(input)!;
            ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
            if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            ExportOptions export = artifacts.ExportOptions(parse, settings);
            ImageRenderOptions? images = parse.GetValue(render) is { } format
                ? new ImageRenderOptions { Format = format, SearchPath = console.RendererSearchPath }
                : null;
            if (parse.GetValue(all) is { } folder)
            {
                ExportAll(
                    rule,
                    console.FullPath(folder),
                    export with
                    {
                        Images = images,
                    },
                    parse.GetValue(target.Check),
                    outcome
                );
            }
            else
            {
                string id = ArtifactOptions.Resolve(parse.GetValue(artifacts.Artifact)!, settings);
                ExportOne(parse, console, rule, id, export, images, target, outcome);
            }

            return outcome.Write(console, options.ModeOf(parse));
        });
        return command;
    }

    private static DiagramLanguage? LanguageOf(string id)
    {
        return id switch
        {
            "mermaid" => DiagramLanguage.Mermaid,
            "dot" => DiagramLanguage.Dot,
            _ => null,
        };
    }

    private static void ExportOne(
        ParseResult parse,
        CliConsole console,
        LoadedRule rule,
        string id,
        ExportOptions export,
        ImageRenderOptions? images,
        OutputTarget target,
        CommandOutcome outcome
    )
    {
        outcome.Result["artifact"] = id;
        ToolResult<string> rendered = ArtifactExporter.Render(rule, id, export);
        outcome.Add(rule, rendered.Diagnostics);
        if (rendered.Value is not { } text)
        {
            return;
        }

        if (images is null)
        {
            target.Deliver(parse, console, outcome, text);
            return;
        }

        // The validator accepts --render only with the mermaid or dot artifact.
        string output = console.FullPath(parse.GetValue(target.Output)!);
        outcome.Result["output"] = output;
        ToolResult<string> image = ImageRenderer.Render(text, LanguageOf(id)!.Value, output, images);
        outcome.Add(output, image.Diagnostics);
    }

    private static void ExportAll(LoadedRule rule, string folder, ExportOptions export, bool check, CommandOutcome outcome)
    {
        outcome.Result["folder"] = folder;
        if (!check)
        {
            ToolResult<ExportManifest> exported = ArtifactExporter.ExportAll(rule, folder, export);
            outcome.Add(rule, exported.Diagnostics);
            if (exported.Value is { } manifest)
            {
                outcome.Result["manifest"] = CommandOutcome.ParseJson(manifest.ToJson());
                outcome.Text = string.Join(
                    "\n",
                    manifest
                        .Artifacts.Select(a => Path.Combine(folder, a.File))
                        .Append(Path.Combine(folder, ExportManifest.FileName))
                );
            }

            return;
        }

        ToolResult<ExportBundle> bundle = ArtifactExporter.RenderAll(rule, export);
        outcome.Add(rule, bundle.Diagnostics);
        if (bundle.Value is not { } files)
        {
            return;
        }

        JsonArray checks = [];
        List<string> lines = [];
        foreach (ExportedFile file in files.Files)
        {
            string filePath = Path.Combine(folder, file.FileName);
            ToolResult<OutputCheck> result = OutputWriter.Check(filePath, file.Text);
            outcome.Add(filePath, result.Diagnostics);
            if (result.Value is { } value)
            {
                checks.Add((JsonNode)OutputTarget.CheckJson(value));
                outcome.Failed |= !value.IsIdentical;
                lines.Add(OutputTarget.CheckLine(value));
            }
        }

        outcome.Result["checks"] = checks;
        outcome.Text = string.Join("\n", lines);
    }
}
