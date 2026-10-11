namespace TruthWeaver.Tests.DocExamples;

using System.Text;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Checks the runnable examples in a Markdown file (README.md, CONTEXT.md, docs/data-sources.md) against the real compiler and printers, so a
/// documented example that stops compiling, or documented output that stops matching, fails the build.
/// </summary>
/// <remarks>
/// <para>
/// A fenced block in a checked language (<c>text</c>, <c>json</c>, <c>yaml</c>, <c>mermaid</c>, <c>ebnf</c>) must be preceded
/// (ignoring blank lines) by a marker comment <c>&lt;!-- doctest:KIND ARGUMENT --&gt;</c>; other languages, including C#
/// fragments, are not checked. The kinds are:
/// </para>
/// <list type="bullet">
/// <item><c>rule ID</c>: DSL text that must compile; its canonical text is remembered under <c>ID</c>.</item>
/// <item><c>json ID</c>, <c>yaml ID</c>: a rule that must compile to the same canonical text as the <c>rule ID</c> example.</item>
/// <item><c>tree ID</c>, <c>mermaid ID</c>: documented output that must equal <c>PrintPlainText()</c> / <c>PrintMermaid()</c> of the <c>rule ID</c> example.</item>
/// <item><c>equation ID</c>, <c>legend ID</c>: documented output that must equal <c>PrintEquation()</c> / <c>PrintEquationWithLegend()</c> of the <c>rule ID</c> example; the option words <c>latex</c>, <c>ascii</c>, <c>cstyle</c>, <c>double-dollar</c>, <c>mathjax</c> and <c>simple</c> follow the ID.</item>
/// <item><c>ruletext ID</c>: documented output that must equal <c>PrintRuleText()</c> of the <c>rule ID</c> example; the option word <c>symbolic</c> follows the ID (<c>cstyle</c> is not valid here — <c>PrintRuleText</c> rejects it).</item>
/// <item>A rewrite word (<c>nnf</c>, <c>cnf</c>, <c>dnf</c>, <c>simplify</c>, <c>canonicalize</c>, <c>compress</c> or <c>primitives</c>) after the ID of a <c>tree</c>, <c>mermaid</c>, <c>equation</c>, <c>legend</c> or <c>ruletext</c> marker applies that rewrite first, so the block documents the rewritten rule. Several words apply in the order written.</item>
/// <item>An <c>equation</c> marker can stand above a display-math paragraph (a line that starts with <c>$$</c>) instead of a fence.</item>
/// <item><c>rewrite-diagnostics ID</c>: documented output that must equal the diagnostics of the rewrite named by the rewrite word; <c>cap=N</c> sets the <c>maxNodeCount</c> of an expanding rewrite.</item>
/// <item><c>metrics ID</c>: the Markdown table that follows must equal the depth and node count of the <c>rule ID</c> example and of each rewrite word that follows the ID.</item>
/// <item><c>diagnostics-dsl SOURCE</c>, <c>diagnostics-json SOURCE</c>, <c>diagnostics-yaml SOURCE</c>: documented output that must equal <c>FormatDiagnostics</c> for compiling the rest of the marker line as that format.</item>
/// <item><c>skip REASON</c>: the block is not runnable (a pseudo-grammar, a class diagram); the reason is mandatory.</item>
/// </list>
/// <para>Failures are returned, not thrown, so the checker can be shown to fail on a deliberately broken example.</para>
/// </remarks>
internal static partial class DocExampleChecker
{
    // The rewrite words a marker accepts, with the row label a metrics table shows for each.
    private static readonly Dictionary<string, string> RewriteLabels = new(StringComparer.Ordinal)
    {
        ["nnf"] = "NNF",
        ["cnf"] = "CNF",
        ["dnf"] = "DNF",
        ["simplify"] = "Simplify",
        ["canonicalize"] = "Canonicalize",
        ["compress"] = "CompressToDerived",
        ["primitives"] = "ExpandToPrimitives",
    };

    private static readonly string[] CheckedLanguages = ["text", "json", "yaml", "mermaid", "ebnf"];

    // The data source names the documentation's variable examples use; "user" carries the JSONPath validator.
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        BuildRegistry(),
        new CompilerOptions(
            DataSources: new DataSourceDeclarations
            {
                ["user"] = JsonQueryValidator.Instance,
                ["request"] = JsonQueryValidator.Instance,
                ["limits"] = JsonQueryValidator.Instance,
            }
        )
    );

    /// <summary>Walks up from the test binary to the directory that holds <c>TruthWeaver.slnx</c>.</summary>
    /// <returns>The repository root, where README.md and CONTEXT.md live.</returns>
    internal static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TruthWeaver.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("TruthWeaver.slnx was not found above " + AppContext.BaseDirectory);
    }

    /// <summary>Checks every example in <paramref name="markdown"/>.</summary>
    /// <param name="markdown">The Markdown text.</param>
    /// <param name="fileName">The file's name, used to locate failures.</param>
    /// <returns>One message per failing example; empty when every example holds.</returns>
    internal static async Task<IReadOnlyList<string>> CheckAsync(string markdown, string fileName)
    {
        List<string> failures = [];
        Dictionary<string, CompiledRule<RuleTestContext>> rules = [with(StringComparer.Ordinal)];
        string[] lines = [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];

        string? marker = null;
        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i++];
            Match fence = FenceRegex().Match(line);

            if (!fence.Success && marker is not null && line.StartsWith('|') && MetricsMarkerRegex().IsMatch(marker))
            {
                // A metrics marker is followed by a Markdown table, not a fence. The table is every consecutive line that starts with a pipe.
                int tableStart = i - 1;
                int tableEnd = tableStart;
                while (tableEnd < lines.Length && lines[tableEnd].StartsWith('|'))
                {
                    tableEnd++;
                }

                CheckMetricsTable(marker, string.Join('\n', lines[tableStart..tableEnd]), $"{fileName}:{i}", rules, failures);
                marker = null;
                i = tableEnd;
                continue;
            }

            if (!fence.Success && marker is not null && line.StartsWith("$$", StringComparison.Ordinal))
            {
                // A display-math paragraph (a line that starts with $$) after an equation marker is checked like a fenced block.
                // The paragraph is every consecutive non-blank line.
                int paragraphStart = i - 1;
                int paragraphEnd = paragraphStart;
                while (paragraphEnd < lines.Length && lines[paragraphEnd].Length > 0)
                {
                    paragraphEnd++;
                }

                await CheckBlockAsync(
                    marker,
                    "math",
                    string.Join('\n', lines[paragraphStart..paragraphEnd]),
                    $"{fileName}:{i}",
                    rules,
                    failures
                );
                marker = null;
                i = paragraphEnd;
                continue;
            }

            if (!fence.Success)
            {
                if (line.Length > 0)
                {
                    // A marker applies only to the fence that follows it; any other text between them breaks the association.
                    marker = MarkerRegex().IsMatch(line) ? line : null;
                }

                continue;
            }

            int openLine = i;
            int closing = Array.FindIndex(lines, i, l => l.StartsWith("```", StringComparison.Ordinal));
            int end = closing < 0 ? lines.Length : closing;
            string body = string.Join('\n', lines[i..end]);
            string language = fence.Groups[1].Value;
            if (Array.IndexOf(CheckedLanguages, language) >= 0)
            {
                await CheckBlockAsync(marker, language, body, $"{fileName}:{openLine}", rules, failures);
            }

            marker = null;
            i = end + 1;
        }

        return failures;
    }

    private static async Task CheckBlockAsync(
        string? markerLine,
        string language,
        string body,
        string location,
        Dictionary<string, CompiledRule<RuleTestContext>> rules,
        List<string> failures
    )
    {
        if (markerLine is null)
        {
            failures.Add(
                $"{location}: untagged {language} block. Add a doctest marker (see docs/doc-examples.md) or a skip marker with a reason."
            );
            return;
        }

        Match marker = MarkerRegex().Match(markerLine);
        string kind = marker.Groups[1].Value;
        string argument = marker.Groups[2].Value;
        string example = $"{location} (doctest:{kind} {argument})";

        switch (kind)
        {
            case "skip":
                if (argument.Length == 0)
                {
                    failures.Add($"{example}: skip needs a reason.");
                }

                break;
            case "rule":
                CompilationResult<RuleTestContext> compiled = Compiler.Compile(body);
                if (compiled.CompiledRule is null)
                {
                    failures.Add($"{example}: does not compile.{Environment.NewLine}{compiled.FormatDiagnostics(body)}");
                }
                else
                {
                    rules[argument] = compiled.CompiledRule;
                }

                break;
            case "json" or "yaml":
                CheckSameRule(kind, argument, body, example, rules, failures);
                break;
            case "tree" or "mermaid" or "equation" or "legend" or "ruletext" or "rewrite-diagnostics":
                // A mermaid marker may follow the rule id with option words: "shapes", "two-line", "symbolic", "cstyle" (the operator style of a tree or mermaid block) and "trace" (colors the nodes by an evaluation without data sources).
                // A ruletext marker takes "symbolic" too (PrintRuleText(OperatorStyle) rejects "cstyle"). An equation or legend marker
                // takes the dialect words "latex", "ascii", "cstyle", "double-dollar", "mathjax" and "simple".
                string[] words = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string ruleId = words.Length > 0 ? words[0] : string.Empty;
                if (!rules.TryGetValue(ruleId, out CompiledRule<RuleTestContext>? rule))
                {
                    failures.Add($"{example}: no earlier doctest:rule {ruleId}.");
                    break;
                }

                CompilationResult<RuleTestContext>? rewritten = Rewrite(rule, words);
                if (kind == "rewrite-diagnostics")
                {
                    if (rewritten is null)
                    {
                        failures.Add($"{example}: names no rewrite word.");
                    }
                    else
                    {
                        CompareOutput(rewritten.FormatDiagnostics(), body, example, failures);
                    }

                    break;
                }

                if (rewritten is not null)
                {
                    if (rewritten.CompiledRule is null)
                    {
                        failures.Add(
                            $"{example}: the rewrite did not produce a rule.{Environment.NewLine}{rewritten.FormatDiagnostics()}"
                        );
                        break;
                    }

                    rule = rewritten.CompiledRule;
                }

                OperatorStyle operatorStyle = OperatorStyleOf(words);
                MermaidOptions mermaidOptions = new()
                {
                    OperatorStyle = operatorStyle,
                    NodeShapes = words.Contains("shapes"),
                    TwoLineTermLabels = words.Contains("two-line"),
                };
                EquationOptions equationOptions = new()
                {
                    Dialect = EquationDialectOf(words),
                    LatexWrap = LatexWrapOf(words),
                    SimpleVariables = words.Contains("simple"),
                };
                string actualOutput;
                if (kind == "legend")
                {
                    EquationWithLegend withLegend = rule.PrintEquationWithLegend(equationOptions);
                    actualOutput = withLegend.Equation + "\n" + withLegend.LegendText;
                }
                else
                {
                    actualOutput = kind switch
                    {
                        "tree" => PlainTextTreePrinter.Print(rule.Outline(), operatorStyle),
                        "equation" => rule.PrintEquation(equationOptions),
                        "ruletext" => rule.PrintRuleText(operatorStyle: operatorStyle),
                        _ when words.Contains("trace") => rule.PrintMermaid(
                            await EvaluateWithoutDataSourcesAsync(rule),
                            mermaidOptions
                        ),
                        _ => rule.PrintMermaid(mermaidOptions),
                    };
                }

                CompareOutput(actualOutput, body, example, failures);
                break;
            case "diagnostics-dsl" or "diagnostics-json" or "diagnostics-yaml":
                CompilationResult<RuleTestContext> result = kind switch
                {
                    "diagnostics-dsl" => Compiler.Compile(argument),
                    "diagnostics-json" => Compiler.CompileJson(argument),
                    _ => Compiler.CompileYaml(argument),
                };
                CompareOutput(result.FormatDiagnostics(argument), body, example, failures);
                break;
            default:
                failures.Add($"{example}: unknown doctest kind '{kind}'.");
                break;
        }
    }

    /// <summary>
    /// Evaluates a rule with no service provider and no data sources, so a variable reference gives <c>Unknown</c> and a fault
    /// while a documentation predicate gives <c>True</c>. The trace of this decision is what the <c>trace</c> word draws.
    /// </summary>
    private static Task<Decision> EvaluateWithoutDataSourcesAsync(CompiledRule<RuleTestContext> rule)
    {
        return rule.EvaluateAsync(new RuleTestContext());
    }

    /// <summary>
    /// Applies the rewrites that a marker names with rewrite words (<c>nnf</c>, <c>cnf</c>, <c>dnf</c>, <c>simplify</c>,
    /// <c>canonicalize</c>, <c>compress</c> or <c>primitives</c>), in the order written. A <c>cap=N</c> word sets the
    /// <c>maxNodeCount</c> of the expanding rewrites.
    /// </summary>
    /// <returns>The rewrite result, or <see langword="null"/> when the marker names no rewrite.</returns>
    private static CompilationResult<RuleTestContext>? Rewrite(CompiledRule<RuleTestContext> rule, string[] words)
    {
        int? cap = null;
        foreach (string word in words)
        {
            if (word.StartsWith("cap=", StringComparison.Ordinal) && int.TryParse(word.AsSpan(4), out int parsed))
            {
                cap = parsed;
            }
        }

        // Several rewrite words apply in the order written, each to the rule the one before produced; a refused step ends the chain.
        CompilationResult<RuleTestContext>? result = null;
        CompiledRule<RuleTestContext> current = rule;
        foreach (string word in words.Where(word => RewriteLabels.ContainsKey(word)))
        {
            result = word switch
            {
                "nnf" => current.ToNnf(maxNodeCount: cap),
                "cnf" => current.ToCnf(maxNodeCount: cap),
                "dnf" => current.ToDnf(maxNodeCount: cap),
                "simplify" => current.Simplify(),
                "canonicalize" => current.Canonicalize(),
                "compress" => current.CompressToDerived(),
                _ => current.ExpandToPrimitives(cap),
            };
            if (result.CompiledRule is null)
            {
                return result;
            }

            current = result.CompiledRule;
        }

        return result;
    }

    /// <summary>
    /// Checks the Markdown table after a <c>metrics</c> marker. The table has one row for the rule and one row for each
    /// rewrite word, with the depth and node count that <c>RuleMetrics</c> reports.
    /// </summary>
    private static void CheckMetricsTable(
        string markerLine,
        string table,
        string location,
        Dictionary<string, CompiledRule<RuleTestContext>> rules,
        List<string> failures
    )
    {
        Match marker = MarkerRegex().Match(markerLine);
        string example = $"{location} (doctest:{marker.Groups[1].Value} {marker.Groups[2].Value})";
        string[] words = marker.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !rules.TryGetValue(words[0], out CompiledRule<RuleTestContext>? rule))
        {
            failures.Add($"{example}: no earlier doctest:rule {(words.Length == 0 ? string.Empty : words[0])}.");
            return;
        }

        StringBuilder builder = new();
        builder.Append("| Form | Depth | NodeCount |\n| --- | --- | --- |\n");
        builder.Append($"| Original | {rule.Metrics.Depth} | {rule.Metrics.NodeCount} |\n");
        foreach (string word in words.Skip(1))
        {
            CompilationResult<RuleTestContext>? rewritten = Rewrite(rule, [word]);
            if (rewritten?.CompiledRule is null)
            {
                failures.Add($"{example}: '{word}' is not a rewrite that produces a rule.");
                return;
            }

            RuleMetrics metrics = rewritten.CompiledRule.Metrics;
            builder.Append($"| {RewriteLabels[word]} | {metrics.Depth} | {metrics.NodeCount} |\n");
        }

        CompareOutput(builder.ToString(), table, example, failures);
    }

    private static OperatorStyle OperatorStyleOf(string[] words)
    {
        if (words.Contains("symbolic"))
        {
            return OperatorStyle.Symbolic;
        }

        return words.Contains("cstyle") ? OperatorStyle.CStyle : OperatorStyle.Word;
    }

    private static EquationDialect EquationDialectOf(string[] words)
    {
        if (words.Contains("latex"))
        {
            return EquationDialect.LaTeX;
        }

        if (words.Contains("ascii"))
        {
            return EquationDialect.AsciiMath;
        }

        return words.Contains("cstyle") ? EquationDialect.CStyle : EquationDialect.Unicode;
    }

    private static LatexWrapMode LatexWrapOf(string[] words)
    {
        if (words.Contains("double-dollar"))
        {
            return LatexWrapMode.DoubleDollar;
        }

        return words.Contains("mathjax") ? LatexWrapMode.MathJaxSafe : LatexWrapMode.None;
    }

    private static void CheckSameRule(
        string kind,
        string id,
        string body,
        string example,
        Dictionary<string, CompiledRule<RuleTestContext>> rules,
        List<string> failures
    )
    {
        if (!rules.TryGetValue(id, out CompiledRule<RuleTestContext>? expected))
        {
            failures.Add($"{example}: no earlier doctest:rule {id}.");
            return;
        }

        CompilationResult<RuleTestContext> result = kind == "json" ? Compiler.CompileJson(body) : Compiler.CompileYaml(body);
        if (result.CompiledRule is null)
        {
            failures.Add($"{example}: does not compile.{Environment.NewLine}{result.FormatDiagnostics(body)}");
        }
        else if (!string.Equals(result.CompiledRule.CanonicalText, expected.CanonicalText, StringComparison.Ordinal))
        {
            failures.Add(
                $"{example}: compiles to '{result.CompiledRule.CanonicalText}', not the rule's '{expected.CanonicalText}'."
            );
        }
    }

    private static void CompareOutput(string actual, string documented, string example, List<string> failures)
    {
        string expected = Normalize(documented);
        string current = Normalize(actual);
        if (!string.Equals(expected, current, StringComparison.Ordinal))
        {
            failures.Add(
                $"{example}: documented output is stale.{Environment.NewLine}--- documented{Environment.NewLine}{expected}{Environment.NewLine}--- actual{Environment.NewLine}{current}"
            );
        }
    }

    /// <summary>Compares line by line, ignoring line-ending style and trailing whitespace.</summary>
    private static string Normalize(string text)
    {
        StringBuilder builder = new();
        foreach (string line in text.Split('\n'))
        {
            builder.Append(line.TrimEnd()).Append('\n');
        }

        return builder.ToString().TrimEnd('\n');
    }

    /// <summary>The predicates the documentation's examples are written against, with the labels and defaults the documented output shows.</summary>
    private static PredicateRegistry<RuleTestContext> BuildRegistry()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        string[] flags =
        [
            "lovesPineapple",
            "isBanned",
            "isDineIn",
            "isTakeout",
            "approvedByAlice",
            "approvedByBob",
            "approvedByCarol",
            "isPrimaryReviewer",
            "isBackupReviewer",
            "isContractor",
            "hasSignedNda",
        ];
        foreach (string name in flags)
        {
            builder.Add(
                PredicateSchema.NoArguments(name, Label(name), $"The documentation's '{name}' predicate."),
                ConstantAsync
            );
        }

        builder.Add(
            new PredicateSchema(
                "hasTopping",
                "Has Topping",
                "Does the order include the given topping?",
                [new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String)]
            ),
            ConstantAsync
        );
        builder.Add(
            new PredicateSchema(
                "hasToppingAmount",
                "Has Topping Amount",
                "Does the order include the given topping at the given amount?",
                [
                    new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String),
                    new PredicateArgumentSchema("amount", "The amount requested.", LiteralKind.String),
                ]
            ),
            ConstantAsync
        );
        builder.Add(
            new PredicateSchema(
                "hasAnyTopping",
                "Has Any Topping",
                "Does the order include at least one of the given toppings?",
                [new PredicateArgumentSchema("toppings", "The toppings to check for.", LiteralKind.StringArray)]
            ),
            ConstantAsync
        );
        builder.Add(
            new PredicateSchema(
                "hasCrust",
                "Has Crust",
                "Is the crust the given one?",
                [
                    new PredicateArgumentSchema("crust", "The crust to check for.", LiteralKind.String),
                    new PredicateArgumentSchema(
                        "ignoreCase",
                        "Compare ignoring case.",
                        LiteralKind.Boolean,
                        false,
                        LiteralValue.OfBoolean(false)
                    ),
                    new PredicateArgumentSchema(
                        "trim",
                        "Trim before comparing.",
                        LiteralKind.Boolean,
                        false,
                        LiteralValue.OfBoolean(false)
                    ),
                ]
            ),
            ConstantAsync
        );
        builder.Add(
            new PredicateSchema(
                "ageAtLeast",
                "Age At Least",
                "Is the user at least the given age?",
                [new PredicateArgumentSchema("min", "The minimum age.", LiteralKind.Int64)]
            ),
            ConstantAsync
        );
        builder.Add(
            new PredicateSchema(
                "hasRole",
                "Has Role",
                "Does the user have the given role?",
                [new PredicateArgumentSchema("role", "The role to check for.", LiteralKind.String)]
            ),
            ConstantAsync
        );
        builder.Add(
            new PredicateSchema(
                "totalAtMost",
                "Total At Most",
                "Is the order total at most the given limit?",
                [new PredicateArgumentSchema("limit", "The highest total that is allowed.", LiteralKind.Int64)]
            ),
            ConstantAsync
        );
        return builder.Build();
    }

    private static ValueTask<TruthValue> ConstantAsync(RuleTestContext context, PredicateArguments args, CancellationToken ct)
    {
        return ValueTask.FromResult(TruthValue.True);
    }

    /// <summary>Turns <c>isDineIn</c> into <c>Is Dine In</c>, matching the label style of the documented output.</summary>
    private static string Label(string name)
    {
        string spaced = CamelBoundaryRegex().Replace(name, " $1");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    [GeneratedRegex("^```(\\w*)")]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"^<!--\s*doctest:(\S+)[ \t]*(.*?)\s*-->$")]
    private static partial Regex MarkerRegex();

    [GeneratedRegex(@"^<!--\s*doctest:metrics\b")]
    private static partial Regex MetricsMarkerRegex();

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex CamelBoundaryRegex();
}
