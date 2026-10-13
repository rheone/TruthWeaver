namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Tests.TestSupport;
using TruthWeaver.Yaml;

public sealed class RuleConverterTests
{
    /// <summary>Gets every pair of a source fixture and a target format.</summary>
    public static TheoryData<string, RuleFormat> Directions =>
        new()
        {
            { "access.rule", RuleFormat.Text },
            { "access.rule", RuleFormat.Json },
            { "access.rule", RuleFormat.Yaml },
            { "access.json", RuleFormat.Text },
            { "access.json", RuleFormat.Json },
            { "access.json", RuleFormat.Yaml },
            { "access.yaml", RuleFormat.Text },
            { "access.yaml", RuleFormat.Json },
            { "access.yaml", RuleFormat.Yaml },
        };

    /// <summary>A conversion in any direction gives text that compiles to the same tree and the same notes as the source.</summary>
    /// <param name="fixture">The source fixture.</param>
    /// <param name="to">The target format.</param>
    [Theory]
    [MemberData(nameof(Directions))]
    public void Convert_FixtureToEachFormat_KeepsTreeAndNotes_Test(string fixture, RuleFormat to)
    {
        LoadedRule source = RuleLoader.LoadFile(Fixtures.Path(fixture)).Value!;

        ToolResult<string> converted = RuleConverter.Convert(source, to);

        Assert.True(converted.Succeeded);
        CompiledRule<StubAssignment> original = RuleConverter.Compile(source).CompiledRule!;
        CompiledRule<StubAssignment>? roundTripped = RuleConverter.CreateCompiler().CompileAs(converted.Value, to).CompiledRule;
        Assert.NotNull(roundTripped);
        Assert.Equal(original.CanonicalText, roundTripped.CanonicalText);
        Assert.Equal("Editors who are active.", roundTripped.Notes["$"]);
    }

    /// <summary>Converting without notes drops them and keeps the tree.</summary>
    [Fact]
    public void Convert_WithoutNotes_DropsNotes_Test()
    {
        LoadedRule source = RuleLoader.LoadFile(Fixtures.Path("access.rule")).Value!;

        ToolResult<string> converted = RuleConverter.Convert(source, RuleFormat.Text, includeNotes: false);

        Assert.Equal("isActive AND hasRole(role: \"editor\")", converted.Value);
    }

    /// <summary>The tool compiles permissively: any predicate, any argument and an undeclared data source compile.</summary>
    [Fact]
    public void Compile_UnregisteredPredicatesAndDataSource_Compiles_Test()
    {
        LoadedRule source = RuleLoader
            .LoadString("anything(level: 3, since: \"2026-01-01\") OR hasRole(role: from(\"user\", \"$.role\"))")
            .Value!;

        CompilationResult<StubAssignment> result = RuleConverter.Compile(source);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "anything(level: 3, since: \"2026-01-01\") OR hasRole(role: from(\"user\", \"$.role\"))",
            result.CompiledRule!.CanonicalText
        );
    }

    /// <summary>A rule that does not compile gives no text and the compiler's diagnostics, with spans into the rule.</summary>
    [Fact]
    public void Convert_SyntaxError_ReturnsCompilerDiagnostics_Test()
    {
        LoadedRule source = RuleLoader.LoadString("isActive AND (").Value!;

        ToolResult<string> converted = RuleConverter.Convert(source, RuleFormat.Json);

        Assert.False(converted.Succeeded);
        Assert.Null(converted.Value);
        Assert.Contains(converted.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>The limits of the settings apply: a rule deeper than the depth limit does not compile.</summary>
    [Fact]
    public void Compile_SettingsDepthLimit_RejectsDeeperRule_Test()
    {
        LoadedRule source = RuleLoader.LoadString("NOT NOT NOT isActive").Value!;

        CompilationResult<StubAssignment> result = RuleConverter.Compile(source, new ToolSettings { MaxDepth = 2 });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }
}
