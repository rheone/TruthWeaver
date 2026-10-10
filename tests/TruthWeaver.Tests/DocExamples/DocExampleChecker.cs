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
/// <item><c>diagnostics-dsl SOURCE</c>, <c>diagnostics-json SOURCE</c>, <c>diagnostics-yaml SOURCE</c>: documented output that must equal <c>FormatDiagnostics</c> for compiling the rest of the marker line as that format.</item>
/// <item><c>skip REASON</c>: the block is not runnable (a pseudo-grammar, a class diagram); the reason is mandatory.</item>
/// </list>
/// <para>Failures are returned, not thrown, so the checker can be shown to fail on a deliberately broken example.</para>
/// </remarks>
internal static partial class DocExampleChecker
{
    private static readonly string[] CheckedLanguages = ["text", "json", "yaml", "mermaid", "ebnf"];

    // The data source names the documentation's variable examples use; "user" carries the JSONPath validator.
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        BuildRegistry(),
        new CompilerOptions(
            DataSources: new DataSourceDeclarations
            {
                ["user"] = JsonQueryValidator.Instance,
                ["request"] = JsonQueryValidator.Instance,
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
    internal static IReadOnlyList<string> Check(string markdown, string fileName)
    {
        List<string> failures = [];
        Dictionary<string, CompiledRule<RuleTestContext>> rules = new(StringComparer.Ordinal);
        string[] lines = [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];

        string? marker = null;
        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i++];
            Match fence = FenceRegex().Match(line);
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
                CheckBlock(marker, language, body, $"{fileName}:{openLine}", rules, failures);
            }

            marker = null;
            i = end + 1;
        }

        return failures;
    }

    private static void CheckBlock(
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
            case "tree" or "mermaid":
                // A mermaid marker may follow the rule id with option words: "shapes" and "two-line".
                string[] words = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string ruleId = words.Length > 0 ? words[0] : string.Empty;
                if (!rules.TryGetValue(ruleId, out CompiledRule<RuleTestContext>? rule))
                {
                    failures.Add($"{example}: no earlier doctest:rule {ruleId}.");
                    break;
                }

                MermaidOptions mermaidOptions = new()
                {
                    NodeShapes = words.Contains("shapes"),
                    TwoLineTermLabels = words.Contains("two-line"),
                };
                CompareOutput(
                    kind == "tree" ? rule.PrintPlainText() : rule.PrintMermaid(mermaidOptions),
                    body,
                    example,
                    failures
                );
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
                        LiteralValue.OfBoolean(true)
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

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex CamelBoundaryRegex();
}
