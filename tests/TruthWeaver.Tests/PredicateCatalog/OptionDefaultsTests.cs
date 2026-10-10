namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;

/// <summary>
/// The option-default conventions of <c>docs/predicate-conventions.md</c> hold for every predicate schema in the built-in
/// catalog. <see cref="OptionDefaultsChecker"/> is proven to fail on hand-written schemas; the real catalog is read
/// from the factories of <see cref="NotXTwinTable"/>.
/// </summary>
public sealed class OptionDefaultsTests
{
    /// <summary>
    /// Every optional <c>ignoreCase</c> or <c>trim</c> argument of the catalog defaults to <c>false</c>, and every optional
    /// Boolean <c>include*</c> argument has a default.
    /// </summary>
    [Fact]
    public void Check_RealCatalog_ReportsNoFailures_Test()
    {
        IEnumerable<(string Factory, PredicateSchema Schema)> schemas = NotXTwinTable.Pairs.SelectMany(pair =>
            new[] { (pair.Positive, pair.PositiveFactory("p").Schema), (pair.Twin, pair.TwinFactory("t").Schema) }
        );

        IReadOnlyList<string> failures = OptionDefaultsChecker.Check(schemas);

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The catalog contains the predicates that carry the options, so the walk above is not vacuous.</summary>
    [Fact]
    public void Check_RealCatalog_CoversThePredicatesThatCarryOptions_Test()
    {
        string[] factories = [.. NotXTwinTable.Pairs.SelectMany(pair => pair.Factories)];

        Assert.Contains("StringPredicates.EqualsConfigurable(String)", factories);
        Assert.Contains("DateTimePredicates.InTimeWindow(DateTimeOffset)", factories);
    }

    /// <summary>An optional <c>ignoreCase</c> argument that defaults to <c>true</c> is reported with the rule and the page.</summary>
    [Fact]
    public void Check_IgnoreCaseDefaultingToTrue_NamesThePredicateTheArgumentAndThePage_Test()
    {
        PredicateSchema schema = SchemaWith(
            new PredicateArgumentSchema("ignoreCase", "d", LiteralKind.Boolean, false, LiteralValue.OfBoolean(true))
        );

        IReadOnlyList<string> failures = OptionDefaultsChecker.Check([("StringPredicates.Fixture(String)", schema)]);

        string failure = Assert.Single(failures);
        Assert.Contains("StringPredicates.Fixture(String)", failure);
        Assert.Contains("'ignoreCase' must default to false", failure);
        Assert.Contains("docs/predicate-conventions.md", failure);
    }

    /// <summary>An optional <c>trim</c> argument with no default is reported.</summary>
    [Fact]
    public void Check_TrimWithoutDefault_IsReported_Test()
    {
        PredicateSchema schema = SchemaWith(new PredicateArgumentSchema("trim", "d", LiteralKind.Boolean, false));

        IReadOnlyList<string> failures = OptionDefaultsChecker.Check([("StringPredicates.Fixture(String)", schema)]);

        Assert.Contains("'trim' must default to false", Assert.Single(failures));
    }

    /// <summary>An optional Boolean <c>include*</c> argument with no default is reported.</summary>
    [Fact]
    public void Check_IncludeFlagWithoutDefault_IsReported_Test()
    {
        PredicateSchema schema = SchemaWith(new PredicateArgumentSchema("includeEnd", "d", LiteralKind.Boolean, false));

        IReadOnlyList<string> failures = OptionDefaultsChecker.Check([("DateTimePredicates.Fixture(DateTimeOffset)", schema)]);

        string failure = Assert.Single(failures);
        Assert.Contains("'includeEnd' has no default", failure);
        Assert.Contains("docs/predicate-conventions.md", failure);
    }

    /// <summary>Options that follow the conventions, and required arguments of the same names, produce no failure.</summary>
    [Fact]
    public void Check_ConformingArguments_ReportsNoFailures_Test()
    {
        PredicateSchema schema = SchemaWith(
            new PredicateArgumentSchema("ignoreCase", "d", LiteralKind.Boolean, false, LiteralValue.OfBoolean(false)),
            new PredicateArgumentSchema("trim", "d", LiteralKind.Boolean, false, LiteralValue.OfBoolean(false)),
            new PredicateArgumentSchema("includeStart", "d", LiteralKind.Boolean, false, LiteralValue.OfBoolean(true)),
            new PredicateArgumentSchema("includeEnd", "d", LiteralKind.Boolean)
        );

        IReadOnlyList<string> failures = OptionDefaultsChecker.Check([("StringPredicates.Fixture(String)", schema)]);

        Assert.Empty(failures);
    }

    private static PredicateSchema SchemaWith(params PredicateArgumentSchema[] arguments)
    {
        return new PredicateSchema("fixture", "Fixture", "A fixture schema.", arguments);
    }
}
