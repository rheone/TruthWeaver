namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 17 (k3-hardening 11, finding 1): the analyzer's dual-rail BDD and the walking evaluator compute the
/// same K3 value from independently written code (<c>Analyzer.Build</c>'s rails versus <c>Evaluator</c>'s Kleene
/// truth tables). Before this ticket the two were compared only at the tautology/contradiction extremes
/// (<see cref="AnalyzerTests.Compile_GeneratedRules_AnalyzerVerdictsAgreeWithK3OracleOverAllAssignments_Test"/>);
/// this pins them together for every generated assignment.
/// </summary>
public sealed class EvaluatorAnalyzerPinningTests
{
    private static readonly string[] TermNames = ["a", "b", "c"];

    /// <summary>
    /// Over many generated rules and every <c>{True, False, Unknown}</c> assignment of three terms, the
    /// evaluator's result agrees with the value read from the analyzer's own dual-rail BDD
    /// (<see cref="Analyzer.ValueAt"/>) for the same compiled tree. Every sub-expression is checked, not only
    /// the root of the generated rule, so every operator's evaluator path is pinned to its analyzer rail.
    /// </summary>
    [Fact]
    public async Task Evaluate_GeneratedRules_AgreesWithAnalyzerRailsOverAllAssignments_Test()
    {
        Random random = new(20261004);
        List<string> disagreements = [];
        int checkedRules = 0;

        for (int i = 0; i < 150; i++)
        {
            foreach (GeneratedRule node in K3RuleGenerator.Subtrees(K3RuleGenerator.GenerateRule(random, depth: 3)))
            {
                TruthValue[] state = new TruthValue[3];
                RuleCompiler<RuleTestContext> compiler = new(
                    PredicateRegistry<RuleTestContext>
                        .CreateBuilder()
                        .AddReadable("a", () => state[0])
                        .AddReadable("b", () => state[1])
                        .AddReadable("c", () => state[2])
                        .Build()
                );

                CompilationResult<RuleTestContext> result = compiler.Compile(node.Text);
                CompiledRule<RuleTestContext>? compiledRule = result.CompiledRule;
                if (compiledRule is null)
                {
                    // Out-of-range thresholds (TRE0008) are authoring errors, not evaluator/analyzer input.
                    continue;
                }

                foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
                {
                    assignment.CopyTo(state, 0);
                    checkedRules++;

                    Dictionary<string, TruthValue> byName = new()
                    {
                        [TermNames[0]] = assignment[0],
                        [TermNames[1]] = assignment[1],
                        [TermNames[2]] = assignment[2],
                    };
                    TruthValue analyzerValue = Analyzer.ValueAt(compiledRule.Root, byName);

                    Decision decision = await compiledRule.EvaluateAsync(
                        new RuleTestContext(),
                        EmptyServiceProvider.Instance,
                        cancellationToken: TestContext.Current.CancellationToken
                    );

                    if (decision.Result != analyzerValue)
                    {
                        disagreements.Add(
                            $"{node.Text} @ [{string.Join(", ", assignment)}]: evaluator={decision.Result}, analyzer={analyzerValue}"
                        );
                    }
                }
            }
        }

        Assert.Empty(disagreements);

        // Guard against a vacuous pass: the sample must contain plenty of checked (rule, assignment) pairs.
        Assert.True(checkedRules > 10_000, $"only {checkedRules} (rule, assignment) pairs were checked");
    }
}
