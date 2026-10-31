namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Two literals that compare equal must print the same text, so that a term identity, its memo key and its canonical
/// text never disagree about whether two terms are the same (ultra-review ticket 27). A decimal keeps no extra scale
/// and an instant keeps no written offset.
/// </summary>
public sealed class LiteralIdentityTests
{
    /// <summary>Two decimals that differ only in scale are one term: one memoized run, and the same text for both.</summary>
    [Fact]
    public async Task EvaluateAsync_DecimalsDifferingOnlyInScale_RunThePredicateOnceAndPrintTheSame_Test()
    {
        Probe probe = new();
        CompiledRule<RuleTestContext> rule = Compile("q(x: 1.0) AND q(x: 1.00)", probe);

        await rule.EvaluateAsync(new RuleTestContext(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["1"], probe.Decimals.Select(d => d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal("q(x: 1)", Compile("q(x: 1.00)", probe).CanonicalText);
    }

    /// <summary>
    /// The review's failure scenario: the same instant written at two offsets is one memoized term, so the predicate
    /// runs once, and the value it reads is the one the canonical text shows.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SameInstantAtTwoOffsets_RunsThePredicateOnceWithTheCanonicalOffset_Test()
    {
        Probe probe = new();
        CompiledRule<RuleTestContext> rule = Compile(
            "p(at: \"2024-01-01T00:00:00Z\") AND p(at: \"2024-01-01T02:00:00+02:00\")",
            probe
        );

        await rule.EvaluateAsync(new RuleTestContext(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([TimeSpan.Zero], probe.Offsets);
        Assert.DoesNotContain("+02:00", rule.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("+00:00", rule.CanonicalText, StringComparison.Ordinal);
    }

    private static CompiledRule<RuleTestContext> Compile(string text, Probe probe)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "p",
                    "p",
                    "Records the offset it reads.",
                    [new PredicateArgumentSchema("at", "An instant.", LiteralKind.DateTimeOffset)]
                ),
                (_, args, _) =>
                {
                    probe.Offsets.Add(args.GetDateTimeOffset("at").Offset);
                    return ValueTask.FromResult(TruthValue.True);
                }
            )
            .Add(
                new PredicateSchema(
                    "q",
                    "q",
                    "Records the decimal it reads.",
                    [new PredicateArgumentSchema("x", "A decimal.", LiteralKind.Decimal)]
                ),
                (_, args, _) =>
                {
                    probe.Decimals.Add(args.GetDecimal("x"));
                    return ValueTask.FromResult(TruthValue.True);
                }
            )
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(text).CompiledRule ?? throw new InvalidOperationException($"'{text}' did not compile.");
    }

    private sealed class Probe
    {
        public List<TimeSpan> Offsets { get; } = [];

        public List<decimal> Decimals { get; } = [];
    }
}
