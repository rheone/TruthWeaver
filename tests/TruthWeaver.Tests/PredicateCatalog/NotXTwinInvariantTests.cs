namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;

/// <summary>
/// The catalog rule "every positive predicate has a <c>NotX</c> twin" holds for the whole predicates package.
/// <see cref="NotXTwinChecker"/> is proven to fail on hand-written fixtures; the real catalog is checked against the
/// reviewed table in <see cref="NotXTwinTable"/>.
/// </summary>
public sealed class NotXTwinInvariantTests
{
    /// <summary>Gets the positive factory key of every pair in the real twin table.</summary>
    public static TheoryData<string> RealPairs => [.. NotXTwinTable.Pairs.Select(pair => pair.Positive)];

    /// <summary>Every predicate factory of the real catalog has a twin or is listed with a reason for having none.</summary>
    [Fact]
    public void CheckCoverage_RealCatalog_ReportsNoFailures_Test()
    {
        IReadOnlyList<string> failures = NotXTwinChecker.CheckCoverage(
            NotXTwinChecker.CatalogFactories(),
            NotXTwinTable.Entries
        );

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>Each real twin answers NOT of its positive form for True, False and Unknown selected values.</summary>
    /// <param name="positive">The positive factory key of the pair.</param>
    [Theory]
    [MemberData(nameof(RealPairs))]
    public async Task CheckPairAsync_RealCatalogPair_TwinIsTheKleeneComplement_Test(string positive)
    {
        TwinPair pair = NotXTwinTable.Pairs.Single(row => row.Positive == positive);

        IReadOnlyList<string> failures = await NotXTwinChecker.CheckPairAsync(pair, TestContext.Current.CancellationToken);

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A positive factory with no twin and no table entry is reported by name.</summary>
    [Fact]
    public void CheckCoverage_PositiveFactoryWithoutTwin_ReportsTheFactory_Test()
    {
        IReadOnlyList<string> failures = NotXTwinChecker.CheckCoverage(["FakePredicates.HasThing(String)"], []);

        string failure = Assert.Single(failures);
        Assert.Contains("'FakePredicates.HasThing(String)'", failure, StringComparison.Ordinal);
        Assert.Contains("no NotX twin", failure, StringComparison.Ordinal);
    }

    /// <summary>A table that pairs every factory, or lists it with a reason, passes.</summary>
    [Fact]
    public void CheckCoverage_EveryFactoryAccountedFor_ReportsNoFailures_Test()
    {
        IReadOnlyList<string> failures = NotXTwinChecker.CheckCoverage(
            ["Fake.HasThing(String)", "Fake.NotHasThing(String)", "Fake.Custom<TContext>"],
            [FakePair(KleeneTwin), new NoTwin("Fake.Custom<TContext>", "The host writes the test.")]
        );

        Assert.Empty(failures);
    }

    /// <summary>A table row that names a factory the catalog lacks is reported, so a removed twin is noticed.</summary>
    [Fact]
    public void CheckCoverage_RowNamesMissingTwin_ReportsTheRow_Test()
    {
        IReadOnlyList<string> failures = NotXTwinChecker.CheckCoverage(["Fake.HasThing(String)"], [FakePair(KleeneTwin)]);

        string failure = Assert.Single(failures);
        Assert.Contains("'Fake.NotHasThing(String)', which is not a predicate factory", failure, StringComparison.Ordinal);
    }

    /// <summary>A twin that is the K3 complement for True, False and Unknown selected values passes.</summary>
    [Fact]
    public async Task CheckPairAsync_KleeneComplementTwin_ReportsNoFailures_Test()
    {
        IReadOnlyList<string> failures = await NotXTwinChecker.CheckPairAsync(
            FakePair(KleeneTwin),
            TestContext.Current.CancellationToken
        );

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A twin that turns an Unknown selected value into a definite answer is reported.</summary>
    [Fact]
    public async Task CheckPairAsync_TwinMapsUnknownToDefinite_ReportsTheTwin_Test()
    {
        IReadOnlyList<string> failures = await NotXTwinChecker.CheckPairAsync(
            FakePair(UnknownToFalseTwin),
            TestContext.Current.CancellationToken
        );

        string failure = Assert.Single(failures);
        Assert.Contains("Fake.NotHasThing(String): for the Unknown probe it answers False", failure, StringComparison.Ordinal);
    }

    /// <summary>A twin that agrees with its positive form instead of negating it is reported.</summary>
    [Fact]
    public async Task CheckPairAsync_TwinIsNotNegated_ReportsTheTwin_Test()
    {
        IReadOnlyList<string> failures = await NotXTwinChecker.CheckPairAsync(
            FakePair(Positive),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, failures.Count);
        Assert.Contains(failures, failure => failure.Contains("for the True probe it answers True", StringComparison.Ordinal));
    }

    /// <summary>A missing probe with no reason is reported, so a row cannot skip the Unknown case silently.</summary>
    [Fact]
    public async Task CheckPairAsync_ProbeMissingWithoutReason_ReportsTheGap_Test()
    {
        IReadOnlyList<string> failures = await NotXTwinChecker.CheckPairAsync(
            FakePair(KleeneTwin) with
            {
                WhenUnknown = null,
            },
            TestContext.Current.CancellationToken
        );

        string failure = Assert.Single(failures);
        Assert.Contains("no Unknown probe", failure, StringComparison.Ordinal);
    }

    /// <summary>A hand-written pair over <see cref="TwinProbeContext.Text"/>: "yes" is True, other text False, null Unknown.</summary>
    private static TwinPair FakePair(Func<string?, TruthValue> twin)
    {
        return new TwinPair(
            "Fake.HasThing(String)",
            "Fake.NotHasThing(String)",
            name => Fake(name, Positive),
            name => Fake(name, twin),
            [],
            new TwinProbeContext(Text: "yes"),
            new TwinProbeContext(Text: "no"),
            new TwinProbeContext()
        );
    }

    private static (
        PredicateSchema Schema,
        Func<TwinProbeContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Fake(string name, Func<string?, TruthValue> answer)
    {
        return (
            PredicateSchema.NoArguments(name, name, "A fixture predicate."),
            (context, _, _) => ValueTask.FromResult(answer(context.Text))
        );
    }

    private static TruthValue Positive(string? text)
    {
        return text switch
        {
            null => TruthValue.Unknown,
            "yes" => TruthValue.True,
            _ => TruthValue.False,
        };
    }

    private static TruthValue KleeneTwin(string? text)
    {
        return text switch
        {
            null => TruthValue.Unknown,
            "yes" => TruthValue.False,
            _ => TruthValue.True,
        };
    }

    // The defect the rule exists to catch: a negation written as a boolean NOT turns a missing value into a definite answer.
    private static TruthValue UnknownToFalseTwin(string? text)
    {
        return text is null or "yes" ? TruthValue.False : TruthValue.True;
    }
}
