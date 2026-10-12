namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core;

/// <summary><c>truthweaver validate</c>: every validation level, the statistics, and the diagnostics as text, JSON or SARIF.</summary>
internal static class ValidateCommand
{
    // The level names of --skip, in the order of the report, with the flag each one switches off.
    private static readonly (string Name, ValidationLevels Level)[] Levels =
    [
        ("compile", ValidationLevels.Compile),
        ("tree-schema", ValidationLevels.TreeSchema),
        ("limits", ValidationLevels.Limits),
        ("predicate-schema", ValidationLevels.PredicateSchema),
        ("analysis", ValidationLevels.Analysis),
        ("conversion-check", ValidationLevels.ConversionCheck),
    ];

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
        Option<bool> strict = new("--strict") { Description = "Fail (exit code 1) on a warning too, not only on an error." };
        Option<bool> sarif = new("--sarif") { Description = "Write the diagnostics as a SARIF 2.1.0 log." };
        Option<string[]> skip = new("--skip")
        {
            Description = "Switch a validation level off. Repeat it for more levels.",
            HelpName = "level",
            AllowMultipleArgumentsPerToken = false,
        };
        skip.AcceptOnlyFromAmong([.. Levels.Select(l => l.Name)]);
        Option<bool> styleLint = new("--style-lint") { Description = "Add the style lint to the analysis level." };

        Command command = new("validate", "Validate a rule in every level, and report the statistics.") { input };
        options.AddTo(command);
        command.Add(strict);
        command.Add(sarif);
        command.Add(skip);
        command.Add(styleLint);
        command.Validators.Add(result =>
        {
            if (result.GetValue(sarif) && result.GetValue(options.Json))
            {
                result.AddError("--sarif and --json cannot be used together.");
            }
        });

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("validate") { Strict = parse.GetValue(strict) };
            OutputMode mode = parse.GetValue(sarif) ? OutputMode.Sarif : options.ModeOf(parse);
            string path = parse.GetValue(input)!;
            ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
            if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
            {
                return outcome.Write(console, mode);
            }

            ValidationLevels levels = ValidationLevels.All;
            foreach (string name in parse.GetValue(skip) ?? [])
            {
                levels &= ~Levels.Single(l => l.Name == name).Level;
            }

            ValidationOptions validation = new()
            {
                Levels = levels,
                Settings = settings,
                Lints = parse.GetValue(styleLint) ? LintRules.All | LintRules.Style : LintRules.All,
            };
            ValidationReport report = RuleValidator.Validate(rule, validation).Value!;
            outcome.AddShownInText(DiagnosticDocument.For(rule, report.Diagnostics));
            outcome.Text = ValidationReportWriter.Format(report, DiagnosticOutputFormat.Text);
            outcome.Result["report"] = CommandOutcome.ParseJson(
                ValidationReportWriter.Format(report, DiagnosticOutputFormat.Json)
            );
            return outcome.Write(console, mode);
        });
        return command;
    }
}
