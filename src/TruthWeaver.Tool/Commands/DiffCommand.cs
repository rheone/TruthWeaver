namespace TruthWeaver.Tool.Commands;

using System.CommandLine;
using System.Text.Json.Nodes;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// <c>truthweaver diff</c>: the structural changes between two rules, whether they are equivalent under Strong K3, and
/// whether the after rule is looser or tighter, with an example assignment for each side.
/// </summary>
internal static class DiffCommand
{
    /// <summary>Creates the command.</summary>
    /// <param name="console">The console.</param>
    /// <returns>The command.</returns>
    public static Command Create(CliConsole console)
    {
        Argument<string> before = new("before")
        {
            Description = "The rule to compare from, such as the current version. - is standard input.",
        };
        Argument<string> after = new("after")
        {
            Description = "The rule to compare to, such as the proposed version. - is standard input.",
        };
        RuleInputOptions options = new();

        Command command = new(
            "diff",
            "Compare two rules: the changes, Strong K3 equivalence, and whether the after rule is looser or tighter."
        )
        {
            before,
            after,
        };
        options.AddTo(command);
        command.Validators.Add(result =>
        {
            if (result.GetValue(before) == SourceReader.StandardInput && result.GetValue(after) == SourceReader.StandardInput)
            {
                result.AddError("Only one of the two rules can come from standard input.");
            }
        });

        command.SetAction(parse =>
        {
            CommandOutcome outcome = new("diff");
            string beforePath = parse.GetValue(before)!;

            // The configuration is searched from the rule that is a file: standard input has no directory.
            string configInput = beforePath == SourceReader.StandardInput ? parse.GetValue(after)! : beforePath;
            ToolSettings settings = options.ResolveSettings(parse, console, configInput, outcome);
            if (outcome.HasErrors)
            {
                return outcome.Write(console, options.ModeOf(parse));
            }

            CompiledRule<StubAssignment>? beforeRule = Compile(
                options.Load(parse, console, beforePath, outcome),
                settings,
                outcome
            );
            CompiledRule<StubAssignment>? afterRule = Compile(
                options.Load(parse, console, parse.GetValue(after)!, outcome),
                settings,
                outcome
            );
            if (beforeRule is not null && afterRule is not null)
            {
                Compare(beforeRule, afterRule, settings, outcome);
            }

            return outcome.Write(console, options.ModeOf(parse));
        });
        return command;
    }

    private static CompiledRule<StubAssignment>? Compile(LoadedRule? rule, ToolSettings settings, CommandOutcome outcome)
    {
        if (rule is null)
        {
            return null;
        }

        CompilationResult<StubAssignment> compiled = RuleConverter.Compile(rule, settings);
        bool succeeded = compiled.TryGetRule(
            out CompiledRule<StubAssignment>? value,
            out EquatableArray<Diagnostic> diagnostics
        );
        outcome.Add(rule, diagnostics);
        return succeeded ? value : null;
    }

    private static void Compare(
        CompiledRule<StubAssignment> before,
        CompiledRule<StubAssignment> after,
        ToolSettings settings,
        CommandOutcome outcome
    )
    {
        RuleDiffResult diff = RuleDiff.Compare(before, after, settings.MaxAnalysisTerms);
        RuleRelationResult relation = RuleImplication.Compare(before, after, settings.MaxAnalysisTerms);

        // The exit code follows Strong K3 equivalence: 0 only when the two rules give the same value for every assignment.
        outcome.Failed = diff.PreservesMeaning != true;

        string equivalence = diff.PreservesMeaning switch
        {
            true => "equivalent",
            false => "not equivalent",
            null => "undecided",
        };
        string relationName = relation.Relation.ToString().ToLowerInvariant();
        List<string> lines =
        [
            RuleDiffPrinter.Print(diff),
            $"Strong K3 equivalence: {equivalence}",
            $"Relation: {relationName}",
        ];
        if (relation.SatisfiesOnlyBefore is { } onlyBefore)
        {
            lines.Add("True only for the before rule: " + Assignment(onlyBefore));
        }

        if (relation.SatisfiesOnlyAfter is { } onlyAfter)
        {
            lines.Add("True only for the after rule: " + Assignment(onlyAfter));
        }

        if (relation.Reason is { } reason)
        {
            lines.Add("Reason: " + reason);
        }

        outcome.Text = string.Join("\n", lines);
        outcome.Result["equivalent"] = diff.PreservesMeaning;
        outcome.Result["relation"] = relationName;
        outcome.Result["changes"] = new JsonArray([.. diff.Entries.Select(Change)]);
        outcome.Result["satisfiesOnlyBefore"] = AssignmentJson(relation.SatisfiesOnlyBefore);
        outcome.Result["satisfiesOnlyAfter"] = AssignmentJson(relation.SatisfiesOnlyAfter);
        outcome.Result["reason"] = relation.Reason;
    }

    private static string Assignment(IReadOnlyDictionary<string, TruthValue> assignment)
    {
        return string.Join(", ", assignment.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key} = {a.Value}"));
    }

    private static JsonObject? AssignmentJson(IReadOnlyDictionary<string, TruthValue>? assignment)
    {
        if (assignment is null)
        {
            return null;
        }

        JsonObject json = [];
        foreach ((string term, TruthValue value) in assignment.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            json[term] = value.ToString();
        }

        return json;
    }

    private static JsonObject Change(RuleDiffEntry entry)
    {
        return new JsonObject
        {
            ["kind"] = entry.Kind.ToString().ToLowerInvariant(),
            ["path"] = new JsonArray([.. entry.Path.Select(index => (JsonNode)index)]),
            ["before"] = NodeText(entry.Before),
            ["after"] = NodeText(entry.After),
        };
    }

    private static string? NodeText(OutlineNode? node)
    {
        return node switch
        {
            null => null,
            { ArgumentText: { } arguments } => $"{node.Label}({arguments})",
            _ => node.Label,
        };
    }
}
