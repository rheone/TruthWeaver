namespace TruthWeaver.Tool;

using System.CommandLine;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Export;

/// <summary>
/// The <c>--artifact</c> and <c>--allow-large-truth-table</c> options of <c>print</c> and <c>export</c>. An artifact is
/// an id of the artifact catalog, or <c>equation</c> for the equation in the dialect and constant style of the settings.
/// </summary>
internal sealed class ArtifactOptions
{
    /// <summary>The id that picks the equation of the settings' dialect and constant style.</summary>
    public const string EquationAlias = "equation";

    /// <summary>Initializes a new instance of the <see cref="ArtifactOptions"/> class.</summary>
    /// <param name="defaultArtifact">The artifact when the option is not given, or <see langword="null"/> for none.</param>
    public ArtifactOptions(string? defaultArtifact)
    {
        this.Artifact = new Option<string?>("--artifact", "-a")
        {
            Description =
                "The artifact id, such as rule-text, json, mermaid, truth-table-markdown or report. 'equation' is the equation in the "
                + "--dialect and --constant-style of the settings.",
            HelpName = "id",
        };
        if (defaultArtifact is not null)
        {
            this.Artifact.DefaultValueFactory = _ => defaultArtifact;
        }

        this.Artifact.CompletionSources.Add(_ => [EquationAlias, .. ArtifactCatalog.All.Select(a => a.Id)]);
    }

    /// <summary>Gets the artifact option.</summary>
    public Option<string?> Artifact { get; }

    /// <summary>Gets the option that prints a truth table over the cap.</summary>
    public Option<bool> AllowLargerTruthTable { get; } =
        new("--allow-large-truth-table")
        {
            Description = "Print a truth table with more than 8 terms, up to 12, with a TRE0034 warning.",
        };

    /// <summary>Gets the catalog id of an artifact, with <c>equation</c> resolved through the settings.</summary>
    /// <param name="id">The id as given.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The catalog id. An id that is not in the catalog stays as it is, so the exporter reports it.</returns>
    public static string Resolve(string id, ToolSettings settings)
    {
        return string.Equals(id, EquationAlias, StringComparison.Ordinal)
            ? ArtifactCatalog.Equation(settings.Dialect, settings.ConstantStyle ?? ConstantStyle.Letters).Id
            : id;
    }

    /// <summary>Adds the options to a command.</summary>
    /// <param name="command">The command.</param>
    public void AddTo(Command command)
    {
        command.Add(this.Artifact);
        command.Add(this.AllowLargerTruthTable);
    }

    /// <summary>Gets the export options of a command.</summary>
    /// <param name="parse">The parse result.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The options.</returns>
    public ExportOptions ExportOptions(ParseResult parse, ToolSettings settings)
    {
        return new ExportOptions { Settings = settings, AllowLargerTruthTable = parse.GetValue(this.AllowLargerTruthTable) };
    }
}
