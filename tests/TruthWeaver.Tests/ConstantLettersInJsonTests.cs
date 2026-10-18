namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

public sealed class ConstantLettersInJsonTests
{
    /// <summary>
    /// A JSON <c>const</c> written as the letter <c>T</c>, <c>F</c> or <c>U</c> (either case) compiles to the same tree as the DSL
    /// text for that constant.
    /// </summary>
    [Theory]
    [InlineData("T", "T")]
    [InlineData("t", "T")]
    [InlineData("F", "F")]
    [InlineData("f", "F")]
    [InlineData("U", "U")]
    [InlineData("u", "U")]
    public void CompileJson_ConstantLetter_EqualsDslConstant_Test(string letter, string dslText)
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> fromJson = compiler.CompileJson($$"""{"const": "{{letter}}"}""");
        CompilationResult<RuleTestContext> fromDsl = compiler.Compile(dslText);

        Assert.True(fromJson.Succeeded);
        Assert.Equal(fromDsl.CompiledRule!.CanonicalText, fromJson.CompiledRule!.CanonicalText);
    }

    /// <summary>
    /// The words <c>true</c>, <c>false</c> and <c>unknown</c> keep working beside the letters.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData("False")]
    [InlineData("UNKNOWN")]
    public void CompileJson_ConstantWord_StillCompiles_Test(string word)
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        Assert.True(compiler.CompileJson($$"""{"const": "{{word}}"}""").Succeeded);
    }

    /// <summary>
    /// A string that is neither a word nor one of the three letters is still a diagnostic.
    /// </summary>
    [Fact]
    public void CompileJson_ConstantOtherLetter_ReportsDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        Assert.False(compiler.CompileJson("""{"const": "X"}""").Succeeded);
    }

    /// <summary>
    /// Emitting a rule with an Unknown constant and compiling the JSON again gives an equal tree, so the emitter's spelling
    /// still round-trips.
    /// </summary>
    [Fact]
    public void PrintJson_UnknownConstant_RoundTrips_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());
        CompiledRule<RuleTestContext> rule = compiler.Compile("U OR F").CompiledRule!;

        CompilationResult<RuleTestContext> again = compiler.CompileJson(rule.PrintJson());

        Assert.Equal(rule.CanonicalText, again.CompiledRule!.CanonicalText);
    }
}
