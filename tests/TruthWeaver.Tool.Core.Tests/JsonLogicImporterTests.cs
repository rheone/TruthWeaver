namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Diagnostics;

public sealed class JsonLogicImporterTests
{
    /// <summary>A comparison of a variable with a literal becomes a term named from the variable path and the catalog member.</summary>
    [Fact]
    public void Import_VariableComparedWithLiteral_GivesNamedTermWithValue_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{">=": [{"var": "user.age"}, 18]}""");

        Assert.True(result.Succeeded);
        Assert.Equal("""{"predicate":"userAgeIsGreaterThanOrEqualTo","args":{"value":18}}""", result.Value);
    }

    /// <summary>A literal on the left flips the comparison, so the term still reads the variable first.</summary>
    [Fact]
    public void Import_LiteralOnTheLeft_FlipsTheComparison_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"<": [18, {"var": "age"}]}""");

        Assert.Equal("""{"predicate":"ageIsGreaterThan","args":{"value":18}}""", result.Value);
    }

    /// <summary>And, or and not map to the K3 operators, and one operand of and or is the operand itself.</summary>
    [Fact]
    public void Import_AndOrNot_MapsToOperators_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"or": [{"and": [true]}, {"!": false}]}""");

        Assert.Equal("""{"op":"or","operands":[{"const":true},{"op":"not","operands":[{"const":false}]}]}""", result.Value);
    }

    /// <summary>An if with a condition list nests the later pairs in the else position.</summary>
    [Fact]
    public void Import_IfChain_NestsInTheElsePosition_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"if": [true, false, false, true, true]}""");

        Assert.Equal(
            """{"op":"if","operands":[{"const":true},{"const":false},{"op":"if","operands":[{"const":false},{"const":true},{"const":true}]}]}""",
            result.Value
        );
    }

    /// <summary>An if with no else branch uses Unknown and warns that JsonLogic gives null there.</summary>
    [Fact]
    public void Import_IfWithoutElse_UsesUnknownAndWarns_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"if": [true, true]}""");

        Assert.True(result.Succeeded);
        Assert.Contains("""{"const":"unknown"}""", result.Value, StringComparison.Ordinal);
        Assert.Contains(result.Diagnostics, d => d.Code == ToolDiagnosticCodes.JsonLogicBehaviorDiffers);
    }

    /// <summary>A variable used as a condition keeps the term and warns that JsonLogic reads it by truthiness.</summary>
    [Fact]
    public void Import_VariableAsCondition_WarnsAboutTruthiness_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"and": [{"var": "active"}, true]}""");

        Assert.True(result.Succeeded);
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.JsonLogicBehaviorDiffers, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("$.and[0].var", warning.Path);
    }

    /// <summary>A missing variable is warned about once for each variable, however many comparisons read it.</summary>
    [Fact]
    public void Import_SameVariableTwice_WarnsAboutMissingValueOnce_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import(
            """{"and": [{">": [{"var": "n"}, 1]}, {"<": [{"var": "n"}, 9]}]}"""
        );

        Assert.Single(result.Diagnostics, d => d.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A loose equality warns, and the strict one does not warn about type conversion.</summary>
    [Fact]
    public void Import_LooseAndStrictEquality_WarnsOnlyForLoose_Test()
    {
        ToolResult<string> loose = JsonLogicImporter.Import("""{"==": [{"var": "n"}, 1]}""");
        ToolResult<string> strict = JsonLogicImporter.Import("""{"===": [{"var": "n"}, 1]}""");

        Assert.Contains(loose.Diagnostics, d => d.Message.Contains("loose", StringComparison.Ordinal));
        Assert.DoesNotContain(strict.Diagnostics, d => d.Message.Contains("loose", StringComparison.Ordinal));
    }

    /// <summary>A comparison with null becomes the definite null test.</summary>
    [Fact]
    public void Import_ComparisonWithNull_GivesNullTests_Test()
    {
        ToolResult<string> isNull = JsonLogicImporter.Import("""{"===": [{"var": "a"}, null]}""");
        ToolResult<string> isNotNull = JsonLogicImporter.Import("""{"!==": [{"var": "a"}, null]}""");

        Assert.Equal("""{"predicate":"aIsNull"}""", isNull.Value);
        Assert.Equal("""{"predicate":"aIsNotNull"}""", isNotNull.Value);
    }

    /// <summary>Three operands with the inclusive operator give a between term; with the strict one, two strict comparisons.</summary>
    [Fact]
    public void Import_RangeComparison_GivesBetweenOrTwoComparisons_Test()
    {
        ToolResult<string> inclusive = JsonLogicImporter.Import("""{"<=": [1, {"var": "n"}, 9]}""");
        ToolResult<string> strict = JsonLogicImporter.Import("""{"<": [1, {"var": "n"}, 9]}""");

        Assert.Equal("""{"predicate":"nIsBetween","args":{"lower":1,"upper":9}}""", inclusive.Value);
        Assert.Equal(
            """{"op":"and","operands":[{"predicate":"nIsGreaterThan","args":{"value":1}},{"predicate":"nIsLessThan","args":{"value":9}}]}""",
            strict.Value
        );
    }

    /// <summary>The in operator maps to the set and collection members.</summary>
    [Fact]
    public void Import_InOperator_GivesIsInAndContains_Test()
    {
        ToolResult<string> inSet = JsonLogicImporter.Import("""{"in": [{"var": "role"}, ["a", "b"]]}""");
        ToolResult<string> inCollection = JsonLogicImporter.Import("""{"in": ["a", {"var": "roles"}]}""");

        Assert.Equal("""{"predicate":"roleIsIn","args":{"values":["a","b"]}}""", inSet.Value);
        Assert.Equal("""{"predicate":"rolesContains","args":{"value":"a"}}""", inCollection.Value);
    }

    /// <summary>An unsupported operator fails the import with an error that names the operator and the path, and gives no value.</summary>
    [Fact]
    public void Import_UnsupportedOperator_FailsWithNamedError_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"and": [true, {"+": [1, 2]}]}""");

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.UnsupportedJsonLogic, error.Code);
        Assert.Contains("'+'", error.Message, StringComparison.Ordinal);
        Assert.Equal("$.and[1].+", error.Path);
    }

    /// <summary>A comparison of two variables has no mapping and fails the import.</summary>
    [Fact]
    public void Import_TwoVariables_FailsTheImport_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"==": [{"var": "a"}, {"var": "b"}]}""");

        Assert.False(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.UnsupportedJsonLogic, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A document that is not JSON is a malformed-document error.</summary>
    [Fact]
    public void Import_MalformedJson_ReportsMalformedDocument_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("{");

        Assert.False(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A variable path with characters that a term name cannot hold gets underscores in the name.</summary>
    [Fact]
    public void Import_VariablePathWithOddCharacters_SanitizesTheName_Test()
    {
        ToolResult<string> result = JsonLogicImporter.Import("""{"===": [{"var": "items.0.first-name"}, "x"]}""");

        Assert.Contains("items0First_nameIsEqualTo", result.Value, StringComparison.Ordinal);
    }
}
