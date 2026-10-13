namespace TruthWeaver.Tool.Core.Tests;

using System.Text.Json;
using global::Json.Schema;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class ToolConfigurationTests
{
    private static readonly JsonSchema Schema = JsonSchema.FromFile(
        Path.Combine(AppContext.BaseDirectory, ToolConfigurationFile.SchemaFileName)
    );

    /// <summary>The configuration fixture reads every member, and a relative schema file is made absolute from the file's directory.</summary>
    [Fact]
    public void Load_ConfigurationFixture_ReadsEveryMember_Test()
    {
        ToolResult<ToolConfiguration> result = ToolConfigurationFile.Load(Fixtures.Path("config/truthweaver.json"));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            new ToolConfiguration
            {
                OperatorStyle = OperatorStyle.Symbolic,
                ConstantStyle = ConstantStyle.Letters,
                Dialect = EquationDialect.LaTeX,
                MaxDepth = 16,
                MaxNodeCount = 256,
                MaxAnalysisTerms = 12,
                MaxRewriteNodeCount = 5000,
                SchemaFile = Fixtures.Path("config/predicates.yaml"),
            },
            result.Value
        );
    }

    /// <summary>The configuration fixture is valid against the published JSON schema.</summary>
    [Fact]
    public void Schema_ConfigurationFixture_IsValid_Test()
    {
        using JsonDocument document = JsonDocument.Parse(Fixtures.Read("config/truthweaver.json"));

        EvaluationResults results = Schema.Evaluate(document.RootElement);

        Assert.True(results.IsValid);
    }

    /// <summary>The schema and the reader reject the same invalid documents.</summary>
    /// <param name="json">A configuration that both must reject.</param>
    [Theory]
    [InlineData("""{"operatorStyle": "fancy"}""")]
    [InlineData("""{"dialect": 3}""")]
    [InlineData("""{"limits": {"maxDepth": 0}}""")]
    [InlineData("""{"limits": {"maxNodeCount": 1.5}}""")]
    [InlineData("""{"limits": 32}""")]
    [InlineData("""{"schemaFile": ""}""")]
    public void Parse_InvalidValue_RejectedByReaderAndSchema_Test(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        ToolResult<ToolConfiguration> result = ToolConfigurationFile.Parse(json);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
        Assert.NotNull(diagnostic.Path);
        Assert.False(Schema.Evaluate(document.RootElement).IsValid);
    }

    /// <summary>Enumeration values ignore case.</summary>
    [Fact]
    public void Parse_EnumerationInOtherCase_IsAccepted_Test()
    {
        ToolResult<ToolConfiguration> result = ToolConfigurationFile.Parse(
            """{"dialect": "LaTeX", "constantStyle": "LOGICSYMBOLS"}"""
        );

        Assert.Equal(EquationDialect.LaTeX, result.Value?.Dialect);
        Assert.Equal(ConstantStyle.LogicSymbols, result.Value?.ConstantStyle);
    }

    /// <summary>An unknown property is a warning, and the reader ignores it; the schema rejects it.</summary>
    [Fact]
    public void Parse_UnknownProperty_WarnsAndIgnoresIt_Test()
    {
        const string json = """{"colour": "blue", "limits": {"maxWidth": 3}}""";
        using JsonDocument document = JsonDocument.Parse(json);

        ToolResult<ToolConfiguration> result = ToolConfigurationFile.Parse(json);

        Assert.True(result.Succeeded);
        Assert.Equal(ToolConfiguration.Empty, result.Value);
        Assert.All(result.Diagnostics, d => Assert.Equal(ToolDiagnosticCodes.UnknownConfigurationProperty, d.Code));
        Assert.Equal(["$.colour", "$.limits.maxWidth"], result.Diagnostics.Select(d => d.Path));
        Assert.False(Schema.Evaluate(document.RootElement).IsValid);
    }

    /// <summary>A document that is not JSON, or not an object, is an error.</summary>
    /// <param name="json">The text.</param>
    /// <param name="code">The expected code.</param>
    [Theory]
    [InlineData("{", ToolDiagnosticCodes.MalformedDocument)]
    [InlineData("[]", ToolDiagnosticCodes.InvalidConfiguration)]
    public void Parse_NotAnObject_ReportsError_Test(string json, string code)
    {
        ToolResult<ToolConfiguration> result = ToolConfigurationFile.Parse(json);

        Assert.Equal(code, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>Find returns the configuration file of the nearest directory at or above the start directory.</summary>
    [Fact]
    public void Find_FromNestedDirectory_FindsNearestFile_Test()
    {
        string? found = ToolConfigurationFile.Find(Fixtures.Path("config/nested"));

        Assert.Equal(Fixtures.Path("config/truthweaver.json"), found);
    }

    /// <summary>Find returns null when no directory up to the root has the file.</summary>
    [Fact]
    public void Find_NoFileAbove_ReturnsNull_Test()
    {
        using TemporaryDirectory directory = new();

        Assert.Null(ToolConfigurationFile.Find(directory.FullPath));
    }

    /// <summary>For each value the command line wins over the configuration file, which wins over the defaults.</summary>
    [Fact]
    public void Resolve_ThreeLayers_CommandLineOverFileOverDefaults_Test()
    {
        ToolConfiguration file = new()
        {
            OperatorStyle = OperatorStyle.Symbolic,
            Dialect = EquationDialect.LaTeX,
            MaxDepth = 16,
        };
        ToolConfiguration commandLine = new() { OperatorStyle = OperatorStyle.CStyle, MaxNodeCount = 64 };

        ToolSettings settings = ToolSettings.Resolve(file, commandLine);

        Assert.Equal(
            new ToolSettings
            {
                OperatorStyle = OperatorStyle.CStyle,
                ConstantStyle = null,
                Dialect = EquationDialect.LaTeX,
                MaxDepth = 16,
                MaxNodeCount = 64,
                MaxAnalysisTerms = CompilerOptions.Default.MaxAnalysisTerms,
                MaxRewriteNodeCount = CompilerOptions.Default.MaxRewriteNodeCount,
                SchemaFile = null,
            },
            settings
        );
    }

    /// <summary>With no layers the settings are the defaults, and the compiler options are the compiler defaults in lenient mode.</summary>
    [Fact]
    public void Resolve_NoLayers_GivesDefaultsAndLenientCompilerDefaults_Test()
    {
        ToolSettings settings = ToolSettings.Resolve(null, null);

        Assert.Equal(ToolSettings.Default, settings);
        Assert.Equal(CompilerOptions.Default with { Mode = CompilationMode.Lenient }, settings.ToCompilerOptions());
    }
}
