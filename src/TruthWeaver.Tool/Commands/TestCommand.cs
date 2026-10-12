namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using System.Text.Json.Nodes;
using TruthWeaver.Diagnostics;
using TruthWeaver.Testing;
using TruthWeaver.Tool.Core;

/// <summary>
/// <c>truthweaver test</c>: run the cases of a rule test file (YAML) over stand-in predicates with stub values, list each
/// case as passed or failed, and optionally write a JUnit XML report for a CI server.
/// </summary>
internal static class TestCommand
{
    /// <summary>Creates the command.</summary>
    /// <param name="console">The console.</param>
    /// <returns>The command.</returns>
    public static Command Create(CliConsole console)
    {
        Argument<string> input = new("file") { Description = "The rule test file (YAML)." };
        Option<string?> junit = new("--junit")
        {
            Description = "Also write the results as JUnit XML to this file, for a CI server.",
            HelpName = "file",
        };
        Option<string?> config = new("--config")
        {
            Description =
                "The configuration file. The default is the nearest truthweaver.json in the directory of the test file or a parent.",
            HelpName = "file",
        };
        Option<int?> maxDepth = RuleInputOptions.PositiveLimit("--max-depth", "The most nesting depth of a rule.");
        Option<int?> maxNodeCount = RuleInputOptions.PositiveLimit("--max-node-count", "The most nodes in a rule.");
        Option<bool> json = new("--json")
        {
            Description = "Write one JSON object with the result, the diagnostics and the exit code.",
        };

        Command command = new(
            "test",
            "Run the cases of a rule test file with stub values, and list each case as passed or failed."
        )
        {
            input,
            junit,
            config,
            maxDepth,
            maxNodeCount,
            json,
        };

        command.SetAction(
            async (parse, cancellationToken) =>
            {
                CommandOutcome outcome = new("test");
                OutputMode mode = parse.GetValue(json) ? OutputMode.Json : OutputMode.Text;
                string path = console.FullPath(parse.GetValue(input)!);

                ToolSettings settings = ResolveSettings(parse, console, path, config, maxDepth, maxNodeCount, outcome);
                if (outcome.HasErrors)
                {
                    return outcome.Write(console, mode);
                }

                if (!RuleTestFile.TryLoad(path, out RuleTestFile? file, out IReadOnlyList<RuleTestFileError> errors))
                {
                    outcome.Add(
                        path,
                        errors.Select(e =>
                            Diagnostic.Error(
                                ToolDiagnosticCodes.InvalidRuleTestFile,
                                FormatError(e),
                                SourceSpan.None,
                                path: e.Path
                            )
                        )
                    );
                    return outcome.Write(console, mode);
                }

                RuleTestReport report = await file.RunAsync(settings.ToCompilerOptions(), cancellationToken)
                    .ConfigureAwait(false);

                // The spans of a compile diagnostic point into the rule. An inline rule is an embedded rule at $.rule of the
                // test file, so its line and column count in the rule text (a rule tree as the reader writes it again).
                outcome.Add(
                    file.RuleFile is { } ruleFile
                        ? new DiagnosticDocument(ruleFile, file.RuleSource, null, report.RuleDiagnostics)
                        : new DiagnosticDocument(path, file.RuleSource, "$.rule", report.RuleDiagnostics)
                );
                outcome.Failed = !report.Passed;
                outcome.Text = report.ToString();
                WriteJsonResult(report, outcome);

                if (parse.GetValue(junit) is { } junitPath)
                {
                    string fullJUnitPath = console.FullPath(junitPath);
                    ToolResult<string> written = OutputWriter.WriteFile(fullJUnitPath, report.PrintJUnit());
                    outcome.Add(fullJUnitPath, written.Diagnostics);
                    outcome.Result["junit"] = fullJUnitPath;
                }

                return outcome.Write(console, mode);
            }
        );
        return command;
    }

    /// <summary>Resolves the compile limits: the options over the configuration file over the defaults.</summary>
    private static ToolSettings ResolveSettings(
        ParseResult parse,
        CliConsole console,
        string path,
        Option<string?> config,
        Option<int?> maxDepth,
        Option<int?> maxNodeCount,
        CommandOutcome outcome
    )
    {
        string? configPath = parse.GetValue(config) is { } explicitPath
            ? console.FullPath(explicitPath)
            : ToolConfigurationFile.Find(Path.GetDirectoryName(path) ?? console.WorkingDirectory);

        ToolConfiguration? fromFile = null;
        if (configPath is not null)
        {
            ToolResult<ToolConfiguration> loaded = ToolConfigurationFile.Load(configPath);
            outcome.Add(configPath, loaded.Diagnostics);
            fromFile = loaded.Value;
        }

        ToolConfiguration commandLine = new()
        {
            MaxDepth = parse.GetValue(maxDepth),
            MaxNodeCount = parse.GetValue(maxNodeCount),
        };
        return ToolSettings.Resolve(fromFile, commandLine);
    }

    /// <summary>
    /// Gives the message of a test file problem with its place in the file, when it has one. The path in the file goes in
    /// the path of the diagnostic, so it is not in the message as <see cref="RuleTestFileError.ToString"/> puts it.
    /// </summary>
    private static string FormatError(RuleTestFileError error)
    {
        return error.Line > 0 ? $"Line {error.Line}, column {error.Column}: {error.Message}" : error.Message;
    }

    /// <summary>Puts the counts and each case in the JSON result.</summary>
    private static void WriteJsonResult(RuleTestReport report, CommandOutcome outcome)
    {
        JsonArray cases = [];
        foreach (RuleTestResult result in report.Results)
        {
            JsonNode item = new JsonObject
            {
                ["name"] = result.Case.Name,
                ["passed"] = result.Passed,
                ["expected"] = result.Case.Expect.Result.ToString(),
                ["result"] = result.Decision?.Result.ToString(),
                ["faults"] = result.Decision?.Faults.Count,
                ["failures"] = new JsonArray([.. result.Failures.Select(f => (JsonNode?)f)]),
            };
            cases.Add(item);
        }

        outcome.Result["name"] = report.Name;
        outcome.Result["passed"] = report.Results.Count - report.Failures.Count;
        outcome.Result["failed"] = report.Failures.Count;
        outcome.Result["cases"] = cases;
    }
}
