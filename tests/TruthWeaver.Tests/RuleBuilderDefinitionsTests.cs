namespace TruthWeaver.Tests;

using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 11-4: the definition API on <see cref="RuleBuilder"/> (ADR-0008 decision 4). Covers that
/// <see cref="RuleBuilder.Use(string, RuleBuilder[])"/> and <see cref="RuleBuilder.WithDefinitions(RuleDefinition[])"/>
/// compile to the same tree the equivalent rule text produces, and that every ADR-0008 guardrail diagnostic
/// (cycle, name collision, wrong argument count, unknown definition) is reported for builder input exactly as it is
/// for rule text and JSON.
/// </summary>
public sealed class RuleBuilderDefinitionsTests
{
    /// <summary>A parameterless use expands to the same compiled tree as the rule-text equivalent.</summary>
    [Fact]
    public void Compile_ParameterlessDefinitionUse_EqualsRuleTextEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleBuilder builder = RuleBuilder
            .And(RuleBuilder.Use("adult"), RuleBuilder.Predicate("b"))
            .WithDefinitions(RuleBuilder.Define("adult", RuleBuilder.Predicate("a")));

        CompiledRule<RuleTestContext> fromBuilder = builder.Compile(compiler).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromText = CompileText(compiler, "let adult = a; adult AND b");

        Assert.Equal(fromText.CanonicalText, fromBuilder.CanonicalText);
        Assert.Equal("a AND b", fromBuilder.CanonicalText);
    }

    /// <summary>A parameterized use substitutes each argument for the matching parameter, equal to the rule-text form.</summary>
    [Fact]
    public void Compile_ParameterizedDefinitionUse_EqualsRuleTextEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinition bothTrue = RuleBuilder.Define(
            "bothTrue",
            ["x", "y"],
            RuleBuilder.And(RuleBuilder.Predicate("x"), RuleBuilder.Predicate("y"))
        );
        RuleBuilder builder = RuleBuilder
            .Use("bothTrue", RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .WithDefinitions(bothTrue);

        CompiledRule<RuleTestContext> fromBuilder = builder.Compile(compiler).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromText = CompileText(compiler, "let bothTrue(x, y) = x AND y; bothTrue(a, b)");

        Assert.Equal(fromText.CanonicalText, fromBuilder.CanonicalText);
    }

    /// <summary>A definition may use another definition declared in the same <see cref="RuleBuilder.WithDefinitions(RuleDefinition[])"/> call; expansion threads through both.</summary>
    [Fact]
    public void Compile_DefinitionUsingAnotherDefinition_EqualsRuleTextEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinition adult = RuleBuilder.Define("adult", RuleBuilder.Predicate("a"));
        RuleDefinition adultAndB = RuleBuilder.Define(
            "adultAndB",
            RuleBuilder.And(RuleBuilder.Use("adult"), RuleBuilder.Predicate("b"))
        );
        RuleBuilder builder = RuleBuilder.Use("adultAndB").WithDefinitions(adult, adultAndB);

        CompiledRule<RuleTestContext> fromBuilder = builder.Compile(compiler).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromText = CompileText(compiler, "let adult = a; let adultAndB = adult AND b; adultAndB");

        Assert.Equal(fromText.CanonicalText, fromBuilder.CanonicalText);
    }

    /// <summary>The <see cref="IEnumerable{T}"/> overloads of <c>Use</c> and <c>WithDefinitions</c> give the same rule as the array overloads.</summary>
    [Fact]
    public void Compile_SequenceOverloads_GiveSameRuleAsArrayOverloads_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        List<string> parameters = ["x", "y"];
        List<RuleBuilder> arguments = [RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b")];
        RuleDefinition bothTrue = RuleBuilder.Define(
            "bothTrue",
            parameters,
            RuleBuilder.And(RuleBuilder.Predicate("x"), RuleBuilder.Predicate("y"))
        );
        List<RuleDefinition> definitions = [bothTrue];

        CompiledRule<RuleTestContext> fromSequences = RuleBuilder
            .Use("bothTrue", arguments)
            .WithDefinitions(definitions)
            .Compile(compiler)
            .GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromArrays = RuleBuilder
            .Use("bothTrue", RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .WithDefinitions(bothTrue)
            .Compile(compiler)
            .GetRuleOrThrow();

        Assert.Equal(fromArrays.CanonicalText, fromSequences.CanonicalText);
    }

    /// <summary>A use of a name no attached definition declares is <c>UnknownDefinition</c>, located by the use's builder path.</summary>
    [Fact]
    public void Compile_UseOfUnknownDefinition_ReportsUnknownDefinition_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleBuilder builder = RuleBuilder.Use("nope");

        CompilationResult<RuleTestContext> result = builder.Compile(compiler);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownDefinition);
        Assert.Equal(SourceSpan.None, diagnostic.Span);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A use that supplies the wrong number of arguments is <c>DefinitionArgumentCountMismatch</c>, naming the counts.</summary>
    [Fact]
    public void Compile_UseWithWrongArgumentCount_ReportsDefinitionArgumentCountMismatch_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinition bothTrue = RuleBuilder.Define(
            "bothTrue",
            ["x", "y"],
            RuleBuilder.And(RuleBuilder.Predicate("x"), RuleBuilder.Predicate("y"))
        );
        RuleBuilder builder = RuleBuilder.Use("bothTrue", RuleBuilder.Predicate("a")).WithDefinitions(bothTrue);

        CompilationResult<RuleTestContext> result = builder.Compile(compiler);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.DefinitionArgumentCountMismatch
        );
        Assert.Equal("2 argument(s)", diagnostic.Expected);
        Assert.Equal("1 argument(s)", diagnostic.Found);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A definition that references itself through a use in its own body is a reported cycle, naming it.</summary>
    [Fact]
    public void Compile_SelfReferencingDefinition_ReportsDefinitionCycleDetected_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinition x = RuleBuilder.Define("x", RuleBuilder.Use("x"));
        RuleBuilder builder = RuleBuilder.Use("x").WithDefinitions(x);

        CompilationResult<RuleTestContext> result = builder.Compile(compiler);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionCycleDetected);
        Assert.Contains("x", diagnostic.Message);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// Two definitions attached with the same name are a reported collision, located at the builder path, rather
    /// than the second one silently overwriting the first in the rendered JSON object (which has one key per name).
    /// </summary>
    [Fact]
    public void Compile_TwoDefinitionsWithSameName_ReportsDefinitionNameCollision_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinition first = RuleBuilder.Define("dup", RuleBuilder.Predicate("a"));
        RuleDefinition second = RuleBuilder.Define("dup", RuleBuilder.Predicate("b"));
        RuleBuilder builder = RuleBuilder.Use("dup").WithDefinitions(first, second);

        CompilationResult<RuleTestContext> result = builder.Compile(compiler);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Equal("$.definitions.dup", diagnostic.Path);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A definition named after a predicate registered with the compiler is a name collision, matching rule text.</summary>
    [Fact]
    public void Compile_DefinitionNamedAfterRegisteredPredicate_ReportsDefinitionNameCollision_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinition collides = RuleBuilder.Define("a", RuleBuilder.Predicate("b"));
        RuleBuilder builder = RuleBuilder.Predicate("b").WithDefinitions(collides);

        CompilationResult<RuleTestContext> result = builder.Compile(compiler);

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
    }

    /// <summary><c>ToJson()</c> on a builder with definitions renders the <c>definitions</c>/<c>use</c> shape ADR-0008 decision 3 gives JSON, so it round-trips through <c>CompileJson(string)</c> unchanged.</summary>
    [Fact]
    public void ToJson_WithDefinitions_RoundTripsThroughCompileJson_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleBuilder builder = RuleBuilder.Use("adult").WithDefinitions(RuleBuilder.Define("adult", RuleBuilder.Predicate("a")));

        string json = builder.ToJson();
        CompiledRule<RuleTestContext> fromRoundTrip = compiler.CompileJson(json).CompiledRule!;
        CompiledRule<RuleTestContext> fromBuilder = builder.Compile(compiler).GetRuleOrThrow();

        Assert.Equal(fromBuilder.CanonicalText, fromRoundTrip.CanonicalText);
        Assert.Contains("\"definitions\"", json);
        Assert.Contains("\"use\"", json);
    }

    private static CompiledRule<RuleTestContext> CompileText(RuleCompiler<RuleTestContext> compiler, string text)
    {
        return compiler.Compile(text).CompiledRule ?? throw new InvalidOperationException($"'{text}' did not compile.");
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build());
    }
}
