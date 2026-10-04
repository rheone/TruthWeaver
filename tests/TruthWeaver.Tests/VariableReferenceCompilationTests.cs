namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Variable references in the DSL (ADR-0006, data-sources ticket 01): parsing, printing, compile-time source
/// declaration and the analyzer's term identity.
/// </summary>
public sealed class VariableReferenceCompilationTests
{
    private const string AgeRule = "ageAtLeast(min: from(\"user\", \"$.minAge\"))";

    /// <summary>A rule using <c>from(...)</c> prints back to the same canonical text and recompiles to the same text.</summary>
    [Fact]
    public void Compile_VariableReference_RoundTripsThroughCanonicalPrinter_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> first = compiler.Compile(AgeRule);
        CompilationResult<RuleTestContext> second = compiler.Compile(first.CompiledRule!.CanonicalText);

        Assert.True(first.Succeeded);
        Assert.Equal(AgeRule, first.CompiledRule.CanonicalText);
        Assert.Equal(first.CompiledRule.CanonicalText, second.CompiledRule!.CanonicalText);
    }

    /// <summary>A query containing quotes and backslashes survives the DSL escapes both ways.</summary>
    [Fact]
    public void Compile_QueryWithEscapes_RoundTripsThroughCanonicalPrinter_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");
        const string text = "ageAtLeast(min: from(\"user\", \"$.a[?@.n==\\\"x\\\\y\\\"]\"))";

        CompilationResult<RuleTestContext> result = compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(text, result.CompiledRule!.CanonicalText);
    }

    /// <summary>A variable may be mixed with literal arguments in one term, in any written order.</summary>
    [Fact]
    public void Compile_VariableAndLiteralArguments_AreSortedByNameInCanonicalText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile("inRange(max: 10, min: from(\"user\", \"$.low\"))");

        Assert.True(result.Succeeded);
        Assert.Equal("inRange(max: 10, min: from(\"user\", \"$.low\"))", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A source name that was not declared is a <c>TRE0024</c> error at the reference.</summary>
    [Fact]
    public void Compile_UndeclaredSource_ReportsUndeclaredDataSourceError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("request");

        CompilationResult<RuleTestContext> result = compiler.Compile(AgeRule);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("'user'", diagnostic.Found);
        Assert.Equal(AgeRule.IndexOf("from(", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>A misspelt source name gets a "did you mean" naming the declared source.</summary>
    [Fact]
    public void Compile_MisspeltSource_SuggestsTheDeclaredName_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user", "request");

        CompilationResult<RuleTestContext> result = compiler.Compile(
            AgeRule.Replace("\"user\"", "\"usr\"", StringComparison.Ordinal)
        );

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
        Assert.NotNull(diagnostic.Suggestion);
        Assert.Equal(DiagnosticSuggestionKind.Replacement, diagnostic.Suggestion.Kind);
        Assert.Contains("user", diagnostic.Suggestion.Text, StringComparison.Ordinal);
    }

    /// <summary>A compiler given no declared sources rejects every variable reference and says how to declare one.</summary>
    [Fact]
    public void Compile_NoDeclaredSources_ReportsUndeclaredWithADeclareHint_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile(AgeRule);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
        Assert.Equal(DiagnosticSuggestionKind.Hint, diagnostic.Suggestion!.Kind);
        Assert.Contains("CompilerOptions.DataSources", diagnostic.Suggestion.Text, StringComparison.Ordinal);
    }

    /// <summary>A rule with only literal arguments compiles exactly as before whether or not sources are declared.</summary>
    [Fact]
    public void Compile_LiteralOnlyRule_IsUnaffectedByDeclaredSources_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile("ageAtLeast(min: 18)");

        Assert.True(result.Succeeded);
        Assert.Equal("ageAtLeast(min: 18)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A variable inside an array literal is a syntax error: only a whole argument can be a reference.</summary>
    [Fact]
    public void Compile_VariableInsideArrayLiteral_IsASyntaxError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile("hasAnyRole(roles: [from(\"user\", \"$.role\")])");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>A bare <c>from</c> with no call is not a reference and is rejected.</summary>
    [Fact]
    public void Compile_BareFromWord_IsASyntaxError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile("ageAtLeast(min: from)");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>A reference missing its query is a syntax error naming the query, not an undeclared source.</summary>
    [Fact]
    public void Compile_ReferenceMissingQuery_IsASyntaxError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile("ageAtLeast(min: from(\"user\"))");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>A reference whose source is not a quoted string is a syntax error.</summary>
    [Fact]
    public void Compile_ReferenceWithUnquotedSource_IsASyntaxError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile("ageAtLeast(min: from(user, \"$.a\"))");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>Two identical references in one rule are a single analyzer variable.</summary>
    [Fact]
    public void DistinctTerms_IdenticalReferences_AreOneVariable_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompiledRule<RuleTestContext> rule = compiler.Compile($"{AgeRule} OR NOT {AgeRule}").CompiledRule!;

        Assert.Single(Analyzer.DistinctTerms(rule.Root));
    }

    /// <summary>References that differ only in their query are different variables even though they might resolve alike.</summary>
    [Fact]
    public void DistinctTerms_DifferingQueries_AreDifferentVariables_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompiledRule<RuleTestContext> rule = compiler
            .Compile($"{AgeRule} OR {AgeRule.Replace("minAge", "limit", StringComparison.Ordinal)}")
            .CompiledRule!;

        Assert.Equal(2, Analyzer.DistinctTerms(rule.Root).Count);
    }

    /// <summary>References that differ only in their source name are different variables.</summary>
    [Fact]
    public void DistinctTerms_DifferingSources_AreDifferentVariables_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user", "request");

        CompiledRule<RuleTestContext> rule = compiler
            .Compile($"{AgeRule} OR {AgeRule.Replace("\"user\"", "\"request\"", StringComparison.Ordinal)}")
            .CompiledRule!;

        Assert.Equal(2, Analyzer.DistinctTerms(rule.Root).Count);
    }

    /// <summary>The analyzer does not call <c>A AND NOT A</c> a contradiction for a variable term, matching literal terms.</summary>
    [Fact]
    public void Compile_VariableTermAndItsNegation_IsNotReportedAsContradiction_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.Compile($"{AgeRule} AND NOT {AgeRule}");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    /// <summary>The JSON printer writes a variable argument as an object with <c>from</c> and <c>query</c>.</summary>
    [Fact]
    public void PrintJson_VariableArgument_IsWrittenAsFromAndQuery_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        string json = compiler.Compile(AgeRule).CompiledRule!.PrintJson();

        Assert.Contains("\"from\":\"user\"", json, StringComparison.Ordinal);
        Assert.Contains("\"query\":\"$.minAge\"", json, StringComparison.Ordinal);
    }

    /// <summary>The outline shows a variable argument by its reference text.</summary>
    [Fact]
    public void Outline_VariableArgument_ShowsTheReference_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        OutlineNode outline = compiler.Compile(AgeRule).CompiledRule!.Outline();

        Assert.Equal("min: from(\"user\", \"$.minAge\")", outline.ArgumentText);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler(params string[] declaredSources)
    {
        DataSourceDeclarations declarations = [];
        foreach (string name in declaredSources)
        {
            declarations.Add(name);
        }

        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "ageAtLeast",
                    "Age at least",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("min", "The minimum age.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Add(
                new PredicateSchema(
                    "inRange",
                    "In range",
                    "Test predicate, always true.",
                    [
                        new PredicateArgumentSchema("min", "The lower bound.", LiteralKind.Int64),
                        new PredicateArgumentSchema("max", "The upper bound.", LiteralKind.Int64),
                    ]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Add(
                new PredicateSchema(
                    "hasAnyRole",
                    "Has any role",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("roles", "The accepted roles.", LiteralKind.StringArray)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
        return new RuleCompiler<RuleTestContext>(registry, new CompilerOptions(DataSources: declarations));
    }
}
