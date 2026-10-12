namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Tool.Core;

/// <summary><c>truthweaver print</c>: one artifact of a rule on standard output.</summary>
internal static class PrintCommand
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
        ArtifactOptions artifacts = new("rule-text");

        Command command = new("print", "Print one artifact of a rule, such as an equation, a diagram or a truth table.")
        {
            input,
        };
        options.AddTo(command);
        artifacts.AddTo(command);

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("print");
            string path = parse.GetValue(input)!;
            ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
            if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            string id = ArtifactOptions.Resolve(parse.GetValue(artifacts.Artifact)!, settings);
            ToolResult<string> rendered = ArtifactExporter.Render(rule, id, artifacts.ExportOptions(parse, settings));
            outcome.Add(rule, rendered.Diagnostics);
            outcome.Result["artifact"] = id;
            if (rendered.Value is { } text)
            {
                outcome.Text = text;
                outcome.Result["text"] = OutputWriter.Normalize(text);
            }

            return outcome.Write(console, options.ModeOf(parse));
        });
        return command;
    }
}
