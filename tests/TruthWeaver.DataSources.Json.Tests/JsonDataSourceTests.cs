namespace TruthWeaver.DataSources.Json.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// <see cref="JsonDataSource"/> (ADR-0006 decisions 5, 8 and 14, data-sources ticket 04): JSONPath queries over a JSON
/// document, node-to-literal conversion, and scoping to a subtree.
/// </summary>
public sealed class JsonDataSourceTests
{
    private const string OrdersJson = """
        {
          "limits": { "age": 18, "ratio": 1.5, "whole": 18.0, "name": "adult", "on": true, "off": false, "nothing": null },
          "roles": ["admin", "auditor"],
          "orders": [
            { "id": "A7", "total": 120, "lines": [{ "sku": "x" }, { "sku": "y" }] },
            { "id": "B2", "total": 80, "lines": [{ "sku": "z" }] }
          ]
        }
        """;

    /// <summary>A filter pins one repeated element and returns its member.</summary>
    [Fact]
    public async Task QueryAsync_FilterSelectingOneOrder_ReturnsThatOrdersTotal_Test()
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        DataQueryResult result = await source.QueryAsync("$.orders[?@.id=='A7'].total", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(LiteralValue.OfInt64(120), Assert.Single(result.Matches));
    }

    /// <summary>A query matching several nodes returns every match in document order, leaving ambiguity to the engine.</summary>
    [Fact]
    public async Task QueryAsync_WildcardOverTheOrders_ReturnsEveryTotalInDocumentOrder_Test()
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        DataQueryResult result = await source.QueryAsync("$.orders[*].total", TestContext.Current.CancellationToken);

        Assert.Equal([LiteralValue.OfInt64(120), LiteralValue.OfInt64(80)], result.Matches);
    }

    /// <summary>A query that matches nothing is a successful, empty result, including a path to a missing property.</summary>
    [Theory]
    [InlineData("$.orders[?@.id=='nope'].total")]
    [InlineData("$.missing")]
    [InlineData("$.missing[*]")]
    public async Task QueryAsync_NothingMatches_IsASuccessfulEmptyResult_Test(string query)
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        DataQueryResult result = await source.QueryAsync(query, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Matches);
    }

    /// <summary>Each JSON scalar converts to the literal of its natural kind; the engine does any further conversion.</summary>
    [Theory]
    [InlineData("$.limits.age", LiteralKind.Int64, "18")]
    [InlineData("$.limits.ratio", LiteralKind.Decimal, "1.5")]
    [InlineData("$.limits.whole", LiteralKind.Decimal, "18.0")]
    [InlineData("$.limits.name", LiteralKind.String, "\"adult\"")]
    [InlineData("$.limits.on", LiteralKind.Boolean, "true")]
    [InlineData("$.limits.off", LiteralKind.Boolean, "false")]
    public async Task QueryAsync_ScalarNode_ConvertsToItsNaturalLiteralKind_Test(string query, LiteralKind kind, string text)
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        DataQueryResult result = await source.QueryAsync(query, TestContext.Current.CancellationToken);

        LiteralValue match = Assert.Single(result.Matches);
        Assert.Equal(kind, match.Kind);
        Assert.Equal(text, match.ToString());
    }

    /// <summary>A matched null, object or array has no literal equivalent and is reported as an unsupported type.</summary>
    [Theory]
    [InlineData("$.limits.nothing")]
    [InlineData("$.limits")]
    [InlineData("$.roles")]
    [InlineData("$.orders[*]")]
    public async Task QueryAsync_NodeWithoutALiteralEquivalent_ReportsUnsupportedType_Test(string query)
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        DataQueryResult result = await source.QueryAsync(query, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(DataQueryErrorKind.UnsupportedType, result.ErrorKind);
        Assert.DoesNotContain("adult", result.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>A query that is not valid JSONPath is reported as a malformed query, as data.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("orders")]
    [InlineData("$.orders[")]
    [InlineData("$..[?@.id==]")]
    public async Task QueryAsync_MalformedJsonPath_ReportsMalformedQuery_Test(string query)
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        DataQueryResult result = await source.QueryAsync(query, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(DataQueryErrorKind.MalformedQuery, result.ErrorKind);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    /// <summary>A cancelled token stops the query instead of answering it.</summary>
    [Fact]
    public async Task QueryAsync_CancelledToken_ThrowsOperationCanceled_Test()
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await source.QueryAsync("$.limits.age", cancelled.Token)
        );
    }

    /// <summary>A source scoped to one order answers absolute queries within that order.</summary>
    [Fact]
    public async Task ScopeAsync_OneOrder_RootsQueriesAtThatOrder_Test()
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        IDataSource order = await source.ScopeAsync("$.orders[?@.id=='B2']", TestContext.Current.CancellationToken);
        DataQueryResult total = await order.QueryAsync("$.total", TestContext.Current.CancellationToken);
        DataQueryResult sku = await order.QueryAsync("$.lines[*].sku", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfInt64(80), Assert.Single(total.Matches));
        Assert.Equal(LiteralValue.OfString("z"), Assert.Single(sku.Matches));
    }

    /// <summary>A scoped source cannot see the rest of the document, so <c>$.orders</c> inside it matches nothing.</summary>
    [Fact]
    public async Task ScopeAsync_ScopedSource_DoesNotSeeTheEnclosingDocument_Test()
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        IDataSource order = await source.ScopeAsync("$.orders[0]", TestContext.Current.CancellationToken);
        DataQueryResult outside = await order.QueryAsync("$.limits.age", TestContext.Current.CancellationToken);

        Assert.True(outside.Succeeded);
        Assert.Empty(outside.Matches);
    }

    /// <summary>Scoping can be repeated, narrowing again within the scoped source.</summary>
    [Fact]
    public async Task ScopeAsync_ScopedTwice_NarrowsFurther_Test()
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        IDataSource order = await source.ScopeAsync("$.orders[?@.id=='A7']", TestContext.Current.CancellationToken);
        IDataSource line = await order.ScopeAsync("$.lines[1]", TestContext.Current.CancellationToken);
        DataQueryResult sku = await line.QueryAsync("$.sku", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfString("y"), Assert.Single(sku.Matches));
    }

    /// <summary>A scope query must match exactly one node: none, several or a malformed query is a caller error.</summary>
    [Theory]
    [InlineData("$.orders[?@.id=='nope']", typeof(InvalidOperationException))]
    [InlineData("$.orders[*]", typeof(InvalidOperationException))]
    [InlineData("$.orders[", typeof(ArgumentException))]
    public Task ScopeAsync_QueryNotMatchingExactlyOneNode_Throws_Test(string query, Type expected)
    {
        JsonDataSource source = JsonDataSource.Parse(OrdersJson);

        return Assert.ThrowsAsync(expected, () => source.ScopeAsync(query, TestContext.Current.CancellationToken).AsTask());
    }

    /// <summary>Text that is not JSON is rejected when the source is created.</summary>
    [Fact]
    public void Parse_MalformedJson_ThrowsJsonException_Test()
    {
        Assert.ThrowsAny<JsonException>(() => JsonDataSource.Parse("{ not json"));
    }

    /// <summary>A source can be created over an already-parsed document node.</summary>
    [Fact]
    public async Task Create_ParsedNode_AnswersQueries_Test()
    {
        JsonDataSource source = JsonDataSource.Create(System.Text.Json.Nodes.JsonNode.Parse("""{ "a": 1 }"""));

        DataQueryResult result = await source.QueryAsync("$.a", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfInt64(1), Assert.Single(result.Matches));
    }

    /// <summary>End to end: a scalar variable over a filter that matches several orders is an ambiguous fault, so the term is Unknown.</summary>
    [Fact]
    public async Task EvaluateAsync_ScalarVariableMatchingSeveralNodes_IsUnknownWithAnAmbiguousFault_Test()
    {
        CompiledRule<object?> rule = Compile("takesInt64(v: from(\"doc\", \"$.orders[*].total\"))");
        DataSources sources = new() { ["doc"] = JsonDataSource.Parse(OrdersJson) };

        Decision decision = await rule.EvaluateAsync(
            null,
            new NoServices(),
            sources,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        VariableResolutionException fault = Assert.IsType<VariableResolutionException>(
            Assert.Single(decision.Faults).Exception
        );
        Assert.Equal(VariableFailureKind.Ambiguous, fault.Kind);
    }

    /// <summary>End to end: a filter naming one order resolves and the predicate sees the order's total.</summary>
    [Fact]
    public async Task EvaluateAsync_ScalarVariableMatchingOneNode_PassesTheValueToThePredicate_Test()
    {
        CompiledRule<object?> rule = Compile("takesInt64(v: from(\"doc\", \"$.orders[?@.id=='A7'].total\"))");
        DataSources sources = new() { ["doc"] = JsonDataSource.Parse(OrdersJson) };

        Decision decision = await rule.EvaluateAsync(
            null,
            new NoServices(),
            sources,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>End to end: an array argument collects every match from a wildcard query.</summary>
    [Fact]
    public async Task EvaluateAsync_ArrayVariable_CollectsEveryMatch_Test()
    {
        CompiledRule<object?> rule = Compile("takesInt64s(v: from(\"doc\", \"$.orders[*].total\"))");
        DataSources sources = new() { ["doc"] = JsonDataSource.Parse(OrdersJson) };

        Decision decision = await rule.EvaluateAsync(
            null,
            new NoServices(),
            sources,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    private static CompiledRule<object?> Compile(string ruleText)
    {
        PredicateRegistryBuilder<object?> builder = PredicateRegistry<object?>.CreateBuilder();
        foreach (
            (string name, LiteralKind kind) in new[]
            {
                ("takesInt64", LiteralKind.Int64),
                ("takesInt64s", LiteralKind.Int64Array),
            }
        )
        {
            builder.Add(
                new PredicateSchema(name, name, "Always true.", [new PredicateArgumentSchema("v", "The value.", kind)]),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            );
        }

        RuleCompiler<object?> compiler = new(
            builder.Build(),
            new CompilerOptions(DataSources: new DataSourceDeclarations { "doc" })
        );
        return compiler.Compile(ruleText).CompiledRule ?? throw new InvalidOperationException("Test rule did not compile.");
    }

    private sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
