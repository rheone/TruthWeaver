namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core;

/// <summary><c>truthweaver convert</c>: rule text, JSON and YAML to each other, with the notes kept.</summary>
internal static class ConvertCommand
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
        OutputTarget target = new();
        Option<RuleFormat> to = new("--to")
        {
            Description = "The format to convert to: text, json or yaml.",
            HelpName = "format",
            Required = true,
        };
        Option<bool> noNotes = new("--no-notes") { Description = "Leave out the notes of the rule." };

        Command command = new("convert", "Convert a rule to rule text, JSON or YAML. The result compiles to the same tree.")
        {
            input,
        };
        options.AddTo(command);
        command.Add(to);
        command.Add(noNotes);
        target.AddTo(command);

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("convert");
            string path = parse.GetValue(input)!;
            ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
            if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            RuleFormat format = parse.GetValue(to);
            ToolResult<string> converted = RuleTextPrinting.Print(
                rule,
                format,
                settings,
                includeNotes: !parse.GetValue(noNotes)
            );
            outcome.Add(rule, converted.Diagnostics);
            outcome.Result["format"] = FormatNames.Of(format);
            if (converted.Value is { } text)
            {
                target.Deliver(parse, console, outcome, text);
            }

            return outcome.Write(console, options.ModeOf(parse));
        });
        return command;
    }
}
