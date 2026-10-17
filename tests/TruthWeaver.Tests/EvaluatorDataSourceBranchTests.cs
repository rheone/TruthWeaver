namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;

/// <summary>
/// How the evaluator resolves a variable reference when the data source answers in an unusual way: an empty array
/// match, a reference two terms share, a source that returns nothing, and a source that reports an error as data.
/// </summary>
public sealed class EvaluatorDataSourceBranchTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>An array argument whose query matches nothing is an empty array, and the trace text of the term says that nothing matched.</summary>
    [Fact]
    public async Task EvaluateAsync_ArrayReferenceMatchingNothing_NotesTheEmptyMatchInTheTrace_Test()
    {
        CompiledRule<object?> rule = Compile("hasAny(v: from(\"user\", \"$.roles\"))");
        DataSources sources = new() { ["user"] = new FakeDataSource() };

        Decision decision = await rule.EvaluateAsync(null, dataSources: sources, cancellationToken: Token);

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
        Assert.EndsWith(" [no match for from(\"user\", \"$.roles\")]", decision.TraceTree!.Text, StringComparison.Ordinal);
    }

    /// <summary>Two different terms that use the same reference share one answer, so the source is asked once.</summary>
    [Fact]
    public async Task EvaluateAsync_TwoTermsSharingAReference_QueryTheSourceOnce_Test()
    {
        CompiledRule<object?> rule = Compile(
            "hasAny(v: from(\"user\", \"$.roles\")) AND hasAll(v: from(\"user\", \"$.roles\"))"
        );
        FakeDataSource source = new FakeDataSource().With("$.roles", ["admin", "auditor"]);
        DataSources sources = new() { ["user"] = source };

        Decision decision = await rule.EvaluateAsync(null, dataSources: sources, cancellationToken: Token);

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(["$.roles"], source.Queries);
    }

    /// <summary>A source that returns no result object at all is a source-error fault, not a crash.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceReturnsNull_IsUnknownWithASourceErrorFault_Test()
    {
        CompiledRule<object?> rule = Compile("hasAny(v: from(\"user\", \"$.roles\"))");
        DataSources sources = new() { ["user"] = new NullResultSource() };

        Decision decision = await rule.EvaluateAsync(null, dataSources: sources, cancellationToken: Token);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        VariableResolutionException failure = Assert.IsType<VariableResolutionException>(
            Assert.Single(decision.Faults).Exception
        );
        Assert.Equal(VariableFailureKind.SourceError, failure.Kind);
        Assert.Contains("returned no result", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A source that reports a node with no literal equivalent is a type-mismatch fault.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceReportsUnsupportedType_IsATypeMismatchFault_Test()
    {
        CompiledRule<object?> rule = Compile("hasAny(v: from(\"user\", \"$.roles\"))");
        FakeDataSource source = new FakeDataSource().Failing("$.roles", "an object", DataQueryErrorKind.UnsupportedType);
        DataSources sources = new() { ["user"] = source };

        Decision decision = await rule.EvaluateAsync(null, dataSources: sources, cancellationToken: Token);

        VariableResolutionException failure = Assert.IsType<VariableResolutionException>(
            Assert.Single(decision.Faults).Exception
        );
        Assert.Equal(VariableFailureKind.TypeMismatch, failure.Kind);
    }

    /// <summary>A source that reports a failing backend as data is a source-error fault that carries the source's message.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceReportsBackendFailure_IsASourceErrorFault_Test()
    {
        CompiledRule<object?> rule = Compile("hasAny(v: from(\"user\", \"$.roles\"))");
        FakeDataSource source = new FakeDataSource().Failing("$.roles", "backend offline");
        DataSources sources = new() { ["user"] = source };

        Decision decision = await rule.EvaluateAsync(null, dataSources: sources, cancellationToken: Token);

        VariableResolutionException failure = Assert.IsType<VariableResolutionException>(
            Assert.Single(decision.Faults).Exception
        );
        Assert.Equal(VariableFailureKind.SourceError, failure.Kind);
        Assert.Contains("backend offline", failure.Message, StringComparison.Ordinal);
    }

    private static CompiledRule<object?> Compile(string ruleText)
    {
        PredicateRegistryBuilder<object?> builder = PredicateRegistry<object?>.CreateBuilder();
        foreach (string name in new[] { "hasAny", "hasAll" })
        {
            builder.Add(
                new PredicateSchema(
                    name,
                    name,
                    "Returns True.",
                    [new PredicateArgumentSchema("v", "The values.", LiteralKind.StringArray)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            );
        }

        DataSourceDeclarations declarations = [];
        declarations.Add("user");
        RuleCompiler<object?> compiler = new(builder.Build(), new CompilerOptions(DataSources: declarations));
        CompilationResult<object?> result = compiler.Compile(ruleText);
        return result.CompiledRule
            ?? throw new InvalidOperationException($"Test rule did not compile: {result.FormatDiagnostics(ruleText)}");
    }

    /// <summary>A badly behaved source that answers every query with <see langword="null"/>.</summary>
    private sealed class NullResultSource : IDataSource
    {
        public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<DataQueryResult>(null!);
        }

        public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(DataScopeResult.Success(this));
        }
    }
}
