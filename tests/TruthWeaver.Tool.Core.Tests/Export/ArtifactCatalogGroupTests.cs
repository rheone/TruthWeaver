namespace TruthWeaver.Tool.Core.Tests.Export;

using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>The catalog states the group of every artifact, and Core says which image renderers are installed.</summary>
public sealed class ArtifactCatalogGroupTests
{
    /// <summary>Every catalog entry has a defined group, so a new artifact cannot be added without choosing one.</summary>
    [Fact]
    public void All_EveryArtifact_HasADefinedGroup_Test()
    {
        foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
        {
            Assert.True(Enum.IsDefined(artifact.Group), $"{artifact.Id} has no group.");
        }
    }

    /// <summary>The groups follow the kind of output: equations, truth tables, diagrams and the rest.</summary>
    /// <param name="id">The artifact id.</param>
    /// <param name="expected">The expected group.</param>
    [Theory]
    [InlineData("rule-text", ArtifactGroup.Rule)]
    [InlineData("yaml", ArtifactGroup.Rule)]
    [InlineData("equation-latex-letters", ArtifactGroup.Equation)]
    [InlineData("truth-table-csv", ArtifactGroup.TruthTable)]
    [InlineData("tree", ArtifactGroup.Diagram)]
    [InlineData("dot", ArtifactGroup.Diagram)]
    [InlineData("statistics", ArtifactGroup.Analysis)]
    [InlineData("diagnostics-sarif", ArtifactGroup.Analysis)]
    [InlineData("report", ArtifactGroup.Report)]
    public void Group_OfAnArtifact_IsTheKindOfOutput_Test(string id, ArtifactGroup expected)
    {
        Assert.True(ArtifactCatalog.TryFind(id, out ArtifactDefinition? artifact));
        Assert.Equal(expected, artifact.Group);
    }

    /// <summary>The diagram artifact of a language is the <c>dot</c> or <c>mermaid</c> artifact.</summary>
    [Fact]
    public void Diagram_ForALanguage_IsTheMatchingArtifact_Test()
    {
        Assert.Equal("dot", ArtifactCatalog.Diagram(DiagramLanguage.Dot).Id);
        Assert.Equal("mermaid", ArtifactCatalog.Diagram(DiagramLanguage.Mermaid).Id);
    }

    /// <summary>With no renderer on the search path, both languages are missing and none is available.</summary>
    [Fact]
    public void MissingLanguages_NoRendererOnTheSearchPath_ListsBothLanguages_Test()
    {
        using TemporaryDirectory empty = new();

        Assert.Equal([DiagramLanguage.Mermaid, DiagramLanguage.Dot], ImageRenderer.MissingLanguages(empty.FullPath));
        Assert.Empty(ImageRenderer.AvailableLanguages(empty.FullPath));
    }
}
