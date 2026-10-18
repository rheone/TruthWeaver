namespace TruthWeaver.Tool.Core.Tests.Palette;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Yaml;

/// <summary>The palette of the structural editor: its entries and the expressions it fills in.</summary>
public sealed class RulePaletteTests
{
    private const string Schema = """
        predicates:
          hasRole:
            description: The user has the role.
            arguments:
              role: string
              minimumLevel:
                kind: int64
                required: false
        """;

    /// <summary>The palette lists the operators, the constants, the predicates of the schema file and those of the rule.</summary>
    [Fact]
    public void Create_RuleAndSchemaFile_ListsEverySource_Test()
    {
        RulePalette palette = RulePalette.Create(Compile("inRegion(region: \"EU\") AND isActive"), SchemaFile());

        Assert.Contains(palette.Entries, e => e is { Source: PaletteSource.Operator, Name: "and" });
        Assert.Contains(palette.Entries, e => e is { Source: PaletteSource.Constant, Name: "Unknown" });
        PaletteEntry schema = Assert.Single(palette.Entries, e => e.Source == PaletteSource.SchemaFile);
        Assert.Equal("hasRole", schema.Name);
        Assert.Equal(
            [("role", LiteralKind.String, true), ("minimumLevel", LiteralKind.Int64, false)],
            schema.Arguments.Select(a => (a.Name, a.Kind, a.Required))
        );
        Assert.Equal(
            ["inRegion", "isActive"],
            palette.Entries.Where(e => e.Source == PaletteSource.Document).Select(e => e.Name)
        );
    }

    /// <summary>A predicate of the rule is a template whose values start at the values of its first use.</summary>
    [Fact]
    public void Create_RulePredicate_StartsTheFormAtTheValuesOfItsFirstUse_Test()
    {
        RulePalette palette = RulePalette.Create(Compile("inRegion(region: \"EU\")"), null);

        PaletteEntry entry = Assert.Single(palette.Entries, e => e.Source == PaletteSource.Document);

        PaletteArgument argument = Assert.Single(entry.Arguments);
        Assert.Equal(("region", LiteralKind.String, "EU"), (argument.Name, argument.Kind, argument.Default));
    }

    /// <summary>Filling a template gives the call as rule text; an empty optional value leaves the argument out.</summary>
    [Fact]
    public void Fill_SchemaPredicate_GivesTheCall_Test()
    {
        PaletteEntry entry = RulePalette.Create(null, SchemaFile()).Entries.Single(e => e.Name == "hasRole");

        ToolResult<string> filled = RulePalette.Fill(entry, ["admin", string.Empty]);

        Assert.True(filled.Succeeded);
        Assert.Equal(
            Compile("hasRole(role: \"admin\")").PrintJson(includeNotes: false),
            Compile(filled.Value).PrintJson(includeNotes: false)
        );
    }

    /// <summary>A number argument takes its value as a number.</summary>
    [Fact]
    public void Fill_NumberArgument_WritesANumber_Test()
    {
        PaletteEntry entry = RulePalette.Create(null, SchemaFile()).Entries.Single(e => e.Name == "hasRole");

        ToolResult<string> filled = RulePalette.Fill(entry, ["admin", "3"]);

        Assert.True(filled.Succeeded);
        Assert.Equal(
            Compile("hasRole(role: \"admin\", minimumLevel: 3)").PrintJson(includeNotes: false),
            Compile(filled.Value).PrintJson(includeNotes: false)
        );
    }

    /// <summary>A constant fills in as itself.</summary>
    [Fact]
    public void Fill_Constant_GivesTheConstant_Test()
    {
        PaletteEntry entry = RulePalette.Create(null, null).Entries.Single(e => e.Name == "False");

        ToolResult<string> filled = RulePalette.Fill(entry, []);

        Assert.Equal("False", filled.Value);
    }

    private static PredicateSchemaFile SchemaFile()
    {
        ToolResult<PredicateSchemaFile> schema = PredicateSchemaFile.Parse(Schema);
        Assert.True(schema.Succeeded);
        return schema.Value;
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        CompilationResult<StubAssignment> result = RuleConverter.CreateCompiler().CompileAs(text, RuleFormat.Text);
        Assert.NotNull(result.Rule);
        return result.Rule;
    }
}
