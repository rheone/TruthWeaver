namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// A predicate argument name matches its schema ignoring case on every surface, as predicate and operator names already
/// do, and the canonical text keeps the spelling that the schema declares.
/// </summary>
public sealed class ArgumentNameCaseTests
{
    private const string Canonical = "hasRole(role: \"Y\")";

    /// <summary>An argument name written in another case in rule text matches the schema and prints in the schema's spelling.</summary>
    [Theory]
    [InlineData("hasRole(Role: \"Y\")")]
    [InlineData("hasRole(ROLE: \"Y\")")]
    [InlineData("hasRole(role: \"Y\")")]
    public void Compile_ArgumentNameInAnotherCase_MatchesTheSchemaArgument_Test(string text)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(Canonical, result.CompiledRule!.CanonicalText);
    }

    /// <summary>An argument name written in another case in a JSON rule matches the schema argument.</summary>
    [Fact]
    public void CompileJson_ArgumentNameInAnotherCase_MatchesTheSchemaArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson("""{"predicate":"hasRole","args":{"ROLE":"Y"}}""");

        Assert.True(result.Succeeded);
        Assert.Equal(Canonical, result.CompiledRule!.CanonicalText);
    }

    /// <summary>An argument name written in another case in a YAML rule matches the schema argument.</summary>
    [Fact]
    public void CompileYaml_ArgumentNameInAnotherCase_MatchesTheSchemaArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileYaml("predicate: hasRole\nargs:\n  Role: Y\n");

        Assert.True(result.Succeeded);
        Assert.Equal(Canonical, result.CompiledRule!.CanonicalText);
    }

    /// <summary>An argument name given to <see cref="RuleBuilder"/> in another case matches the schema argument.</summary>
    [Fact]
    public void Compile_BuilderArgumentNameInAnotherCase_MatchesTheSchemaArgument_Test()
    {
        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasRole", ("Role", "Y")).Compile(CreateCompiler());

        Assert.True(result.Succeeded);
        Assert.Equal(Canonical, result.CompiledRule!.CanonicalText);
    }

    /// <summary>The predicate reads the value through the schema's own spelling, whatever case the rule used.</summary>
    [Fact]
    public async Task EvaluateAsync_ArgumentNameInAnotherCase_ReachesThePredicateUnderTheSchemaName_Test()
    {
        CompiledRule<RuleTestContext> rule = CreateCompiler().Compile("hasRole(ROLE: \"Y\")").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>Two spellings of one argument make the same term, so the compiled rules are equal.</summary>
    [Fact]
    public void Compile_TwoSpellingsOfOneArgument_GiveTheSameTerm_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        string spelledUpper = compiler.Compile("hasRole(ROLE: \"Y\") AND hasRole(role: \"Y\")").CompiledRule!.CanonicalText;

        // Term identity keys on the schema's spelling, so memoisation sees one variable (the canonical text keeps both calls).
        Assert.Equal("hasRole(role: \"Y\") AND hasRole(role: \"Y\")", spelledUpper);
    }

    /// <summary>A required argument supplied in another case is not reported missing.</summary>
    [Fact]
    public void Compile_RequiredArgumentInAnotherCase_IsNotMissing_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("hasRole(rOlE: \"Y\")");

        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.MissingArgument);
    }

    /// <summary>A name that is no schema argument in any case is still reported unknown.</summary>
    [Fact]
    public void Compile_ArgumentNameNoCaseMatches_ReportsUnknownArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("hasRole(Rolee: \"Y\")");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownArgument);
    }

    /// <summary>The same argument written twice in different cases is <c>TRE0032</c> in rule text.</summary>
    [Fact]
    public void Compile_SameArgumentInTwoCases_ReportsDuplicateArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("hasRole(role: \"Y\", Role: \"Z\")");

        Assert.Equal(DiagnosticCodes.DuplicateArgument, Assert.Single(Errors(result)).Code);
    }

    /// <summary>The same argument written twice in different cases is <c>TRE0032</c> in JSON.</summary>
    [Fact]
    public void CompileJson_SameArgumentInTwoCases_ReportsDuplicateArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson("""{"predicate":"hasRole","args":{"role":"Y","ROLE":"Z"}}""");

        Assert.Equal(DiagnosticCodes.DuplicateArgument, Assert.Single(Errors(result)).Code);
    }

    /// <summary>The same argument written twice in different cases is <c>TRE0032</c> in YAML.</summary>
    [Fact]
    public void CompileYaml_SameArgumentInTwoCases_ReportsDuplicateArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileYaml("predicate: hasRole\nargs:\n  role: Y\n  Role: Z\n");

        Assert.Equal(DiagnosticCodes.DuplicateArgument, Assert.Single(Errors(result)).Code);
    }

    /// <summary>The same argument given to <see cref="RuleBuilder"/> in two cases is <c>TRE0032</c> and <c>ToJson</c> throws.</summary>
    [Fact]
    public void Compile_BuilderSameArgumentInTwoCases_ReportsDuplicateArgument_Test()
    {
        RuleBuilder builder = RuleBuilder.Predicate("hasRole", ("role", "Y"), ("ROLE", "Z"));

        CompilationResult<RuleTestContext> result = builder.Compile(CreateCompiler());

        Assert.Equal(DiagnosticCodes.DuplicateArgument, Assert.Single(Errors(result)).Code);
        Assert.Throws<InvalidOperationException>(() => builder.ToJson());
    }

    /// <summary>A schema with two argument names that differ only in case is rejected when the predicate is registered.</summary>
    [Fact]
    public void Add_SchemaWithCaseVariantArgumentNames_ThrowsNamingBothArguments_Test()
    {
        PredicateSchema schema = new(
            "hasCrust",
            "Has Crust",
            "Test predicate with two arguments that differ only in case.",
            [
                new PredicateArgumentSchema("crust", "The crust.", LiteralKind.String),
                new PredicateArgumentSchema("Crust", "The crust again.", LiteralKind.String),
            ]
        );
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            builder.Add(schema, (_, _, _) => ValueTask.FromResult(TruthValue.True))
        );

        Assert.Contains("hasCrust", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'crust'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'Crust'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A schema whose argument names differ by more than case registers.</summary>
    [Fact]
    public void Add_SchemaWithDistinctArgumentNames_Registers_Test()
    {
        PredicateSchema schema = new(
            "hasCrust",
            "Has Crust",
            "Test predicate with two distinct arguments.",
            [
                new PredicateArgumentSchema("crust", "The crust.", LiteralKind.String),
                new PredicateArgumentSchema("sauce", "The sauce.", LiteralKind.String),
            ]
        );

        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(schema, (_, _, _) => ValueTask.FromResult(TruthValue.True))
            .Build();

        Assert.True(registry.TryGetSchema("HASCRUST", out _));
    }

    private static IEnumerable<Diagnostic> Errors(CompilationResult<RuleTestContext> result)
    {
        return result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build());
    }
}
