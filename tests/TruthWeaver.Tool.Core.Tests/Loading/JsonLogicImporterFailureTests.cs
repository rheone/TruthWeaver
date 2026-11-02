namespace TruthWeaver.Tool.Core.Tests.Loading;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;

/// <summary>The shapes of a JsonLogic document that the importer refuses, or imports with a warning.</summary>
public sealed class JsonLogicImporterFailureTests
{
    /// <summary>A null document is a falsy condition: the import gives the constant Unknown and warns.</summary>
    [Fact]
    public void Import_NullDocument_GivesUnknownWithAWarning_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("null");

        Assert.True(result.Succeeded);
        Assert.Equal("""{"const":"unknown"}""", result.Value);
        Assert.Equal(ToolDiagnosticCodes.JsonLogicBehaviorDiffers, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>An object with two keys is not an operation, so the import fails at the object.</summary>
    [Fact]
    public void Import_ObjectWithTwoKeys_FailsWithTheObjectPath_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"and": [true], "or": [true]}""");

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.UnsupportedJsonLogic, error.Code);
        Assert.Equal("$", error.Path);
    }

    /// <summary>A number cannot stand where a truth value is expected.</summary>
    [Fact]
    public void Import_NumberAsCondition_Fails_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"and": [true, 1]}""");

        Assert.False(result.Succeeded);
        Assert.Equal("$.and[1]", Assert.Single(result.Diagnostics).Path);
    }

    /// <summary>Each unsupported operator is reported on its own, not only the first.</summary>
    [Fact]
    public void Import_TwoUnsupportedOperators_ReportsEachOccurrence_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"and": [{"+": [1]}, {"map": [1]}]}""");

        Assert.False(result.Succeeded);
        Assert.Equal(
            ["$.and[0].+", "$.and[1].map"],
            result.Diagnostics.Where(d => d.Code == ToolDiagnosticCodes.UnsupportedJsonLogic).Select(d => d.Path)
        );
    }

    /// <summary>An and or an or with no argument has no mapping.</summary>
    [Fact]
    public void Import_EmptyAnd_Fails_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"and": []}""");

        Assert.False(result.Succeeded);
        Assert.Contains("at least one", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }

    /// <summary>A variable path with an empty segment cannot give a term name.</summary>
    [Fact]
    public void Import_VariablePathWithEmptySegment_Fails_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{">": [{"var": "a..b"}, 1]}""");

        Assert.False(result.Succeeded);
        Assert.Contains("empty segment", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }

    /// <summary>An ordering comparison with null has no definite mapping and fails.</summary>
    [Fact]
    public void Import_OrderingComparisonWithNull_Fails_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{">": [{"var": "n"}, null]}""");

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("null", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A variable default is dropped, and the import warns that a missing value is Unknown instead.</summary>
    [Fact]
    public void Import_VariableWithDefault_WarnsThatTheDefaultIsNotUsed_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"==": [{"var": ["n", 5]}, 1]}""");

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("default", StringComparison.Ordinal));
    }

    /// <summary>A variable whose path is not a string fails the import.</summary>
    [Fact]
    public void Import_VariablePathNotAString_Fails_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"===": [{"var": 3}, 1]}""");

        Assert.False(result.Succeeded);
        Assert.Contains("variable path", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }
}
