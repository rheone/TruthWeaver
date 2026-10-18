namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;

/// <summary>
/// Reads the command line of <c>truthweaver-tui</c> and prepares the first document and the settings, before any terminal
/// work: <c>truthweaver-tui [rule-file]</c>, <c>--help</c> and <c>--version</c>. The settings come from the nearest
/// <c>truthweaver.json</c>, found from the directory of the rule file or the working directory, as for the command line.
/// </summary>
internal static class TuiStartup
{
    /// <summary>The usage text that <c>--help</c> prints.</summary>
    public const string Usage = """
        Usage: truthweaver-tui [rule-file | -] [--format text|json|yaml]

        Opens the TruthWeaver workbench, a terminal editor for one rule in rule text, JSON or YAML.
        With no file, it starts with an empty rule. Press F1 in the workbench for the keys.
        A dash reads the rule from standard input.

        Options:
          --format <f>   The format of the file or of standard input: text, json or yaml.
                         Without it, the extension and then the content decide.
          -h, --help     Show this help and exit.
          --version      Show the version and exit.
        """;

    /// <summary>Prepares a run.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="workingDirectory">The working directory, for relative paths and the configuration file.</param>
    /// <param name="standardInput">The redirected standard input, or <see langword="null"/> when standard input is a terminal.</param>
    /// <returns>The document and settings to run with, or the text to print and the exit code.</returns>
    public static TuiStartupResult Prepare(
        IReadOnlyList<string> args,
        string workingDirectory,
        TextReader? standardInput = null
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        if (args.Any(a => a is "-h" or "--help" or "-?"))
        {
            return TuiStartupResult.Exit(Usage, ToolExitCode.Success);
        }

        if (args.Contains("--version"))
        {
            string version = typeof(TuiStartup).Assembly.GetName().Version?.ToString() ?? "unknown";
            return TuiStartupResult.Exit($"truthweaver-tui {version}", ToolExitCode.Success);
        }

        if (!TryParseArguments(args, out string? fileArgument, out bool useStandardInput, out RuleFormat? format))
        {
            return TuiStartupResult.Exit(
                "Expected at most one rule file or a dash, and only the --format option.\n\n" + Usage,
                ToolExitCode.Unusable
            );
        }

        if (useStandardInput && standardInput is null)
        {
            return TuiStartupResult.Exit(
                "Standard input is a terminal. Pipe a rule in, or name a file.\n\n" + Usage,
                ToolExitCode.Unusable
            );
        }

        string? file = fileArgument is null ? null : Path.GetFullPath(fileArgument, workingDirectory);
        string? configPath = ToolConfigurationFile.Find(Path.GetDirectoryName(file) ?? workingDirectory);
        ToolConfiguration? configuration = null;
        if (configPath is not null)
        {
            ToolResult<ToolConfiguration> loaded = ToolConfigurationFile.Load(configPath);
            if (!loaded.TryGetValue(out configuration, out EquatableArray<Diagnostic> problems))
            {
                return TuiStartupResult.Exit(Describe(configPath, problems), ToolExitCode.Unusable);
            }
        }

        ToolSettings settings = ToolSettings.Resolve(configuration, null);
        if (useStandardInput)
        {
            return FromStandardInput(standardInput!, format, settings);
        }

        if (file is null)
        {
            return TuiStartupResult.Run(RuleDocument.Create(settings: settings), settings);
        }

        ToolResult<RuleDocument> opened = RuleDocument.Open(file, settings, format);
        return opened.TryGetValue(out RuleDocument? document, out EquatableArray<Diagnostic> diagnostics)
            ? TuiStartupResult.Run(document, settings)
            : TuiStartupResult.Exit(Describe(file, diagnostics), ToolExitCode.Unusable);
    }

    private static bool TryParseArguments(
        IReadOnlyList<string> args,
        out string? file,
        out bool standardInput,
        out RuleFormat? format
    )
    {
        file = null;
        standardInput = false;
        format = null;
        int index = 0;
        while (index < args.Count)
        {
            string arg = args[index++];
            if (arg == "--format" || arg.StartsWith("--format=", StringComparison.Ordinal))
            {
                string? value;
                if (arg == "--format")
                {
                    value = index < args.Count ? args[index++] : null;
                }
                else
                {
                    value = arg["--format=".Length..];
                }

                if (!TryParseFormat(value, out format))
                {
                    return false;
                }
            }
            else if (arg == "-" && file is null && !standardInput)
            {
                standardInput = true;
            }
            else if (arg.StartsWith('-') || file is not null || standardInput)
            {
                return false;
            }
            else
            {
                file = arg;
            }
        }

        return true;
    }

    private static bool TryParseFormat(string? value, out RuleFormat? format)
    {
        format = value?.ToLowerInvariant() switch
        {
            "text" or "rule" => RuleFormat.Text,
            "json" => RuleFormat.Json,
            "yaml" or "yml" => RuleFormat.Yaml,
            _ => null,
        };
        return format is not null;
    }

    // A rule piped in opens even when it does not compile, so the diagnostics show in the workbench.
    private static TuiStartupResult FromStandardInput(TextReader standardInput, RuleFormat? format, ToolSettings settings)
    {
        string text = standardInput.ReadToEnd();
        ToolResult<LoadedRule> loaded = RuleLoader.LoadString(text, "<stdin>", new RuleLoadOptions { Format = format });
        return loaded.TryGetValue(out LoadedRule? rule, out EquatableArray<Diagnostic> diagnostics)
            ? TuiStartupResult.Run(RuleDocument.Create(rule.Text, rule.Format, settings), settings)
            : TuiStartupResult.Exit(Describe("<stdin>", diagnostics), ToolExitCode.Unusable);
    }

    private static string Describe(string origin, IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(
            '\n',
            diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => $"{origin}: error {d.Code}: {d.Message}")
        );
    }
}
