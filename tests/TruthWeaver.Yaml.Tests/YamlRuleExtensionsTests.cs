namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;
using YamlDotNet.RepresentationModel;

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

    /// <summary>A YAML <c>const</c> written as the letter T, F or U (either case) compiles to the same tree as the DSL text.</summary>
    [Theory]
    [InlineData("T", "T")]
    [InlineData("f", "F")]
    [InlineData("U", "U")]
    [InlineData("\"u\"", "U")]
    public void CompileYaml_ConstantLetter_EqualsDslConstant_Test(string letter, string dslText)
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());

        CompilationResult<YamlTestContext> fromYaml = compiler.CompileYaml($"const: {letter}");
        CompilationResult<YamlTestContext> fromDsl = compiler.Compile(dslText);

        Assert.True(fromYaml.Succeeded);
        Assert.Equal(fromDsl.CompiledRule!.CanonicalText, fromYaml.CompiledRule!.CanonicalText);
    }

    /// <summary>TryCompileYaml returns the rule for valid YAML text and false with diagnostics for an unknown predicate.</summary>
    [Fact]
    public void TryCompileYaml_ValidAndUnknownPredicate_ReturnsRuleOrFailure_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        Assert.True(compiler.TryCompileYaml("predicate: isManager", out CompiledRule<YamlTestContext>? rule, out _));
        Assert.Equal("isManager", rule.CanonicalText);
        Assert.False(
            compiler.TryCompileYaml(
                "predicate: neverRegistered",
                out CompiledRule<YamlTestContext>? none,
                out EquatableArray<Diagnostic> diagnostics
            )
        );
        Assert.Null(none);
        Assert.NotEmpty(diagnostics);
    }

    /// <summary>TryCompileYaml accepts a YamlNode extracted from a larger document.</summary>
    [Fact]
    public void TryCompileYaml_YamlNode_ReturnsRule_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );
        YamlStream stream = [];
        using StringReader reader = new("predicate: isManager");
        stream.Load(reader);

        bool ok = compiler.TryCompileYaml(stream.Documents[0].RootNode, out CompiledRule<YamlTestContext>? rule, out _);

        Assert.True(ok);
        Assert.NotNull(rule);
        Assert.Equal("isManager", rule.CanonicalText);
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

    /// <summary>PrintYaml to a writer writes exactly the YAML that the string form returns, and a failing writer's exception reaches the caller unchanged.</summary>
    [Fact]
    public void PrintYaml_ToWriter_WritesTheStringFormAndPropagatesWriterFailure_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddStringArgPredicate("hasRole", "role", "Y")
                .Build()
        );
        CompiledRule<YamlTestContext> rule = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        using StringWriter writer = new();
        IOException failure = new("disk full");
        using ThrowingWriter failing = new(failure);

        rule.PrintYaml(writer);
        IOException thrown = Assert.Throws<IOException>(() => rule.PrintYaml(failing));

        Assert.Equal(rule.PrintYaml(), writer.ToString());
        Assert.Same(failure, thrown);
        Assert.Throws<ArgumentNullException>(() => rule.PrintYaml(null!));
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

    /// <summary>A writer whose every write fails with a preset exception.</summary>
    private sealed class ThrowingWriter(Exception failure) : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(char value)
        {
            throw failure;
        }

        public override void Write(string? value)
        {
            throw failure;
        }
    }
}
