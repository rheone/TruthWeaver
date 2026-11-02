namespace TruthWeaver.Tool.Core.Palette;

using TruthWeaver.Tool.Core.Loading;

/// <summary>
/// Reads the predicate schema file of a palette once and keeps it. The file is read again only when the path changes or the
/// file changes on disk, so a palette that is built for every search does not read the file for every search.
/// </summary>
public sealed class PaletteSchemaCache
{
    private string? path;
    private DateTime stamp;
    private PredicateSchemaFile? schema;

    /// <summary>Gets the schema file at a path.</summary>
    /// <param name="schemaPath">The path, or <see langword="null"/> when no schema file is set.</param>
    /// <returns>
    /// The schema file, or <see langword="null"/> when there is none or it cannot be read. The validation panel reports
    /// the problems of a file that cannot be read.
    /// </returns>
    public PredicateSchemaFile? Get(string? schemaPath)
    {
        if (schemaPath is null)
        {
            this.path = null;
            this.schema = null;
            return null;
        }

        DateTime written = File.Exists(schemaPath) ? File.GetLastWriteTimeUtc(schemaPath) : DateTime.MinValue;
        if (this.path != schemaPath || this.stamp != written)
        {
            this.path = schemaPath;
            this.stamp = written;
            this.schema = PredicateSchemaFile.Load(schemaPath).Value;
        }

        return this.schema;
    }
}
