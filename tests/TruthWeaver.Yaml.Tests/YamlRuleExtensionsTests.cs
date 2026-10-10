namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;

public sealed class YamlRuleExtensionsTests
{
    /// <summary>CompileYaml compiles a single term.</summary>
    [Fact]
    public void CompileYaml_SingleTerm_CompilesRule_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: isManager");

        Assert.True(result.Succeeded);
        Assert.Equal("isManager", result.CompiledRule!.CanonicalText);
    }

    /// <summary>PrintYaml and CompileYaml round trip a compiled rule.</summary>
    [Fact]
    public void PrintYaml_CompiledRule_RoundTripsThroughCompileYaml_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddStringArgPredicate("hasRole", "role", "Y")
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>CompileYaml reports a diagnostic rather than throwing for an unknown predicate.</summary>
    [Fact]
    public void CompileYaml_UnknownPredicate_ReportsDiagnostic_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: neverRegistered");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    /// <summary>
    /// An unquoted <c>null</c>, <c>~</c> or empty value as a predicate argument is rejected with the diagnostic JSON gives
    /// for <c>null</c>: the same code, expectation and path. It does not become the string "null".
    /// </summary>
    [Theory]
    [InlineData("null")]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("~")]
    [InlineData("")]
    public void CompileYaml_UnquotedNullArgument_IsRejectedLikeJsonNull_Test(string value)
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<YamlTestContext> yaml = compiler.CompileYaml($"predicate: hasRole\nargs:\n  role: {value}\n");
        CompilationResult<YamlTestContext> json = compiler.CompileJson("""{"predicate":"hasRole","args":{"role":null}}""");

        Assert.False(yaml.Succeeded);
        Diagnostic expected = Assert.Single(json.Diagnostics);
        Diagnostic actual = Assert.Single(yaml.Diagnostics);
        Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.Expected, actual.Expected);
        Assert.Equal(expected.Found, actual.Found);
        Assert.Equal(expected.Path, actual.Path);
    }

    /// <summary>A quoted <c>"null"</c> is the author's explicit string, so it still compiles as the text.</summary>
    [Fact]
    public void CompileYaml_QuotedNullArgument_IsTheString_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "null").Build()
        );

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: hasRole\nargs:\n  role: \"null\"\n");

        Assert.True(result.Succeeded);
        Assert.Equal("hasRole(role: \"null\")", result.CompiledRule!.CanonicalText);
    }
}
