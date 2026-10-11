namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;

public sealed class LiteralKindFuzzTests
{
    /// <summary>
    /// The public rule fuzzer finds no failure over predicates that take each literal kind, so the evaluator, the rewrites
    /// and both printed forms agree with Strong Kleene for calls with every kind of argument.
    /// </summary>
    [Fact]
    public async Task RunAsync_PredicateForEachLiteralKind_PassesEveryCheck_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (LiteralKind kind in Enum.GetValues<LiteralKind>())
        {
            builder.Add(
                new PredicateSchema(
                    $"takes{kind}",
                    $"Takes {kind}",
                    $"Takes one {kind} argument.",
                    [new PredicateArgumentSchema("value", $"A {kind} value.", kind)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.Unknown)
            );
        }

        RuleFuzzReport report = await RuleFuzzer.RunAsync(
            builder.Build(),
            seed: 20261009,
            new RuleFuzzerOptions { RuleCount = 300 },
            TestContext.Current.CancellationToken
        );

        report.ShouldPass();
        Assert.Equal(Enum.GetValues<LiteralKind>().Length, report.Terms.Count);
    }
}
