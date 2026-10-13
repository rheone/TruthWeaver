namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// <see cref="RuleCoverage"/>: which terms and branches of a rule decided the outcome over a corpus of decisions. Each
/// context is a string such as <c>"a=T,b=F,c=X"</c>: <c>T</c>, <c>F</c> and <c>U</c> are the answer of the predicate,
/// <c>X</c> makes it throw (a fault), and a missing name answers <c>Unknown</c>.
/// </summary>
public sealed class RuleCoverageTests
{
    private static readonly string[] Names = ["a", "b", "c", "d"];

    /// <summary>Every term gets its evaluated count, its True, False and Unknown counts and the number of decisions it decided.</summary>
    [Fact]
    public async Task FromDecisions_Corpus_CountsValuesAndDecisionsPerTerm_Test()
    {
        CompiledRule<string> rule = Compile("a AND b");
        Decision[] decisions = await EvaluateAsync(rule, "a=T,b=T", "a=F,b=T", "a=T,b=U");

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        Assert.Equal(3, coverage.DecisionCount);
        Assert.Equal((1, 1, 1), (coverage.TrueCount, coverage.FalseCount, coverage.UnknownCount));
        Assert.Equal([new TermCoverage("a", 3, 2, 1, 0, 0, 3), new TermCoverage("b", 2, 1, 0, 1, 0, 2)], coverage.Terms);
    }

    /// <summary>A term absorbed by another (<c>a OR (a AND b)</c>) never decides the outcome, and its branch is listed under its operator.</summary>
    [Fact]
    public async Task FromDecisions_AbsorbedTerm_IsListedAsNeverDecidedUnderItsOperator_Test()
    {
        CompiledRule<string> rule = Compile("a OR (a AND b)");
        Decision[] decisions = await EvaluateAsync(
            rule,
            EvaluationMode.Exhaustive,
            [.. from a in "TFU" from b in "TFU" select $"a={a},b={b}"]
        );

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        TermCoverage termB = Assert.Single(coverage.Terms, term => term.Term == "b");
        Assert.True(termB.NeverDecided);
        Assert.Equal(9, termB.EvaluatedCount);
        NodeCoverage and = Assert.Single(coverage.OperatorsWithUndecidedOperands);
        Assert.Equal("$.operands[1]", and.Path);
        NodeCoverage undecided = Assert.Single(and.UndecidedOperands);
        Assert.Equal(("$.operands[1].operands[1]", "b"), (undecided.Path, undecided.Text));
        Assert.False(Assert.Single(coverage.Terms, term => term.Term == "a").NeverDecided);
    }

    /// <summary>A term that answered Unknown in every decision is marked always Unknown.</summary>
    [Fact]
    public async Task FromDecisions_TermAlwaysUnknown_IsMarkedAlwaysUnknown_Test()
    {
        CompiledRule<string> rule = Compile("a OR c");
        Decision[] decisions = await EvaluateAsync(rule, "a=F,c=U", "a=F");

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        Assert.True(Assert.Single(coverage.Terms, term => term.Term == "c").AlwaysUnknown);
        Assert.False(Assert.Single(coverage.Terms, term => term.Term == "a").AlwaysUnknown);
    }

    /// <summary>A term whose predicate throws is counted as Unknown and as a fault for that decision.</summary>
    [Fact]
    public async Task FromDecisions_FaultingTerm_CountsTheFault_Test()
    {
        CompiledRule<string> rule = Compile("a AND b");
        Decision[] decisions = await EvaluateAsync(rule, "a=T,b=X", "a=T,b=T");

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        Assert.Equal(new TermCoverage("b", 2, 1, 0, 1, 1, 2), Assert.Single(coverage.Terms, term => term.Term == "b"));
    }

    /// <summary>An operand skipped by short-circuiting counts as not evaluated and does not decide.</summary>
    [Fact]
    public async Task FromDecisions_ShortCircuitedOperand_CountsAsNotEvaluated_Test()
    {
        CompiledRule<string> rule = Compile("a AND b");
        Decision[] decisions = await EvaluateAsync(rule, "a=F,b=T", "a=F,b=F");

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        NodeCoverage b = Assert.Single(coverage.Nodes, node => node.Path == "$.operands[1]");
        Assert.Equal((0, 2, 0), (b.EvaluatedCount, b.NotEvaluatedCount, b.DecidedCount));
        Assert.Equal(new TermCoverage("b", 0, 0, 0, 0, 0, 0), Assert.Single(coverage.Terms, term => term.Term == "b"));
    }

    /// <summary>Coverage from supplied decisions does not call any predicate again.</summary>
    [Fact]
    public async Task FromDecisions_SuppliedDecisions_DoesNotEvaluateAgain_Test()
    {
        int calls = 0;
        CompiledRule<string> rule = Compile("a AND b", () => Interlocked.Increment(ref calls));
        Decision[] decisions = await EvaluateAsync(rule, "a=T,b=T", "a=T,b=F");
        int callsAfterEvaluation = calls;

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        Assert.Equal(2, coverage.DecisionCount);
        Assert.Equal(callsAfterEvaluation, calls);
    }

    /// <summary>The asynchronous form reads a stream of decisions, such as the output of <c>EvaluateManyAsync</c>.</summary>
    [Fact]
    public async Task FromDecisionsAsync_StreamFromEvaluateMany_GivesTheSameCoverageAsFromDecisions_Test()
    {
        CompiledRule<string> rule = Compile("a AND (b OR c)");
        string[] contexts = ["a=T,b=T", "a=T,b=F,c=T", "a=F", "a=T,b=F,c=U"];

        RuleCoverage streamed = await RuleCoverage.FromDecisionsAsync(
            rule,
            rule.EvaluateManyAsync(contexts, cancellationToken: TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken
        );

        RuleCoverage supplied = RuleCoverage.FromDecisions(rule, await EvaluateAsync(rule, contexts));
        Assert.Equal(supplied.Terms, streamed.Terms);
        Assert.Equal(supplied.Nodes, streamed.Nodes);
    }

    /// <summary>A decision made by a different rule is rejected, because its trace does not match the rule.</summary>
    [Fact]
    public async Task FromDecisions_DecisionOfAnotherRule_ThrowsArgumentException_Test()
    {
        CompiledRule<string> rule = Compile("a AND b");
        Decision[] decisions = await EvaluateAsync(Compile("a OR (b AND c)"), "a=F,b=T,c=T");

        Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, decisions));
    }

    /// <summary>With no decisions every count is zero and every term is listed as never decided.</summary>
    [Fact]
    public void FromDecisions_NoDecisions_ListsEveryTermAsNeverDecided_Test()
    {
        CompiledRule<string> rule = Compile("a OR b");

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, []);

        Assert.Equal(0, coverage.DecisionCount);
        Assert.All(coverage.Terms, term => Assert.True(term.NeverDecided));
        Assert.False(coverage.Terms[0].AlwaysUnknown);
    }

    /// <summary>
    /// For every operator and every assignment, the coverage accepts the decisions of both evaluation modes (its
    /// recomputation of each operator value agrees with the evaluator). In exhaustive mode, where nothing is skipped, a
    /// term decides exactly when another value of it changes the result of a new evaluation.
    /// </summary>
    [Theory]
    [InlineData("NOT a")]
    [InlineData("a AND b")]
    [InlineData("a OR b")]
    [InlineData("a XOR b")]
    [InlineData("a EQUIVALENT b")]
    [InlineData("a IMPLIES b")]
    [InlineData("a NAND b")]
    [InlineData("a NOR b")]
    [InlineData("PARITY(a, b, c)")]
    [InlineData("AtLeast(2, a, b, c)")]
    [InlineData("AtMost(1, a, b, c)")]
    [InlineData("Exactly(1, a, b, c)")]
    [InlineData("GreaterThan(1, a, b, c)")]
    [InlineData("LessThan(2, a, b, c)")]
    [InlineData("ExactlyOne(a, b, c)")]
    [InlineData("ANY(a, b, c)")]
    [InlineData("ALL(a, b, c)")]
    [InlineData("NONE(a, b, c)")]
    [InlineData("BETWEEN(1, 2, a, b, c)")]
    [InlineData("If(a, b, c)")]
    [InlineData("COALESCE(a, b, c)")]
    [InlineData("IsTrue(a)")]
    [InlineData("IsFalse(a)")]
    [InlineData("IsUnknown(a)")]
    [InlineData("IsKnown(a)")]
    [InlineData("a AND True")]
    [InlineData("(a OR b) AND NOT (b AND c)")]
    public async Task FromDecisions_EveryOperatorAndAssignment_DecidedMatchesAChangedResult_Test(string text)
    {
        CompiledRule<string> rule = Compile(text);
        string[] contexts = [.. from a in "TFU" from b in "TFU" from c in "TFU" select $"a={a},b={b},c={c}"];

        RuleCoverage shortCircuit = RuleCoverage.FromDecisions(rule, await EvaluateAsync(rule, contexts));
        Decision[] decisions = await EvaluateAsync(rule, EvaluationMode.Exhaustive, contexts);

        Assert.Equal(contexts.Length, shortCircuit.DecisionCount);
        for (int i = 0; i < contexts.Length; i++)
        {
            RuleCoverage single = RuleCoverage.FromDecisions(rule, [decisions[i]]);
            foreach (TermCoverage term in single.Terms)
            {
                bool changes = await AnyOtherValueChangesResultAsync(rule, contexts[i], term.Term, decisions[i].Result);
                Assert.True(
                    changes == (term.DecidedCount == 1),
                    $"{text} {contexts[i]}: '{term.Term}' decided {term.DecidedCount}, another value changes the result: {changes}."
                );
            }
        }
    }

    /// <summary>The Markdown report has the term table, the summary lists and the branches that never decided.</summary>
    [Fact]
    public async Task PrintMarkdown_Coverage_WritesTheTermTableAndTheUndecidedBranches_Test()
    {
        CompiledRule<string> rule = Compile("a OR (a AND b)");
        Decision[] decisions = await EvaluateAsync(rule, EvaluationMode.Exhaustive, "a=T,b=T", "a=F,b=X", "a=U,b=U");
        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        string markdown = coverage.PrintMarkdown();

        Assert.Equal(
            """
            # Rule coverage

            Rule: `a OR (a AND b)`

            Decisions: 3 (True 1, False 1, Unknown 1)

            ## Terms

            | Term | Evaluated | True | False | Unknown | Faults | Decided |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: |
            | `a` | 3 | 1 | 1 | 1 | 0 | 3 |
            | `b` | 3 | 1 | 0 | 2 | 1 | 0 |

            Never decided: `b`

            Always Unknown: none

            Faulted: `b`

            ## Branches that never decided

            | Operator | Operator path | Branch | Branch path |
            | --- | --- | --- | --- |
            | `AND` | `$.operands[1]` | `b` | `$.operands[1].operands[1]` |

            """.ReplaceLineEndings("\n"),
            markdown
        );
    }

    /// <summary>The JSON report has the counts per term and per node, and the undecided branches per operator.</summary>
    [Fact]
    public async Task PrintJson_Coverage_WritesTermsNodesAndUndecidedBranches_Test()
    {
        CompiledRule<string> rule = Compile("a OR (a AND b)");
        Decision[] decisions = await EvaluateAsync(rule, EvaluationMode.Exhaustive, "a=T,b=T", "a=F,b=X");
        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        using JsonDocument json = JsonDocument.Parse(coverage.PrintJson());

        JsonElement root = json.RootElement;
        Assert.Equal("a OR (a AND b)", root.GetProperty("rule").GetString());
        Assert.Equal(2, root.GetProperty("decisions").GetInt32());
        JsonElement b = root.GetProperty("terms")[1];
        Assert.Equal("b", b.GetProperty("term").GetString());
        Assert.Equal(1, b.GetProperty("faults").GetInt32());
        Assert.True(b.GetProperty("neverDecided").GetBoolean());
        Assert.Equal(5, root.GetProperty("nodes").GetArrayLength());
        JsonElement undecided = root.GetProperty("undecidedBranches");
        Assert.Equal(1, undecided.GetArrayLength());
        Assert.Equal("$.operands[1]", undecided[0].GetProperty("path").GetString());
        Assert.Equal(1, undecided[0].GetProperty("branches").GetArrayLength());
        Assert.Equal("$.operands[1].operands[1]", undecided[0].GetProperty("branches")[0].GetString());
    }

    /// <summary>The <see cref="TextWriter"/> forms write the same text as the string forms.</summary>
    [Fact]
    public async Task PrintMarkdownAndPrintJson_TextWriter_WriteTheSameTextAsTheStringForms_Test()
    {
        CompiledRule<string> rule = Compile("a AND b");
        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, await EvaluateAsync(rule, "a=T,b=F"));
        await using StringWriter markdown = new();
        await using StringWriter json = new();

        coverage.PrintMarkdown(markdown);
        coverage.PrintJson(json);

        Assert.Equal((coverage.PrintMarkdown(), coverage.PrintJson()), (markdown.ToString(), json.ToString()));
    }

    /// <summary>Null arguments are rejected.</summary>
    [Fact]
    public void FromDecisions_NullArguments_ThrowArgumentNull_Test()
    {
        CompiledRule<string> rule = Compile("a");

        Assert.Throws<ArgumentNullException>(() => RuleCoverage.FromDecisions<string>(null!, []));
        Assert.Throws<ArgumentNullException>(() => RuleCoverage.FromDecisions(rule, null!));
        Assert.Throws<ArgumentNullException>(() => RuleCoverage.FromDecisions(rule, [null!]));
    }

    private static CompiledRule<string> Compile(string text, Action? onCall = null)
    {
        PredicateRegistryBuilder<string> builder = PredicateRegistry<string>.CreateBuilder();
        foreach (string name in Names)
        {
            builder.Add(
                PredicateSchema.NoArguments(name, name, $"Reads '{name}' from the context text."),
                (context, _, _) =>
                {
                    onCall?.Invoke();
                    return ValueTask.FromResult(Read(context, name));
                }
            );
        }

        CompilationResult<string> result = new RuleCompiler<string>(builder.Build()).Compile(text);
        return result.CompiledRule ?? throw new InvalidOperationException(result.FormatDiagnostics(text));
    }

    private static TruthValue Read(string context, string name)
    {
        foreach (string pair in context.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=');
            if (parts[0] == name)
            {
                return parts[1] switch
                {
                    "T" => TruthValue.True,
                    "F" => TruthValue.False,
                    "U" => TruthValue.Unknown,
                    _ => throw new InvalidOperationException($"'{name}' faulted."),
                };
            }
        }

        return TruthValue.Unknown;
    }

    private static Task<Decision[]> EvaluateAsync(CompiledRule<string> rule, params string[] contexts)
    {
        return EvaluateAsync(rule, EvaluationMode.ShortCircuit, contexts);
    }

    private static async Task<Decision[]> EvaluateAsync(
        CompiledRule<string> rule,
        EvaluationMode mode,
        params string[] contexts
    )
    {
        return await rule.EvaluateManyAsync(
                contexts,
                options: new EvaluationOptions(Mode: mode),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<bool> AnyOtherValueChangesResultAsync(
        CompiledRule<string> rule,
        string context,
        string term,
        TruthValue result
    )
    {
        foreach (char other in "TFU")
        {
            string changed = string.Join(
                ',',
                context
                    .Split(',')
                    .Select(pair => pair.StartsWith(term + "=", StringComparison.Ordinal) ? $"{term}={other}" : pair)
            );
            Decision decision = await rule.EvaluateAsync(changed, cancellationToken: TestContext.Current.CancellationToken);
            if (decision.Result != result)
            {
                return true;
            }
        }

        return false;
    }
}
