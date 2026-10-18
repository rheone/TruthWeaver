namespace TruthWeaver.Tool;

using System.CommandLine;
using System.CommandLine.Parsing;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;

/// <summary>
/// The options that every command that reads a rule shares: the input format and path, the configuration file, the
/// settings that override it, and <c>--json</c>. Each command makes its own instance, so that each command lists the
/// options in its own help.
/// </summary>
internal sealed class RuleInputOptions
{
    /// <summary>Gets the input format override.</summary>
    public Option<RuleFormat?> From { get; } =
        new("--from")
        {
            Description =
                "The format of the input: text, json or yaml. The default finds it from the file extension, then the content.",
            HelpName = "format",
        };

    /// <summary>Gets the path of a rule embedded in a JSON or YAML document.</summary>
    public Option<string?> Path { get; } =
        new("--path")
        {
            Description = "Select one rule inside a JSON or YAML document, such as rules.publish or $['rules'][0].",
            HelpName = "path",
        };

    /// <summary>Gets the explicit configuration file.</summary>
    public Option<string?> Config { get; } =
        new("--config")
        {
            Description =
                "The configuration file. The default is the nearest truthweaver.json in the directory of the input or a parent.",
            HelpName = "file",
        };

    /// <summary>Gets the operator style override.</summary>
    public Option<OperatorStyle?> OperatorStyle { get; } =
        new("--operator-style")
        {
            Description = "The operator style of rule text: word, symbolic or cstyle.",
            HelpName = "style",
        };

    /// <summary>Gets the constant style override.</summary>
    public Option<ConstantStyle?> ConstantStyle { get; } =
        new("--constant-style")
        {
            Description = "The style of the constants True, False and Unknown in equations: word, letters or logicsymbols.",
            HelpName = "style",
        };

    /// <summary>Gets the equation dialect override.</summary>
    public Option<EquationDialect?> Dialect { get; } =
        new("--dialect")
        {
            Description = "The equation dialect: unicode, latex, asciimath, cstyle, mathml, typst or ascii.",
            HelpName = "dialect",
        };

    /// <summary>Gets the depth limit override.</summary>
    public Option<int?> MaxDepth { get; } = PositiveLimit("--max-depth", "The most nesting depth of a rule.");

    /// <summary>Gets the node limit override.</summary>
    public Option<int?> MaxNodeCount { get; } = PositiveLimit("--max-node-count", "The most nodes in a rule.");

    /// <summary>Gets the analysis limit override.</summary>
    public Option<int?> MaxAnalysisTerms { get; } =
        PositiveLimit("--max-analysis-terms", "The most distinct terms for the Strong K3 analysis and the equivalence checks.");

    /// <summary>Gets the predicate schema file override.</summary>
    public Option<string?> SchemaFile { get; } =
        new("--schema-file")
        {
            Description = "The predicate schema file (YAML) that declares the predicates and the kinds of their arguments.",
            HelpName = "file",
        };

    /// <summary>Gets the JSON output switch.</summary>
    public Option<bool> Json { get; } =
        new("--json") { Description = "Write one JSON object with the result, the diagnostics and the exit code." };

    /// <summary>Makes the argument of one input: a file, or <c>-</c> (the default) for standard input.</summary>
    /// <param name="name">The argument name in the help.</param>
    /// <param name="description">The description.</param>
    /// <returns>The argument.</returns>
    public static Argument<string> InputArgument(string name, string description)
    {
        return new Argument<string>(name) { Description = description, DefaultValueFactory = _ => SourceReader.StandardInput };
    }

    /// <summary>Makes an option that takes a whole number from 1.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">The description.</param>
    /// <returns>The option.</returns>
    public static Option<int?> PositiveLimit(string name, string description)
    {
        Option<int?> option = new(name) { Description = description, HelpName = "n" };
        option.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int?>() is < 1)
            {
                result.AddError($"{name} must be a whole number from 1.");
            }
        });
        return option;
    }

    /// <summary>Adds every option to a command.</summary>
    /// <param name="command">The command.</param>
    public void AddTo(Command command)
    {
        command.Add(this.From);
        command.Add(this.Path);
        command.Add(this.Config);
        command.Add(this.OperatorStyle);
        command.Add(this.ConstantStyle);
        command.Add(this.Dialect);
        command.Add(this.MaxDepth);
        command.Add(this.MaxNodeCount);
        command.Add(this.MaxAnalysisTerms);
        command.Add(this.SchemaFile);
        command.Add(this.Json);
    }

    /// <summary>Gets the output mode: JSON with <c>--json</c>, otherwise text.</summary>
    /// <param name="parse">The parse result.</param>
    /// <returns>The mode.</returns>
    public OutputMode ModeOf(ParseResult parse)
    {
        return parse.GetValue(this.Json) ? OutputMode.Json : OutputMode.Text;
    }

    /// <summary>
    /// Resolves the settings: the command-line options over the configuration file over the defaults. The configuration
    /// file is <c>--config</c>, or the nearest <c>truthweaver.json</c> from the directory of the input (the working
    /// directory for standard input). Configuration diagnostics go to <paramref name="outcome"/>.
    /// </summary>
    /// <param name="parse">The parse result.</param>
    /// <param name="console">The console.</param>
    /// <param name="input">The first input of the command.</param>
    /// <param name="outcome">The outcome that collects the diagnostics.</param>
    /// <returns>The settings.</returns>
    public ToolSettings ResolveSettings(ParseResult parse, CliConsole console, string input, CommandOutcome outcome)
    {
        string? configPath = parse.GetValue(this.Config) is { } explicitPath
            ? console.FullPath(explicitPath)
            : ToolConfigurationFile.Find(StartDirectory(console, input));

        ToolConfiguration? file = null;
        if (configPath is not null)
        {
            ToolResult<ToolConfiguration> loaded = ToolConfigurationFile.Load(configPath);
            outcome.Add(configPath, loaded.Diagnostics);
            file = loaded.Value;
        }

        ToolConfiguration commandLine = new()
        {
            OperatorStyle = parse.GetValue(this.OperatorStyle),
            ConstantStyle = parse.GetValue(this.ConstantStyle),
            Dialect = parse.GetValue(this.Dialect),
            MaxDepth = parse.GetValue(this.MaxDepth),
            MaxNodeCount = parse.GetValue(this.MaxNodeCount),
            MaxAnalysisTerms = parse.GetValue(this.MaxAnalysisTerms),
            SchemaFile = parse.GetValue(this.SchemaFile) is { } schema ? console.FullPath(schema) : null,
        };
        return ToolSettings.Resolve(file, commandLine);
    }

    /// <summary>Loads one input with the format override and the embedded-rule path. Load diagnostics go to <paramref name="outcome"/>.</summary>
    /// <param name="parse">The parse result.</param>
    /// <param name="console">The console.</param>
    /// <param name="input">The file, or <c>-</c> for standard input.</param>
    /// <param name="outcome">The outcome that collects the diagnostics.</param>
    /// <returns>The rule, or <see langword="null"/> when it cannot be loaded.</returns>
    public LoadedRule? Load(ParseResult parse, CliConsole console, string input, CommandOutcome outcome)
    {
        RuleLoadOptions options = new() { Format = parse.GetValue(this.From), Path = parse.GetValue(this.Path) };
        ToolResult<LoadedRule> loaded;
        string origin;
        if (input == SourceReader.StandardInput)
        {
            origin = SourceReader.StandardInput;
            using Stream stream = console.OpenInput();
            loaded = RuleLoader.LoadStream(stream, options: options);
        }
        else
        {
            origin = console.FullPath(input);
            loaded = RuleLoader.LoadFile(origin, options);
        }

        if (loaded.TryGetValue(out LoadedRule? rule, out EquatableArray<Diagnostic> diagnostics))
        {
            outcome.Add(rule, diagnostics);
            return rule;
        }

        outcome.Add(origin, diagnostics);
        return null;
    }

    private static string StartDirectory(CliConsole console, string input)
    {
        return input == SourceReader.StandardInput
            ? console.WorkingDirectory
            : System.IO.Path.GetDirectoryName(console.FullPath(input)) ?? console.WorkingDirectory;
    }
}
