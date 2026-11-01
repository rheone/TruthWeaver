namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// In <see cref="CompilationMode.Lenient"/>, an argument of an unregistered predicate keeps the value as
/// written or the compile reports <see cref="DiagnosticCodes.ArgumentTypeMismatch"/>. It never becomes <c>0</c> or an
/// empty array.
/// </summary>
public sealed class LenientLiteralGuessingTests
{
    /// <summary>
    /// A number in exponent form is kept by its value, so <c>limit(max: 1e5)</c> and <c>limit(max: 0)</c> stay two
    /// different terms and the converted rule holds 100000, not 0.
    /// </summary>
    [Fact]
    public void CompileJson_LenientExponentFormNumber_KeepsTheValue_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileJson(
            """{ "predicate": "limit", "args": { "max": 1e5 } }"""
        );

        Assert.True(result.Succeeded);
        Assert.Contains("max: 100000", result.CompiledRule!.PrintRuleText());
    }

    /// <summary>A YAML number in exponent form is kept by its value, like the JSON form.</summary>
    [Fact]
    public void CompileYaml_LenientExponentFormNumber_KeepsTheValue_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(
            """
            predicate: limit
            args:
              max: 1e5
            """
        );

        Assert.True(result.Succeeded);
        Assert.Contains("max: 100000", result.CompiledRule!.PrintRuleText());
    }

    /// <summary>
    /// An integer beyond the range of <see cref="long"/> that a <see cref="decimal"/> holds exactly, such as a large
    /// identifier, keeps all of its digits.
    /// </summary>
    [Fact]
    public void Compile_LenientIntegerBeyondInt64_KeepsAllDigits_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("lookup(id: 99999999999999999999)");

        Assert.True(result.Succeeded);
        Assert.Contains("id: 99999999999999999999", result.CompiledRule!.PrintRuleText());
    }

    /// <summary>
    /// A number that neither <see cref="long"/> nor <see cref="decimal"/> holds exactly is reported, with the path of
    /// the argument. It does not become <c>0</c>.
    /// </summary>
    [Fact]
    public void CompileJson_LenientNumberBeyondDecimal_ReportsArgumentTypeMismatch_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileJson(
            """{ "predicate": "limit", "args": { "max": 1e400 } }"""
        );

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.ArgumentTypeMismatch);
        Assert.Contains("max", diagnostic.Message);
        Assert.EndsWith("max", diagnostic.Path);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// Two terms whose numbers cannot be kept do not collapse into one term. The compile fails with the diagnostic and
    /// the analyzer reports no contradiction between them.
    /// </summary>
    [Fact]
    public void Compile_LenientTermsDifferingByUnkeepableNumber_ReportsNoContradiction_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile(
            "foo(n: 100000000000000000000000000000) AND NOT foo(n: 0)"
        );

        Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.ArgumentTypeMismatch);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// An array whose elements have different kinds is reported. It is not turned into strings, and it is not turned
    /// into an empty array.
    /// </summary>
    [Theory]
    [InlineData("levels: [1, 2.5]")]
    [InlineData("tags: [\"a\", 1]")]
    [InlineData("flags: [true, 1]")]
    public void Compile_LenientMixedKindArray_ReportsArgumentTypeMismatch_Test(string argument)
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile($"check({argument})");

        Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.ArgumentTypeMismatch);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A list inside a list has no exact element kind to keep, so the compile reports it instead of emptying the list.</summary>
    [Fact]
    public void CompileJson_LenientNestedList_ReportsArgumentTypeMismatch_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileJson(
            """{ "predicate": "grid", "args": { "cells": [[1], [2]] } }"""
        );

        Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.ArgumentTypeMismatch);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A literal that has an exact kind still compiles, and two terms that differ in one literal print as two terms.</summary>
    [Fact]
    public void Compile_LenientExactLiterals_CompileAndStayTwoTerms_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateLenientCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile(
            "foo(n: 1, tags: [\"a\", \"b\"], ratio: 1.5) AND NOT foo(n: 2, tags: [\"a\", \"b\"], ratio: 1.5)"
        );

        Assert.True(result.Succeeded);
        string printed = result.CompiledRule!.PrintRuleText();
        Assert.Contains("n: 1", printed);
        Assert.Contains("n: 2", printed);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    private static RuleCompiler<RuleTestContext> CreateLenientCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );
    }
}
