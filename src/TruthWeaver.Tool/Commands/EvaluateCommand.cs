namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>
/// <c>truthweaver evaluate</c>: evaluate a rule with stub values for its terms and data files for its
/// <c>from(...)</c> references, and print the decision and its trace.
/// </summary>
internal static class EvaluateCommand
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
        Option<string?> stubs = new("--stubs")
        {
            Description = "The stub-value file (JSON or YAML): a truth value for each term. A term with no value is Unknown.",
            HelpName = "file",
        };
        Option<string[]> data = new("--data")
        {
            Description = "A data file for from(\"name\", ...) references, as name=file. Repeat it for more sources.",
            HelpName = "name=file",
            AllowMultipleArgumentsPerToken = false,
        };
        data.Validators.Add(result =>
        {
            foreach (
                string value in (result.GetValueOrDefault<string[]>() ?? []).Where(v =>
                    v.IndexOf('=', StringComparison.Ordinal) <= 0 || v.EndsWith('=')
                )
            )
            {
                result.AddError($"--data takes name=file, not '{value}'.");
            }
        });
        Option<bool> exhaustive = new("--exhaustive") { Description = "Evaluate every term, with no short-circuiting." };
        Option<int?> faultBudget = RuleInputOptions.PositiveLimit(
            "--fault-budget",
            "Stop the evaluation when this many faults are recorded."
        );
        Option<bool> showValues = new("--show-values")
        {
            Description = "Show the value that each from(...) reference resolved to in the trace.",
        };
        Option<bool> trace = new("--trace") { Description = "Print the trace tree after the result." };

        Command command = new("evaluate", "Evaluate a rule with stub values and data files, and print the decision.") { input };
        options.AddTo(command);
        command.Add(stubs);
        command.Add(data);
        command.Add(exhaustive);
        command.Add(faultBudget);
        command.Add(showValues);
        command.Add(trace);

        command.SetAction(
            async (parse, cancellationToken) =>
            {
                CommandOutcome outcome = new("evaluate");
                string path = parse.GetValue(input)!;
                ToolSettings settings = options.ResolveSettings(parse, console, path, outcome);
                if (outcome.HasErrors || options.Load(parse, console, path, outcome) is not { } rule)
                {
                    return outcome.Write(console, options.ModeOf(parse));
                }

                StubAssignment stubValues = StubAssignment.Empty;
                if (parse.GetValue(stubs) is { } stubPath)
                {
                    ToolResult<StubAssignment> loaded = StubAssignment.Load(console.FullPath(stubPath));
                    outcome.Add(console.FullPath(stubPath), loaded.Diagnostics);
                    stubValues = loaded.Value ?? StubAssignment.Empty;
                }

                DataSourceSet? sources = LoadData(parse.GetValue(data) ?? [], console, outcome);
                if (outcome.HasErrors)
                {
                    return outcome.Write(console, options.ModeOf(parse));
                }

                EvaluationOptions evaluation = new(
                    FaultBudget: parse.GetValue(faultBudget),
                    Mode: parse.GetValue(exhaustive) ? EvaluationMode.Exhaustive : EvaluationMode.ShortCircuit,
                    IncludeResolvedValues: parse.GetValue(showValues),
                    IncludeTrace: true
                );
                StubEvaluationOptions stubOptions = new()
                {
                    Stubs = stubValues,
                    DataSources = sources,
                    Evaluation = evaluation,
                    Settings = settings,
                };
                ToolResult<StubEvaluation> evaluated = await StubEvaluator
                    .EvaluateAsync(rule, stubOptions, cancellationToken)
                    .ConfigureAwait(false);
                outcome.Add(rule, evaluated.Diagnostics);
                if (evaluated.Value is { } value)
                {
                    Describe(value, parse.GetValue(trace), outcome);
                }

                return outcome.Write(console, options.ModeOf(parse));
            }
        );
        return command;
    }

    private static DataSourceSet? LoadData(string[] values, CliConsole console, CommandOutcome outcome)
    {
        if (values.Length == 0)
        {
            return null;
        }

        // The validator has checked that each value is name=file with a name.
        List<KeyValuePair<string, string>> files =
        [
            .. values.Select(value =>
            {
                int split = value.IndexOf('=', StringComparison.Ordinal);
                return new KeyValuePair<string, string>(value[..split], console.FullPath(value[(split + 1)..]));
            }),
        ];
        ToolResult<DataSourceSet> loaded = DataSourceFiles.LoadAll(files);
        outcome.Add("--data", loaded.Diagnostics);
        return loaded.Value;
    }

    private static void Describe(StubEvaluation evaluation, bool includeTrace, CommandOutcome outcome)
    {
        Decision decision = evaluation.Decision;
        List<string> lines = [$"Result: {decision.Result}"];
        foreach (Fault fault in decision.Faults)
        {
            lines.Add($"Fault: {fault.Term}: {fault.ExceptionType}: {fault.Exception.Message}");
        }

        if (includeTrace)
        {
            lines.Add(evaluation.Rule.PrintPlainText(decision));
        }

        outcome.Text = string.Join("\n", lines);

        // The record holds the literal argument values: the rule is the user's own, so nothing is redacted here.
        DecisionRecord record = new(decision, RuleIdentity.Of(evaluation.Rule), evaluation.Options);
        outcome.Result["result"] = decision.Result.ToString();
        outcome.Result["decision"] = CommandOutcome.ParseJson(
            record.PrintJson(new DecisionJsonOptions(IncludeArgumentValues: true))
        );
    }
}
