namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Tool.Core;

/// <summary><c>truthweaver format</c>: print a rule in its own format, canonical or pretty, or check that it already is.</summary>
internal static class FormatCommand
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
        Option<bool> pretty = new("--pretty")
        {
            Description =
                "Break rule text over lines of at most 80 characters, and indent JSON. YAML is always one node per line.",
        };

        Command command = new("format", "Print a rule in its own format: canonical, or pretty with --pretty.") { input };
        options.AddTo(command);
        command.Add(pretty);

        // Here --check without --output compares with the input itself: the rule is formatted when nothing changes.
        target.AddTo(command, checkNeedsOutput: false);

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("format");
            string path = parse.GetValue(input)!;
            ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
            if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            ToolResult<string> formatted = RuleTextPrinting.Print(rule, rule.Format, settings, pretty: parse.GetValue(pretty));
            outcome.Add(rule, formatted.Diagnostics);
            outcome.Result["format"] = FormatNames.Of(rule.Format);
            if (formatted.Value is not { } text)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            if (parse.GetValue(target.Check) && parse.GetValue(target.Output) is null)
            {
                OutputTarget.Report(outcome, CheckAgainstInput(console, rule, text, outcome));
            }
            else
            {
                target.Deliver(parse, console, outcome, text);
            }

            return outcome.Write(console, options.ModeOf(parse));
        });
        return command;
    }

    /// <summary>
    /// Compares the formatted text with the input. A whole file is compared byte for byte, so a CRLF line ending or a byte
    /// order mark is a difference; standard input and an embedded rule are compared as text.
    /// </summary>
    private static OutputCheck CheckAgainstInput(CliConsole console, LoadedRule rule, string text, CommandOutcome outcome)
    {
        if (rule.Path is null && rule.Source.Origin != SourceReader.StandardInput)
        {
            ToolResult<OutputCheck> check = OutputWriter.Check(console.FullPath(rule.Source.Origin), text);
            outcome.Add(rule.Source.Origin, check.Diagnostics);
            if (check.Value is { } value)
            {
                return value;
            }
        }

        return OutputTarget.CompareText(rule.Source.Origin, rule.Text, text);
    }
}
