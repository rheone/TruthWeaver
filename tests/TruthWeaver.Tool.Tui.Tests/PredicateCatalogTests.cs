namespace TruthWeaver.Tool.Tui.Tests;

using System.Reflection;
using TruthWeaver.Abstractions;
using TruthWeaver.Predicates;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>The catalog of ready-made predicates that the palette offers, and the search over it.</summary>
public sealed class PredicateCatalogTests
{
    /// <summary>Every public factory of the predicates package is in the catalog, so a new factory needs no change in the terminal UI.</summary>
    [Fact]
    public void All_EveryPublicFactory_AppearsInTheCatalog_Test()
    {
        string[] factories =
        [
            .. typeof(NullBehavior)
                .Assembly.GetExportedTypes()
                .Where(t => t is { IsAbstract: true, IsSealed: true })
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(m =>
                    m.IsGenericMethodDefinition
                    && m.GetParameters() is [{ Name: "name" }, { Name: "selector" }, ..]
                    && m.ReturnType.IsGenericType
                    && m.ReturnType.GetGenericArguments()[0] == typeof(PredicateSchema)
                )
                .Select(m => m.Name)
                .Distinct()
                .Order(StringComparer.Ordinal),
        ];

        string[] cataloged = [.. PredicateCatalog.All.Select(p => p.Entry.Name).Distinct().Order(StringComparer.Ordinal)];

        Assert.NotEmpty(factories);
        Assert.Equal(factories, cataloged);
        Assert.All(PredicateCatalog.All, p => Assert.Equal(PaletteSource.Catalog, p.Entry.Source));
    }

    /// <summary>The entries sit in family groups: one run of entries for each family, with a family title.</summary>
    [Fact]
    public void All_Entries_SitInFamilyGroups_Test()
    {
        string[] families = [.. PredicateCatalog.All.Select(p => p.Family)];

        string[] runs = [.. families.Where((f, i) => i == 0 || families[i - 1] != f)];

        Assert.Equal(runs.Length, runs.Distinct().Count());
        Assert.Contains("String", runs);
        Assert.Contains("Numeric", runs);
        Assert.Contains("Date and time", runs);
        Assert.Contains("Collection", runs);
    }

    /// <summary>An entry keeps the arguments and kinds of the factory schema.</summary>
    [Fact]
    public void All_StartsWith_HasOneRequiredStringArgument_Test()
    {
        CatalogPredicate startsWith = Assert.Single(
            PredicateCatalog.All,
            p => p is { Family: "String", Entry.Name: "StartsWith" }
        );

        PaletteArgument argument = Assert.Single(startsWith.Entry.Arguments);
        Assert.Equal(("value", LiteralKind.String, true), (argument.Name, argument.Kind, argument.Required));
        Assert.Equal("Starts With", startsWith.Label);
    }

    /// <summary>Every alias names predicates that the catalog has, so a renamed predicate cannot leave a dead alias.</summary>
    [Fact]
    public void Aliases_EveryName_NamesARealCatalogPredicate_Test()
    {
        HashSet<string> names = [.. PredicateCatalog.All.Select(p => p.Entry.Name)];

        Assert.NotEmpty(PredicateCatalog.Aliases);
        foreach ((string phrase, IReadOnlyList<string> aliased) in PredicateCatalog.Aliases)
        {
            Assert.NotEmpty(aliased);
            Assert.All(
                aliased,
                name => Assert.True(names.Contains(name), $"The alias '{phrase}' names '{name}', which is not in the catalog.")
            );
        }
    }

    /// <summary>A search finds a predicate by its name, with no regard to case.</summary>
    [Theory]
    [InlineData("startswith")]
    [InlineData("STARTSWITH")]
    public void Search_ByName_FindsThePredicate_Test(string query)
    {
        Assert.Contains(PredicateCatalog.Search(query), p => p.Entry.Name == "StartsWith");
    }

    /// <summary>A search finds a predicate by its label.</summary>
    [Fact]
    public void Search_ByLabel_FindsThePredicate_Test()
    {
        Assert.Contains(PredicateCatalog.Search("Ends With"), p => p.Entry.Name == "EndsWith");
        Assert.Contains(PredicateCatalog.Search("ends with"), p => p.Entry.Name == "EndsWith");
    }

    /// <summary>A search finds a predicate by a word of its description.</summary>
    [Fact]
    public void Search_ByDescriptionWord_FindsThePredicate_Test()
    {
        CatalogPredicate predicate = PredicateCatalog.All[0];
        string word = predicate
            .Entry.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .First(w => w.All(char.IsLetter) && w.Length > 4);

        IReadOnlyList<CatalogPredicate> found = PredicateCatalog.Search(word.ToUpperInvariant());

        Assert.Contains(predicate, found);
    }

    /// <summary>A search finds the predicates of a family by the family name.</summary>
    [Fact]
    public void Search_ByFamilyName_FindsTheWholeFamily_Test()
    {
        IReadOnlyList<CatalogPredicate> found = PredicateCatalog.Search("date and TIME");

        Assert.All(PredicateCatalog.All.Where(p => p.Family == "Date and time"), p => Assert.Contains(p, found));
    }

    /// <summary>A search finds a predicate by a common synonym, such as "older than" for a date that is before another.</summary>
    [Theory]
    [InlineData("older than", "IsBefore")]
    [InlineData("Older Than", "IsBefore")]
    [InlineData("empty", "IsEmpty")]
    [InlineData("regex", "IsMatch")]
    public void Search_ByAlias_FindsThePredicate_Test(string query, string name)
    {
        Assert.Contains(PredicateCatalog.Search(query), p => p.Entry.Name == name);
    }

    /// <summary>A search for words that nothing has finds nothing, and an empty search finds everything.</summary>
    [Fact]
    public void Search_NoMatchAndEmptyQuery_GiveNothingAndEverything_Test()
    {
        Assert.Empty(PredicateCatalog.Search("zzzqqq"));
        Assert.Equal(PredicateCatalog.All.Count, PredicateCatalog.Search(string.Empty).Count);
    }

    /// <summary>A rule built from a catalog template compiles, whatever the family.</summary>
    [Theory]
    [InlineData("StartsWith", "abc")]
    [InlineData("HasCountEqualTo", "3")]
    [InlineData("IsGreaterThan", "10")]
    public void Fill_CatalogTemplate_GivesRuleTextThatCompiles_Test(string name, string value)
    {
        PaletteEntry entry = PredicateCatalog.All.First(p => p.Entry.Name == name).Entry;

        ToolResult<string> filled = RulePalette.Fill(entry, [value]);

        Assert.True(filled.Succeeded);
        RuleDocument document = RuleDocument.Create(filled.Value);
        Assert.NotNull(document.Rule);
        Assert.Contains(name, document.Text, StringComparison.Ordinal);
    }

    /// <summary>A two-argument template, such as a range, compiles when both values are given.</summary>
    [Fact]
    public void Fill_BetweenTemplate_GivesRuleTextThatCompiles_Test()
    {
        PaletteEntry entry = PredicateCatalog.All.First(p => p.Entry.Name == "IsBetween" && p.Family == "Numeric").Entry;

        ToolResult<string> filled = RulePalette.Fill(entry, ["1", "5"]);

        Assert.True(filled.Succeeded);
        Assert.NotNull(RuleDocument.Create(filled.Value).Rule);
    }
}
