namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>The predicate schema file declares predicates, their arguments and the kinds of the arguments.</summary>
public sealed class PredicateSchemaFileTests
{
    /// <summary>The fixture declares each predicate with its arguments, kinds and required flags, in file order.</summary>
    [Fact]
    public void Load_Fixture_ReadsEveryPredicateAndArgument_Test()
    {
        ToolResult<PredicateSchemaFile> result = PredicateSchemaFile.Load(Fixtures.Path("config/predicates.yaml"));

        Assert.True(result.Succeeded);
        IReadOnlyList<PredicateSchema> predicates = result.Value.Predicates;
        Assert.Equal(["isActive", "hasRole", "inGroup"], predicates.Select(p => p.Name));
        Assert.Equal("Is the user active?", predicates[0].Description);
        Assert.Empty(predicates[0].Arguments);
        PredicateArgumentSchema role = Assert.Single(predicates[1].Arguments);
        Assert.Equal(("role", LiteralKind.String, true), (role.Name, role.Type, role.Required));
        PredicateArgumentSchema since = predicates[2].Arguments[1];
        Assert.Equal(("since", LiteralKind.DateTimeOffset, false), (since.Name, since.Type, since.Required));
    }

    /// <summary>A kind is matched by name with any case, including the array kinds.</summary>
    [Fact]
    public void Parse_KindInAnyCase_ReadsTheKind_Test()
    {
        ToolResult<PredicateSchemaFile> result = PredicateSchemaFile.Parse(
            "predicates:\n  p:\n    arguments:\n      ids: GUIDARRAY\n      n: int64\n"
        );

        Assert.Equal([LiteralKind.GuidArray, LiteralKind.Int64], result.Value!.Predicates[0].Arguments.Select(a => a.Type));
    }

    /// <summary>Each problem in a schema file is an invalid-schema-file error at its path.</summary>
    /// <param name="yaml">The schema file.</param>
    /// <param name="path">The path of the error.</param>
    [Theory]
    [InlineData("predicates:\n  p:\n    arguments:\n      a: text\n", "$.predicates['p'].arguments['a']")]
    [InlineData("predicates:\n  p:\n    arguments:\n      a: '3'\n", "$.predicates['p'].arguments['a']")]
    [InlineData("predicates:\n  p:\n    returns: bool\n", "$.predicates['p']")]
    [InlineData(
        "predicates:\n  p:\n    arguments:\n      a: { kind: string, required: maybe }\n",
        "$.predicates['p'].arguments['a']"
    )]
    [InlineData("predicate:\n  p: {}\n", "$")]
    [InlineData("predicates:\n  AND: {}\n", "$.predicates['AND']")]
    [InlineData("predicates:\n  p: {}\n  P: {}\n", "$.predicates['P']")]
    [InlineData("- p\n", "$")]
    public void Parse_InvalidDeclaration_ReportsInvalidSchemaFile_Test(string yaml, string path)
    {
        ToolResult<PredicateSchemaFile> result = PredicateSchemaFile.Parse(yaml);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.InvalidSchemaFile, error.Code);
        Assert.Equal(path, error.Path);
    }

    /// <summary>A schema file that is not well-formed YAML is a malformed-document error.</summary>
    [Fact]
    public void Parse_MalformedYaml_ReportsMalformedDocument_Test()
    {
        ToolResult<PredicateSchemaFile> result = PredicateSchemaFile.Parse("predicates: [a\n");

        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A schema file that does not exist is an unreadable-input error.</summary>
    [Fact]
    public void Load_MissingFile_ReportsInputUnreadable_Test()
    {
        using TemporaryDirectory directory = new();

        ToolResult<PredicateSchemaFile> result = PredicateSchemaFile.Load(directory.File("none.yaml"));

        Assert.Equal(ToolDiagnosticCodes.InputUnreadable, Assert.Single(result.Diagnostics).Code);
    }
}
