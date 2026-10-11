namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="RuleBuilder.FromCompiled{TContext}"/> joins already compiled rules under any builder operator. The joined
/// tree compiles against the destination registry, so every term is validated again.
/// </summary>
public sealed class RuleBuilderFromCompiledTests
{
    /// <summary>A join of two rules compiles to the same rule as the equivalent text.</summary>
    [Fact]
    public void FromCompiled_TwoRulesJoinedWithAnd_CompilesLikeTheEquivalentText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(Registry("a", "b", "c"));
        CompiledRule<RuleTestContext> left = compiler.Compile("a OR b").CompiledRule!;
        CompiledRule<RuleTestContext> right = compiler.Compile("NOT c").CompiledRule!;

        CompilationResult<RuleTestContext> joined = RuleBuilder
            .And(RuleBuilder.FromCompiled(left), RuleBuilder.FromCompiled(right))
            .Compile(compiler);

        Assert.True(joined.Succeeded);
        Assert.Equal(compiler.Compile("(a OR b) AND NOT c").CompiledRule!.CanonicalText, joined.CompiledRule!.CanonicalText);
    }

    /// <summary>A compiled rule works with every builder operator, not only <c>And</c>.</summary>
    [Fact]
    public void FromCompiled_RuleUnderNot_CompilesAndNegatesTheRule_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(Registry("a"));
        CompiledRule<RuleTestContext> rule = compiler.Compile("a").CompiledRule!;

        CompilationResult<RuleTestContext> joined = RuleBuilder.Not(RuleBuilder.FromCompiled(rule)).Compile(compiler);

        Assert.Equal("NOT a", joined.CompiledRule!.CanonicalText);
    }

    /// <summary>A predicate that the destination registry lacks gives the normal unknown-predicate diagnostic.</summary>
    [Fact]
    public void FromCompiled_RuleFromAnotherRegistry_DestinationLacksPredicate_ReportsUnknownPredicate_Test()
    {
        CompiledRule<RuleTestContext> source = new RuleCompiler<RuleTestContext>(Registry("a", "extra"))
            .Compile("a AND extra")
            .CompiledRule!;
        RuleCompiler<RuleTestContext> destination = new(Registry("a"));

        CompilationResult<RuleTestContext> joined = RuleBuilder
            .Or(RuleBuilder.FromCompiled(source), RuleBuilder.Predicate("a"))
            .Compile(destination);

        Assert.False(joined.Succeeded);
        Diagnostic diagnostic = Assert.Single(joined.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, diagnostic.Code);
    }

    /// <summary>A predicate whose schema differs in the destination is checked against the destination schema.</summary>
    [Fact]
    public void FromCompiled_PredicateWithDifferentSchemaInDestination_ReportsArgumentTypeMismatch_Test()
    {
        CompiledRule<RuleTestContext> source = new RuleCompiler<RuleTestContext>(ArgumentRegistry(LiteralKind.Int64))
            .Compile("check(value: 3)")
            .CompiledRule!;
        RuleCompiler<RuleTestContext> destination = new(ArgumentRegistry(LiteralKind.String));

        CompilationResult<RuleTestContext> joined = RuleBuilder.Not(RuleBuilder.FromCompiled(source)).Compile(destination);

        Assert.False(joined.Succeeded);
        Assert.Contains(joined.Diagnostics, d => d.Code == DiagnosticCodes.ArgumentTypeMismatch);
    }

    /// <summary>A data source that the destination does not declare gives <c>TRE0024</c> even though the source rule compiled.</summary>
    [Fact]
    public void FromCompiled_DataSourceUndeclaredInDestination_ReportsUndeclaredDataSource_Test()
    {
        DataSourceDeclarations declarations = ["user"];
        CompiledRule<RuleTestContext> source = new RuleCompiler<RuleTestContext>(
            ArgumentRegistry(LiteralKind.Int64),
            new CompilerOptions(DataSources: declarations)
        )
            .Compile("check(value: from(\"user\", \"$.age\"))")
            .CompiledRule!;
        RuleCompiler<RuleTestContext> destination = new(ArgumentRegistry(LiteralKind.Int64));

        CompilationResult<RuleTestContext> joined = RuleBuilder.Not(RuleBuilder.FromCompiled(source)).Compile(destination);

        Assert.Contains(joined.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
    }

    /// <summary>The destination's depth limit applies to the joined tree.</summary>
    [Fact]
    public void FromCompiled_JoinedTreeAboveDestinationDepthLimit_ReportsMaxDepthExceeded_Test()
    {
        CompiledRule<RuleTestContext> source = new RuleCompiler<RuleTestContext>(Registry("a"))
            .Compile("NOT NOT NOT a")
            .CompiledRule!;
        RuleCompiler<RuleTestContext> destination = new(Registry("a"), new CompilerOptions(MaxDepth: 4));

        CompilationResult<RuleTestContext> joined = RuleBuilder.Not(RuleBuilder.FromCompiled(source)).Compile(destination);

        Assert.False(joined.Succeeded);
        Assert.Contains(joined.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    /// <summary>A term present in both source rules is one term in the joined rule, so its predicate runs once.</summary>
    [Fact]
    public async Task FromCompiled_TermSharedByBothRules_IsEvaluatedOnce_Test()
    {
        int calls = 0;
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("shared", "Shared", "Counts its calls."),
                (_, _, _) =>
                {
                    calls++;
                    return ValueTask.FromResult(TruthValue.True);
                }
            )
            .AddConstant("other", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        CompiledRule<RuleTestContext> left = compiler.Compile("shared AND other").CompiledRule!;
        CompiledRule<RuleTestContext> right = compiler.Compile("shared OR other").CompiledRule!;

        CompiledRule<RuleTestContext> joined = RuleBuilder
            .And(RuleBuilder.FromCompiled(left), RuleBuilder.FromCompiled(right))
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await joined.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(1, calls);
    }

    /// <summary>A null rule is rejected at once.</summary>
    [Fact]
    public void FromCompiled_NullRule_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() => RuleBuilder.FromCompiled<RuleTestContext>(null!));
    }

    private static PredicateRegistry<RuleTestContext> Registry(params string[] names)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string name in names)
        {
            builder.AddConstant(name, true);
        }

        return builder.Build();
    }

    private static PredicateRegistry<RuleTestContext> ArgumentRegistry(LiteralKind kind)
    {
        return PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "check",
                    "Check",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("value", "The value.", kind)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
    }
}
