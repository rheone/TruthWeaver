namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Reports;

/// <summary><c>truthweaver report</c>: the Markdown report of a rule.</summary>
internal static class ReportCommand
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
        Option<bool> allowLarger = new("--allow-large-truth-table")
        {
            Description = "Put a truth table with more than 8 terms, up to 12, in the report, with a TRE0034 warning.",
        };
        Option<int?> limit = ArtifactOptions.NewLimitOption();

        Command command = new(
            "report",
            "Write the Markdown report of a rule: the rule, the equation, a diagram, the truth table, the statistics and the analysis."
        )
        {
            input,
        };
        options.AddTo(command);
        command.Add(allowLarger);
        command.Add(limit);
        target.AddTo(command);

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("report");
            string path = parse.GetValue(input)!;
            ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
            if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            ExportOptions export = new()
            {
                Settings = settings,
                AllowLargerTruthTable = parse.GetValue(allowLarger),
                TruthTableTermLimit = parse.GetValue(limit),
            };
            ToolResult<string> report = RuleReport.Format(rule, export);
            outcome.Add(rule, report.Diagnostics);
            if (report.Value is { } text)
            {
                target.Deliver(parse, console, outcome, text);
            }

            return outcome.Write(console, options.ModeOf(parse));
        });
        return command;
    }
}
