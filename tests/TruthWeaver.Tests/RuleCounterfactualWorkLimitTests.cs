namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="RuleCounterfactual"/> on a rule that has far more equally small change sets than the search can walk: the
/// search stops at its work limit and still returns one smallest change set, marked incomplete.
/// </summary>
public sealed class RuleCounterfactualWorkLimitTests
{
    private const int Clauses = 18;

    /// <summary>
    /// Eighteen clauses that each need exactly one of two terms to become true give 2^18 equally small change sets, and
    /// every route to the first of them has the same length. The search reaches its work limit before it completes
    /// any set, so it returns one smallest set and says the list is incomplete.
    /// </summary>
    [Fact]
    public void FindChanges_MoreEquallySmallPathsThanTheWorkLimit_ReturnsOneSmallestSetAndIsIncomplete_Test()
    {
        string[] names = [.. Enumerable.Range(0, 2 * Clauses).Select(i => $"t{i}")];
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string name in names)
        {
            builder.AddConstant(name, false);
        }

        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(
            builder.Build(),
            new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096)
        )
            .Compile(string.Join(" AND ", Enumerable.Range(0, Clauses).Select(i => $"(t{2 * i} XOR t{(2 * i) + 1})")))
            .GetRuleOrThrow();

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            names.ToDictionary(name => name, _ => TruthValue.False),
            new CounterfactualOptions(MaxAnalysisTerms: names.Length)
        );

        Assert.Equal(CounterfactualOutcome.Found, result.Outcome);
        Assert.False(result.IsComplete);
        ChangeSet set = Assert.Single(result.ChangeSets);
        Assert.Equal(Clauses, set.Count);
        Assert.All(set.Changes, change => Assert.Equal(TruthValue.True, change.To));
    }
}
