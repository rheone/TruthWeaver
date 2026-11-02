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

    /// <summary>An empty search finds every entry but the operators: the own entries first, then the catalog by family.</summary>
    [Fact]
    public void Search_EmptyText_FindsOwnEntriesThenTheCatalogByFamily_Test()
    {
        RulePalette palette = RulePalette.Create(Compile("inRegion(region: \"EU\")"), SchemaFile());

        IReadOnlyList<PaletteGroup> groups = palette.Search(string.Empty);

        Assert.Null(groups[0].Title);
        Assert.DoesNotContain(groups.SelectMany(g => g.Entries), e => e.Source == PaletteSource.Operator);
        Assert.Contains(groups[0].Entries, e => e.Source == PaletteSource.SchemaFile);
        Assert.Equal(PredicateCatalog.All.Count, groups.Skip(1).Sum(g => g.Entries.Count));
        Assert.All(groups.Skip(1), g => Assert.NotNull(g.Title));
    }

    /// <summary>Own entries match when each word is in the line or the description; the case does not matter.</summary>
    [Fact]
    public void Search_WordsOfAnOwnEntry_FindsTheEntry_Test()
    {
        RulePalette palette = RulePalette.Create(Compile("inRegion(region: \"EU\")"), SchemaFile());

        PaletteGroup own = Assert.Single(palette.Search("HASROLE role", e => e.Source == PaletteSource.SchemaFile));

        Assert.Equal("hasRole", Assert.Single(own.Entries).Name);
        Assert.Empty(palette.Search("zzzqqq"));
    }

    /// <summary>A catalog predicate is found by an alias and is listed under its family.</summary>
    [Fact]
    public void Search_AliasOfACatalogPredicate_FindsItUnderItsFamily_Test()
    {
        RulePalette palette = RulePalette.Create(null, null);

        IReadOnlyList<PaletteGroup> groups = palette.Search("begins with");

        PaletteGroup group = Assert.Single(groups, g => g.Entries.Any(e => e.Name == "StartsWith"));
        Assert.Equal("String", group.Title);
    }

    /// <summary>The filter limits what a search can find.</summary>
    [Fact]
    public void Search_WithAFilter_FindsOnlyThoseEntries_Test()
    {
        RulePalette palette = RulePalette.Create(Compile("inRegion(region: \"EU\")"), SchemaFile());

        IReadOnlyList<PaletteGroup> groups = palette.Search(string.Empty, e => e.Source == PaletteSource.Catalog);

        Assert.All(groups, g => Assert.NotNull(g.Title));
        Assert.All(groups.SelectMany(g => g.Entries), e => Assert.Equal(PaletteSource.Catalog, e.Source));
    }

    /// <summary>The facts of a predicate of the rule come from the rule; the catalog adds the family.</summary>
    [Fact]
    public void Find_PredicateOfTheRule_GivesTheRuleTemplate_Test()
    {
        RulePalette palette = RulePalette.Create(Compile("inRegion(region: \"EU\")"), null);

        PredicateFacts? facts = palette.Find("inRegion");

        Assert.NotNull(facts);
        Assert.Equal(PaletteSource.Document, facts.Entry.Source);
        Assert.Null(facts.Family);
        Assert.False(facts.TakesNullBehavior);
    }

    /// <summary>A catalog predicate has a family and, when its factory takes a null behavior, says so.</summary>
    [Fact]
    public void Find_CatalogPredicate_GivesFamilyAndNullBehavior_Test()
    {
        RulePalette palette = RulePalette.Create(null, null);
        CatalogPredicate expected = PredicateCatalog.All.First(p => p.TakesNullBehavior);

        PredicateFacts? facts = palette.Find(expected.Entry.Name);

        Assert.NotNull(facts);
        Assert.Equal(expected.Family, facts.Family);
        Assert.True(facts.TakesNullBehavior);
        Assert.Null(palette.Find("noSuchPredicate"));
    }

    /// <summary>A schema file is read once and read again when the file changes.</summary>
    [Fact]
    public void Get_SchemaFile_IsCachedUntilTheFileChanges_Test()
    {
        string path = Path.Combine(Path.GetTempPath(), $"palette-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, Schema);
            PaletteSchemaCache cache = new();

            PredicateSchemaFile? first = cache.Get(path);

            Assert.Same(first, cache.Get(path));
            Assert.NotNull(first);
            File.WriteAllText(path, Schema.Replace("hasRole", "hasGroup", StringComparison.Ordinal));
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
            Assert.Equal("hasGroup", Assert.Single(cache.Get(path)!.Predicates).Name);
            Assert.Null(cache.Get(null));
        }
        finally
        {
            File.Delete(path);
        }
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
