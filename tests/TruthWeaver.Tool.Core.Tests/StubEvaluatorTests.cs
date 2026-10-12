namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Tests.TestSupport;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

public sealed class StubEvaluatorTests
{
    /// <summary>Each term takes its stub value, so the rule is evaluated over the stubs with the Strong K3 operators.</summary>
    [Fact]
    public async Task EvaluateAsync_StubValues_GiveEachTermItsValue_Test()
    {
        LoadedRule rule = RuleLoader.LoadFile(Fixtures.Path("access.rule")).Value!;
        StubAssignment stubs = StubAssignment.Load(Fixtures.Path("stubs.json")).Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Stubs = stubs },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.False, result.Value.Decision.Result);
        Assert.Empty(result.Value.Decision.Faults);
    }

    /// <summary>A term with no stub value is Unknown, and it is not a fault.</summary>
    [Fact]
    public async Task EvaluateAsync_NoStubValues_EveryTermIsUnknown_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("isActive OR hasRole(role: \"editor\")").Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.Unknown, result.Value.Decision.Result);
        Assert.Empty(result.Value.Decision.Faults);
    }

    /// <summary>A predicate used with different argument sets and number kinds compiles, and each call finds its own stub.</summary>
    [Fact]
    public async Task EvaluateAsync_OnePredicateWithSeveralArgumentShapes_FindsEachStub_Test()
    {
        LoadedRule rule = RuleLoader
            .LoadString("score(min: 1) AND score(min: 2.5, max: 9) AND tagged(tags: [\"a\", \"b\"])")
            .Value!;
        StubAssignment stubs = StubAssignment
            .Parse(
                """
                'score(min: 1)': T
                'score(max: 9, min: 2.5)': T
                'tagged(tags: ["a", "b"])': T
                """,
                RuleFormat.Yaml
            )
            .Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Stubs = stubs },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.True, result.Value.Decision.Result);
    }

    /// <summary>A term with a data reference takes the stub value of the term with the value that the data file gives.</summary>
    [Fact]
    public async Task EvaluateAsync_DataReference_UsesTheStubOfTheResolvedTerm_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("hasName(name: from(\"user\", \"$.name\"))").Value!;
        StubAssignment stubs = StubAssignment.Parse("'hasName(name: \"Ada\")': True", RuleFormat.Yaml).Value!;
        DataSourceSet data = DataSourceFiles
            .LoadAll([new KeyValuePair<string, string>("user", Fixtures.Path("user.json"))])
            .Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Stubs = stubs, DataSources = data },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.True, result.Value.Decision.Result);
    }

    /// <summary>A data reference to a source that was not given is a fault, and its term is Unknown.</summary>
    [Fact]
    public async Task EvaluateAsync_MissingDataSource_IsAFaultAndUnknown_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("hasName(name: from(\"user\", \"$.name\"))").Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.Unknown, result.Value.Decision.Result);
        Assert.Single(result.Value.Decision.Faults);
    }

    /// <summary>The predicate schema file gives the kind of a data reference argument, so a number from the data file fits.</summary>
    [Fact]
    public async Task EvaluateAsync_SchemaFileKind_AppliesToADataReference_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("olderThan(age: from(\"user\", \"$.age\"))").Value!;
        PredicateSchemaFile schema = PredicateSchemaFile
            .Parse("predicates:\n  olderThan:\n    arguments:\n      age: int64\n")
            .Value!;
        DataSourceSet data = [];
        data.Add("user", DataSourceFiles.Parse("{\"age\": 42}", RuleFormat.Json).Value!);
        StubAssignment stubs = StubAssignment.Parse("'olderThan(age: 42)': True", RuleFormat.Yaml).Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions
            {
                Stubs = stubs,
                DataSources = data,
                PredicateSchema = schema,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.True, result.Value.Decision.Result);
        Assert.Empty(result.Value.Decision.Faults);
    }

    /// <summary>A rule that does not compile gives no evaluation and the compile diagnostics.</summary>
    [Fact]
    public async Task EvaluateAsync_RuleThatDoesNotCompile_ReturnsTheDiagnostics_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("isActive AND").Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>The evaluation options apply: exhaustive mode runs a term that short-circuiting would skip.</summary>
    [Fact]
    public async Task EvaluateAsync_ExhaustiveMode_RunsEveryTerm_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("isActive AND isAdmin").Value!;
        StubAssignment stubs = StubAssignment.Parse("isActive: F", RuleFormat.Yaml).Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Stubs = stubs, Evaluation = new EvaluationOptions(Mode: EvaluationMode.Exhaustive) },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Trace? trace = result.Value.Decision.Trace;
        Assert.NotNull(trace);
        Assert.DoesNotContain(trace.Entries, e => e.NotEvaluated);
    }
}
