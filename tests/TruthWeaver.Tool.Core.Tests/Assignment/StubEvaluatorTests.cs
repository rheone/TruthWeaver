namespace TruthWeaver.Tool.Core.Tests.Assignment;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
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

    /// <summary>Arguments of every literal kind, and a mix of whole and decimal numbers, compile and find their stubs.</summary>
    [Fact]
    public async Task EvaluateAsync_ArgumentsOfEveryLiteralKind_FindEachStub_Test()
    {
        LoadedRule rule = RuleLoader
            .LoadString(
                "flag(on: true) AND ids(ids: [1, 2]) AND prices(prices: [1.5]) AND oks(oks: [true]) "
                    + "AND num(n: 1) AND num(n: 2.5) AND nums(n: [1]) AND nums(n: [2.5]) AND empty(items: [])"
            )
            .Value!;
        StubAssignment stubs = StubAssignment
            .Parse(
                """
                'flag(on: true)': T
                'ids(ids: [1, 2])': T
                'prices(prices: [1.5])': T
                'oks(oks: [true])': T
                'num(n: 1)': T
                'num(n: 2.5)': T
                'nums(n: [1])': T
                'nums(n: [2.5])': T
                'empty(items: [])': T
                """,
                RuleFormat.Yaml
            )
            .Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Stubs = stubs },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(TruthValue.True, result.Value.Decision.Result);
    }

    /// <summary>An argument given by a literal in one call and by a data reference in another keeps the literal kind.</summary>
    [Fact]
    public async Task EvaluateAsync_ArgumentAsLiteralAndReference_EvaluatesBoth_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("older(age: 1) AND older(age: from(\"user\", \"$.age\"))").Value!;
        DataSourceSet data = [];
        data.Add("user", DataSourceFiles.Parse("{\"age\": 42}", RuleFormat.Json).Value!);
        StubAssignment stubs = StubAssignment.Parse("'older(age: 1)': T\n'older(age: 42)': T\n", RuleFormat.Yaml).Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Stubs = stubs, DataSources = data },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(TruthValue.True, result.Value.Decision.Result);
        Assert.Empty(result.Value.Decision.Faults);
    }

    /// <summary>The schema file named in the settings gives the kind of an argument when no schema was passed in the options.</summary>
    [Fact]
    public async Task EvaluateAsync_SchemaFileInSettings_IsLoadedAndApplied_Test()
    {
        using TemporaryDirectory directory = new();
        string schemaPath = directory.File("predicates.yaml");
        await File.WriteAllTextAsync(
            schemaPath,
            "predicates:\n  olderThan:\n    arguments:\n      age: int64\n",
            TestContext.Current.CancellationToken
        );
        LoadedRule rule = RuleLoader.LoadString("olderThan(age: from(\"user\", \"$.age\"))").Value!;
        DataSourceSet data = [];
        data.Add("user", DataSourceFiles.Parse("{\"age\": 42}", RuleFormat.Json).Value!);
        StubAssignment stubs = StubAssignment.Parse("'olderThan(age: 42)': True", RuleFormat.Yaml).Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions
            {
                Stubs = stubs,
                DataSources = data,
                Settings = ToolSettings.Default with { SchemaFile = schemaPath },
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(TruthValue.True, result.Value.Decision.Result);
        Assert.Empty(result.Value.Decision.Faults);
    }

    /// <summary>A schema file in the settings that cannot be read gives no evaluation and its diagnostics.</summary>
    [Fact]
    public async Task EvaluateAsync_SchemaFileInSettingsMissing_ReturnsTheDiagnostics_Test()
    {
        using TemporaryDirectory directory = new();
        LoadedRule rule = RuleLoader.LoadString("isActive").Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { Settings = ToolSettings.Default with { SchemaFile = directory.File("missing.yaml") } },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>A schema file kind that the literal in the rule does not fit fails with the error of the stand-in compile.</summary>
    [Fact]
    public async Task EvaluateAsync_SchemaKindDiffersFromLiteral_ReturnsTheDiagnostics_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("olderThan(age: 42)").Value!;
        PredicateSchemaFile schema = PredicateSchemaFile
            .Parse("predicates:\n  olderThan:\n    arguments:\n      age: string\n")
            .Value!;

        ToolResult<StubEvaluation> result = await StubEvaluator.EvaluateAsync(
            rule,
            new StubEvaluationOptions { PredicateSchema = schema },
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
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
            new StubEvaluationOptions
            {
                Stubs = stubs,
                Evaluation = new EvaluationOptions(Mode: EvaluationMode.Exhaustive, IncludeTrace: true),
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Trace? trace = result.Value.Decision.Trace;
        Assert.NotNull(trace);
        Assert.DoesNotContain(trace.Entries, e => e.NotEvaluated);
    }
}
