namespace TruthWeaver.Tool.Core.Export;

using TruthWeaver.Tool.Core.Configuration;

/// <summary>
/// One entry of the <see cref="ArtifactCatalog"/>: an output that the tools can make from a rule, such as the rule as
/// JSON, the equation in LaTeX or the truth table as CSV. Each artifact has fixed printer options, so the same rule gives
/// the same text every time. <see cref="ArtifactExporter"/> makes the text.
/// </summary>
public sealed class ArtifactDefinition
{
    /// <summary>Initializes a new instance of the <see cref="ArtifactDefinition"/> class.</summary>
    /// <param name="id">The artifact id.</param>
    /// <param name="group">The kind of output the artifact is.</param>
    /// <param name="extension">The file name extension, with the dot.</param>
    /// <param name="format">The name of the output format.</param>
    /// <param name="description">A one-line description.</param>
    /// <param name="options">The printer options, as names and values.</param>
    /// <param name="render">Makes the text from a compiled source.</param>
    internal ArtifactDefinition(
        string id,
        ArtifactGroup group,
        string extension,
        string format,
        string description,
        IEnumerable<KeyValuePair<string, string>> options,
        Func<ArtifactSource, ToolResult<string>> render
    )
    {
        this.Id = id;
        this.Group = group;
        this.FileName = id + extension;
        this.Format = format;
        this.Description = description;
        this.Options = [.. options.OrderBy(option => option.Key, StringComparer.Ordinal)];
        this.Render = render;
    }

    /// <summary>Gets the artifact id, such as <c>equation-latex-letters</c>. Ids are lower case and stable.</summary>
    public string Id { get; }

    /// <summary>Gets the kind of output the artifact is. A front end lists the artifacts under it.</summary>
    public ArtifactGroup Group { get; }

    /// <summary>Gets the file name that an export of every artifact uses: the id and an extension, such as <c>json.json</c>.</summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the name of the output format, such as <c>rule-text</c>, <c>json</c>, <c>latex</c>, <c>mermaid</c> or
    /// <c>markdown</c>. Plain text with no other format is <c>text</c>.
    /// </summary>
    public string Format { get; }

    /// <summary>Gets a one-line description of the artifact.</summary>
    public string Description { get; }

    /// <summary>
    /// Gets the printer options of the artifact, as names and values in ordinal order of the name, such as
    /// <c>dialect</c> = <c>latex</c>. The manifest of an export of every artifact records them. An artifact with the
    /// default options of its printer has none.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Options { get; }

    /// <summary>Gets the function that makes the text of the artifact.</summary>
    internal Func<ArtifactSource, ToolResult<string>> Render { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Id;
    }
}
