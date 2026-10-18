namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;

/// <summary><c>truthweaver import</c>: a rule in another rule format (JsonLogic) to rule text, JSON or YAML.</summary>
internal static class ImportCommand
{
    private const string JsonLogic = "jsonlogic";

    /// <summary>Creates the command.</summary>
    /// <param name="console">The console.</param>
    /// <returns>The command.</returns>
    public static Command Create(CliConsole console)
    {
        Argument<string> input = RuleInputOptions.InputArgument(
            "input",
            "The file to import, or - for standard input (the default)."
        );
        Option<string> from = new("--from")
        {
            Description = "The format to import. The only value is jsonlogic.",
            HelpName = "format",
            DefaultValueFactory = _ => JsonLogic,
        };
        from.AcceptOnlyFromAmong(JsonLogic);
        Option<RuleFormat> to = new("--to")
        {
            Description = "The format to write: text, json or yaml. The default is text.",
            HelpName = "format",
            DefaultValueFactory = _ => RuleFormat.Text,
        };
        Option<string?> config = new("--config")
        {
            Description =
                "The configuration file. The default is the nearest truthweaver.json in the directory of the input or a parent.",
            HelpName = "file",
        };
        Option<OperatorStyle?> operatorStyle = new("--operator-style")
        {
            Description = "The operator style of rule text: word, symbolic or cstyle.",
            HelpName = "style",
        };
        Option<int?> maxDepth = RuleInputOptions.PositiveLimit("--max-depth", "The most nesting depth of a rule.");
        Option<int?> maxNodeCount = RuleInputOptions.PositiveLimit("--max-node-count", "The most nodes in a rule.");
        Option<bool> json = new("--json")
        {
            Description = "Write one JSON object with the result, the diagnostics and the exit code.",
        };
        OutputTarget target = new();

        Command command = new("import", "Import a JsonLogic rule as rule text, JSON or YAML.")
        {
            input,
            from,
            to,
            config,
            operatorStyle,
            maxDepth,
            maxNodeCount,
            json,
        };
        target.AddTo(command);

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("import");
            OutputMode mode = parse.GetValue(json) ? OutputMode.Json : OutputMode.Text;
            string path = parse.GetValue(input)!;
            outcome.Result["from"] = parse.GetValue(from);

            string origin = path == SourceReader.StandardInput ? path : console.FullPath(path);
            ToolResult<SourceText> read;
            if (path == SourceReader.StandardInput)
            {
                using Stream stream = console.OpenInput();
                read = SourceReader.ReadStream(stream);
            }
            else
            {
                read = SourceReader.ReadFile(origin);
            }

            if (!read.TryGetValue(out SourceText? source, out EquatableArray<Diagnostic> readDiagnostics))
            {
                outcome.Add(origin, readDiagnostics);
                return outcome.Write(console, mode);
            }

            ToolSettings settings = ResolveSettings(
                parse,
                console,
                path,
                config,
                operatorStyle,
                maxDepth,
                maxNodeCount,
                outcome
            );
            ToolResult<string> imported = JsonLogicImporter.Import(source.Text);
            outcome.Add(origin, imported.Diagnostics);
            if (outcome.HasErrors || imported.Value is not { } tree)
            {
                return outcome.Write(console, mode);
            }

            // The imported tree is JSON; compile it with the tool's limits and print it in the format asked for.
            RuleFormat format = parse.GetValue(to);
            ToolResult<LoadedRule> loaded = RuleLoader.LoadString(
                tree,
                "<jsonlogic>",
                new RuleLoadOptions { Format = RuleFormat.Json }
            );
            if (!loaded.TryGetValue(out LoadedRule? rule, out EquatableArray<Diagnostic> loadDiagnostics))
            {
                outcome.Add(origin, loadDiagnostics);
                return outcome.Write(console, mode);
            }

            ToolResult<string> printed = RuleTextPrinting.Print(rule, format, settings);
            outcome.Add(rule, printed.Diagnostics);
            outcome.Result["format"] = FormatNames.Of(format);
            if (printed.Value is { } text)
            {
                target.Deliver(parse, console, outcome, text);
            }

            return outcome.Write(console, mode);
        });
        return command;
    }

    private static ToolSettings ResolveSettings(
        ParseResult parse,
        CliConsole console,
        string input,
        Option<string?> config,
        Option<OperatorStyle?> operatorStyle,
        Option<int?> maxDepth,
        Option<int?> maxNodeCount,
        CommandOutcome outcome
    )
    {
        string startDirectory =
            input == SourceReader.StandardInput
                ? console.WorkingDirectory
                : Path.GetDirectoryName(console.FullPath(input)) ?? console.WorkingDirectory;
        string? configPath = parse.GetValue(config) is { } explicitPath
            ? console.FullPath(explicitPath)
            : ToolConfigurationFile.Find(startDirectory);

        ToolConfiguration? fromFile = null;
        if (configPath is not null)
        {
            ToolResult<ToolConfiguration> loadedConfiguration = ToolConfigurationFile.Load(configPath);
            outcome.Add(configPath, loadedConfiguration.Diagnostics);
            fromFile = loadedConfiguration.Value;
        }

        ToolConfiguration commandLine = new()
        {
            OperatorStyle = parse.GetValue(operatorStyle),
            MaxDepth = parse.GetValue(maxDepth),
            MaxNodeCount = parse.GetValue(maxNodeCount),
        };
        return ToolSettings.Resolve(fromFile, commandLine);
    }
}
