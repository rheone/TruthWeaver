namespace TruthWeaver.Tool.Core.Tests.Palette;

using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Palette;

/// <summary>The cache of the predicate schema file of a palette: it reads the file once and again when the file changes.</summary>
public sealed class PaletteSchemaCacheTests : IDisposable
{
    private const string Schema = """
        predicates:
          hasRole:
            description: The user has the role.
            arguments:
              role: string
        """;

    private readonly string path = Path.Combine(Path.GetTempPath(), $"palette-{Guid.NewGuid():N}.yaml");

    /// <summary>Deletes the schema file that a test wrote.</summary>
    public void Dispose()
    {
        File.Delete(this.path);
    }

    /// <summary>A second request for an unchanged file gives the file that the first request read.</summary>
    [Fact]
    public void Get_UnchangedFile_ReturnsTheSameSchemaFile_Test()
    {
        File.WriteAllText(this.path, Schema);
        PaletteSchemaCache cache = new();

        PredicateSchemaFile? first = cache.Get(this.path);

        Assert.NotNull(first);
        Assert.Same(first, cache.Get(this.path));
    }

    /// <summary>A file that changed on disk is read again.</summary>
    [Fact]
    public void Get_FileChangedOnDisk_ReadsTheFileAgain_Test()
    {
        File.WriteAllText(this.path, Schema);
        PaletteSchemaCache cache = new();
        _ = cache.Get(this.path);

        File.WriteAllText(this.path, Schema.Replace("hasRole", "hasGroup", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(this.path, DateTime.UtcNow.AddMinutes(1));

        Assert.Equal("hasGroup", Assert.Single(cache.Get(this.path)!.Predicates).Name);
    }

    /// <summary>With no path there is no schema file.</summary>
    [Fact]
    public void Get_NoPath_ReturnsNull_Test()
    {
        Assert.Null(new PaletteSchemaCache().Get(null));
    }

    /// <summary>A file that does not exist gives no schema file and no exception.</summary>
    [Fact]
    public void Get_MissingFile_ReturnsNull_Test()
    {
        Assert.Null(new PaletteSchemaCache().Get(this.path));
    }
}
