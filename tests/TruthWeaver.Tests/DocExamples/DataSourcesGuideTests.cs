namespace TruthWeaver.Tests.DocExamples;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Runs the C# snippets of <c>docs/data-sources.md</c> (and the README section) against the real library, because the
/// documentation checker only covers the text, JSON and YAML blocks. Each test follows one snippet of the guide.
/// </summary>
public sealed class DataSourcesGuideTests
{
    private const string AgeRule = "ageAtLeast(min: from(\"user\", \"$.minAge\"))";

    /// <summary>"Supplying data sources": declare names, compile, supply sources, evaluate.</summary>
    [Fact]
    public async Task Guide_DeclareCompileAndEvaluate_GivesTheResolvedAnswer_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            Registry(),
            new CompilerOptions(DataSources: new DataSourceDeclarations { "user", "request" })
        );
        CompilationResult<RuleTestContext> result = compiler.Compile(
            "ageAtLeast(min: from(\"user\", \"$.minAge\")) AND hasRole(role: from(\"request\", \"$.requiredRole\"))"
        );
        DataSources sources = new()
        {
            ["user"] = new FakeDataSource().With("$.minAge", 18L),
            ["request"] = new FakeDataSource().With("$.requiredRole", "admin"),
        };

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(decision.IsSatisfied);
    }

    /// <summary>"Supplying data sources": a name declared with the JSONPath validator turns a malformed query into TRE0025.</summary>
    [Fact]
    public void Guide_DeclaredWithValidator_MalformedQueryIsTre0025_Test()
    {
        DataSourceDeclarations declarations = new() { { "user", JsonQueryValidator.Instance }, "request" };
        RuleCompiler<RuleTestContext> compiler = new(Registry(), new CompilerOptions(DataSources: declarations));

        CompilationResult<RuleTestContext> checkedSource = compiler.Compile("ageAtLeast(min: from(\"user\", \"$.orders[\"))");
        CompilationResult<RuleTestContext> uncheckedSource = compiler.Compile(
            "hasRole(role: from(\"request\", \"$.orders[\"))"
        );

        Assert.Contains(checkedSource.Diagnostics, d => d.Code == DiagnosticCodes.MalformedDataQuery);
        Assert.True(uncheckedSource.Succeeded);
    }

    /// <summary>"Queries": the JSON and YAML snippets answer the same query alike, and a scope narrows to one repeated subtree.</summary>
    [Fact]
    public async Task Guide_JsonYamlAndScope_AnswerTheDocumentedQueries_Test()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        JsonDataSource json = JsonDataSource.Parse("""{ "minAge": 18, "roles": ["admin", "auditor"] }""");
        YamlDataSource yaml = YamlDataSource.Parse("minAge: 18\nroles: [admin, auditor]\n");
        JsonDataSource orders = JsonDataSource.Parse(
            """{ "orders": [ { "id": "A7", "total": 40 }, { "id": "B2", "total": 9 } ] }"""
        );

        IDataSource order = await orders.ScopeAsync("$.orders[?@.id=='A7']", token);

        Assert.Equal(
            (await json.QueryAsync("$.roles[*]", token)).Matches,
            (await yaml.QueryAsync("$.roles[*]", token)).Matches
        );
        Assert.Equal(LiteralValue.OfInt64(18), Assert.Single((await yaml.QueryAsync("$.minAge", token)).Matches));
        Assert.Equal(LiteralValue.OfInt64(40), Assert.Single((await order.QueryAsync("$.total", token)).Matches));
    }

    /// <summary>"Using variables in RuleBuilder": the deferred and eager forms.</summary>
    [Fact]
    public async Task Guide_RuleBuilder_DeferredAndEagerForms_CompileAsDocumented_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            Registry(),
            new CompilerOptions(DataSources: new DataSourceDeclarations { "user" })
        );
        IDataSource configSource = JsonDataSource.Parse("""{ "limits": { "age": 21 } }""");

        RuleBuilder deferred = RuleBuilder.Predicate("ageAtLeast", ("min", Arg.From("user", "$.minAge")));
        long limit = await configSource.GetAsync<long>("$.limits.age", TestContext.Current.CancellationToken);
        RuleBuilder eager = RuleBuilder.Predicate("ageAtLeast", ("min", limit));

        Assert.Equal(AgeRule, deferred.Compile(compiler).CompiledRule!.CanonicalText);
        Assert.Equal("ageAtLeast(min: 21)", eager.Compile(compiler).CompiledRule!.CanonicalText);
    }

    /// <summary>"Failures": the opt-in puts the value in the trace, and the fault never carries it.</summary>
    [Fact]
    public async Task Guide_IncludeResolvedValues_TraceShowsTheValueAndFaultsDoNot_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            Registry(),
            new CompilerOptions(DataSources: new DataSourceDeclarations { "user", "request" })
        );
        CompiledRule<RuleTestContext> rule = compiler
            .Compile("ageAtLeast(min: from(\"user\", \"$.minAge\")) OR hasRole(role: from(\"request\", \"$.missing\"))")
            .CompiledRule!;
        DataSources sources = new()
        {
            ["user"] = new FakeDataSource().With("$.minAge", 18L),
            ["request"] = new FakeDataSource(),
        };

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            sources,
            new EvaluationOptions(Mode: EvaluationMode.Exhaustive, IncludeResolvedValues: true),
            TestContext.Current.CancellationToken
        );

        Assert.Contains(
            "min: from(\"user\", \"$.minAge\") = 18",
            decision.TraceTree!.Children[0].Text,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("18", Assert.Single(decision.Faults).Exception.Message, StringComparison.Ordinal);
    }

    /// <summary>"Testing with FakeDataSource": the chained setup, including an array and a scripted failure.</summary>
    [Fact]
    public async Task Guide_FakeDataSource_ChainedSetupAnswersAsScripted_Test()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        FakeDataSource user = new FakeDataSource()
            .With("$.minAge", 18L)
            .With("$.roles[*]", ["admin", "auditor"])
            .Failing("$.salary", "source offline");

        Assert.Equal(2, (await user.QueryAsync("$.roles[*]", token)).Matches.Count);
        Assert.False((await user.QueryAsync("$.salary", token)).Succeeded);
    }

    private static PredicateRegistry<RuleTestContext> Registry()
    {
        return PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "ageAtLeast",
                    "Age At Least",
                    "Is the user at least the given age?",
                    [new PredicateArgumentSchema("min", "The minimum age.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Add(
                new PredicateSchema(
                    "hasRole",
                    "Has Role",
                    "Does the user have the given role?",
                    [new PredicateArgumentSchema("role", "The role to check for.", LiteralKind.String)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
    }
}
