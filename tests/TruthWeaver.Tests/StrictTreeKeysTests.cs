namespace TruthWeaver.Tests;

using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// A JSON or YAML rule tree rejects a key the shape does not define, and every front end rejects a predicate argument
/// that is named twice, so a typo never turns into a silently ignored setting.
/// </summary>
public sealed class StrictTreeKeysTests
{
    /// <summary>A key that the node kind does not define is a malformed tree, reported at the key's path.</summary>
    [Theory]
    [InlineData("""{"predicate":"isManager","bogus":1}""", "$.bogus")]
    [InlineData("""{"const":true,"bogus":1}""", "$.bogus")]
    [InlineData(
        """{"op":"and","operands":[{"predicate":"isManager"},{"predicate":"isDepartmentHead"}],"bogus":1}""",
        "$.bogus"
    )]
    public void CompileJson_UnknownKey_ReportsMalformedTreeAtTheKey_Test(string json, string path)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileJson(json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal(path, diagnostic.Path);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A node that has both <c>predicate</c> and <c>op</c> is rejected, not read as a predicate.</summary>
    [Fact]
    public void CompileJson_PredicateAndOpTogether_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson("""{"predicate":"isManager","op":"and","operands":[]}""");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree && d.Path == "$.op");
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A threshold key on an operator that takes none is rejected.</summary>
    [Theory]
    [InlineData("k")]
    [InlineData("min")]
    [InlineData("max")]
    public void CompileJson_StrayThresholdKeyOnAnd_ReportsMalformedTree_Test(string key)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson(
                $$"""{"op":"and","{{key}}":1,"operands":[{"predicate":"isManager"},{"predicate":"isDepartmentHead"}]}"""
            );

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal($"$.{key}", diagnostic.Path);
    }

    /// <summary>
    /// A threshold operator accepts <c>k</c> but not <c>min</c>, and <c>between</c> accepts <c>min</c> and <c>max</c>
    /// but not <c>k</c>.
    /// </summary>
    [Theory]
    [InlineData(
        """{"op":"atLeast","k":1,"min":0,"operands":[{"predicate":"isManager"},{"predicate":"isDepartmentHead"}]}""",
        "$.min"
    )]
    [InlineData(
        """{"op":"between","min":0,"max":1,"k":1,"operands":[{"predicate":"isManager"},{"predicate":"isDepartmentHead"}]}""",
        "$.k"
    )]
    public void CompileJson_BoundKeyOfAnotherOperator_ReportsMalformedTree_Test(string json, string path)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileJson(json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal(path, diagnostic.Path);
    }

    /// <summary>A misspelt <c>args</c> is rejected and the suggestion names the right key.</summary>
    [Fact]
    public void CompileJson_MisspeltArgs_ReportsMalformedTreeWithASuggestion_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson("""{"predicate":"hasRole","arg":{"role":"Y"}}""");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$.arg", diagnostic.Path);
        Assert.NotNull(diagnostic.Suggestion);
        Assert.Contains("args", diagnostic.Suggestion.Text, StringComparison.Ordinal);
    }

    /// <summary>The same unknown key is rejected in a YAML rule.</summary>
    [Fact]
    public void CompileYaml_UnknownKey_ReportsMalformedTreeAtTheKey_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileYaml("predicate: isManager\nbogus: 1\n");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$.bogus", diagnostic.Path);
    }

    /// <summary>A node with every key it defines still compiles.</summary>
    [Fact]
    public void CompileJson_OnlyDefinedKeys_Compiles_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson(
                """{"op":"atLeast","k":1,"operands":[{"predicate":"hasRole","args":{"role":"Y"}},{"predicate":"isManager"}]}"""
            );

        Assert.True(result.Succeeded);
    }

    /// <summary>A predicate argument named twice in the rule text is an error, not last-wins.</summary>
    [Fact]
    public void Compile_DuplicateArgumentInRuleText_ReportsDuplicateArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("hasRole(role: \"Y\", role: \"Z\")");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.DuplicateArgument, diagnostic.Code);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A predicate argument named twice in a JSON rule is an error, reported at the second occurrence.</summary>
    [Fact]
    public void CompileJson_DuplicateArgument_ReportsDuplicateArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson("""{"predicate":"hasRole","args":{"role":"Y","role":"Z"}}""");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.DuplicateArgument, diagnostic.Code);
        Assert.Equal("$.args.role", diagnostic.Path);
    }

    /// <summary>A predicate argument named twice in a YAML rule is a duplicate-argument error at the repeated key.</summary>
    [Fact]
    public void CompileYaml_DuplicateArgument_ReportsDuplicateArgument_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileYaml("predicate: hasRole\nargs:\n  role: Y\n  role: Z\n");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.DuplicateArgument, diagnostic.Code);
        Assert.Equal("$.args.role", diagnostic.Path);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A key repeated outside <c>args</c> in a YAML rule stays a malformed tree, not a duplicate argument.</summary>
    [Fact]
    public void CompileYaml_DuplicateKeyOutsideArgs_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileYaml("predicate: hasRole\npredicate: isManager\n");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
    }

    /// <summary>A predicate argument named twice in a builder is an error, not last-wins.</summary>
    [Fact]
    public void Compile_BuilderDuplicateArgument_ReportsDuplicateArgument_Test()
    {
        RuleBuilder builder = RuleBuilder.And(
            RuleBuilder.Predicate("isManager"),
            RuleBuilder.Predicate("hasRole", ("role", "Y"), ("role", "Z"))
        );

        CompilationResult<RuleTestContext> result = builder.Compile(CreateCompiler());

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.DuplicateArgument, diagnostic.Code);
        Assert.Equal("$.operands[1].args.role", diagnostic.Path);
        Assert.Null(result.CompiledRule);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
