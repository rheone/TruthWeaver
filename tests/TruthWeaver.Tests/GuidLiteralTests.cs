namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary><see cref="LiteralKind.Guid"/>: a quoted-string-in-the-DSL literal, same story as <see cref="LiteralKind.DateTimeOffset"/>.</summary>
public sealed class GuidLiteralTests
{
    private static readonly Guid MatchId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>A Guid argument matching the schema evaluates to true.</summary>
    [Fact]
    public async Task Evaluate_GuidArgumentMatchingSchema_ReturnsTrue_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddGuidArgPredicate("hasId", "id", MatchId).Build()
        );

        Decision decision = await compiler
            .Compile($"hasId(id: \"{MatchId}\")")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A Guid argument not matching the schema evaluates to false.</summary>
    [Fact]
    public async Task Evaluate_GuidArgumentNotMatchingSchema_ReturnsFalse_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddGuidArgPredicate("hasId", "id", MatchId).Build()
        );

        Decision decision = await compiler
            .Compile("hasId(id: \"22222222-2222-2222-2222-222222222222\")")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>A malformed Guid literal is a compile error, not an exception.</summary>
    [Fact]
    public void Literal_MalformedGuid_IsCompileError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddGuidArgPredicate("hasId", "id", MatchId).Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("hasId(id: \"not-a-guid\")");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.ArgumentTypeMismatch);
    }

    /// <summary>A Guid argument round trips through DSL, JSON and YAML.</summary>
    [Fact]
    public void Literal_GuidArgument_RoundTripsThroughDslJsonAndYaml_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddGuidArgPredicate("hasId", "id", MatchId).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile($"hasId(id: \"{MatchId}\")").CompiledRule!;

        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(original.PrintJson()).CompiledRule!;
        CompiledRule<RuleTestContext> fromYaml = compiler.CompileYaml(original.PrintYaml()).CompiledRule!;

        Assert.Equal(original.CanonicalText, fromJson.CanonicalText);
        Assert.Equal(original.CanonicalText, fromYaml.CanonicalText);
    }

    /// <summary>Guid array arguments with a different element order are distinct terms.</summary>
    [Fact]
    public void Identity_GuidArrayElementOrder_ProducesDistinctTerms_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyId",
                        "hasAnyId",
                        "True iff any of the given ids matches.",
                        [new PredicateArgumentSchema("ids", "The ids to check.", LiteralKind.GuidArray)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetGuidArray("ids").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompiledRule<RuleTestContext> first = compiler
            .Compile("hasAnyId(ids: [\"11111111-1111-1111-1111-111111111111\", \"22222222-2222-2222-2222-222222222222\"])")
            .CompiledRule!;
        CompiledRule<RuleTestContext> second = compiler
            .Compile("hasAnyId(ids: [\"22222222-2222-2222-2222-222222222222\", \"11111111-1111-1111-1111-111111111111\"])")
            .CompiledRule!;

        Assert.NotEqual(first.CanonicalText, second.CanonicalText);
    }
}
