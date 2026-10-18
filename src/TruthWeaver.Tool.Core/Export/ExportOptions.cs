namespace TruthWeaver.Tool.Core.Export;

using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Reports;

/// <summary>The options of <see cref="ArtifactExporter"/> and <see cref="RuleReport"/>.</summary>
public sealed record ExportOptions
{
    /// <summary>Gets the default options: the default settings, the truth-table cap kept, and no images.</summary>
    public static ExportOptions Default { get; } = new();

    /// <summary>
    /// Gets the settings. The compile limits and the analysis limit apply to every artifact, and the schema file applies to
    /// the diagnostics artifacts. The printer options of an artifact are fixed by its <see cref="ArtifactDefinition"/>.
    /// The default is <see cref="ToolSettings.Default"/>.
    /// </summary>
    public ToolSettings Settings { get; init; } = ToolSettings.Default;

    /// <summary>
    /// Gets a value indicating whether a truth table with more terms than <see cref="TruthTableOptions.DefaultTermCap"/> is
    /// printed anyway, up to <see cref="TruthTableOptions.HardTermLimit"/>, with a <c>TRE0034</c> warning. The default is
    /// <see langword="false"/>.
    /// </summary>
    public bool AllowLargerTruthTable { get; init; }

    /// <summary>
    /// Gets the most distinct terms that a truth table can have, or <see langword="null"/> for
    /// <see cref="TruthTableOptions.TermLimit"/>'s default of <see cref="TruthTableOptions.HardTermLimit"/>. A number sets
    /// <see cref="TruthTableOptions.TermLimit"/> and also allows a table above <see cref="TruthTableOptions.DefaultTermCap"/>,
    /// as if <see cref="AllowLargerTruthTable"/> were set, so <c>TruthTableTermLimit = 14</c> is enough to print a rule of
    /// 14 terms. There is no ceiling: the caller owns the 3<sup>n</sup> cost. A number below
    /// <see cref="TruthTableOptions.DefaultTermCap"/> lowers the cap too. The default is <see langword="null"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int? TruthTableTermLimit
    {
        get;
        init
        {
            if (value is { } limit)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets the image options for <see cref="ArtifactExporter.ExportAll"/>: when set, the export also renders the Mermaid
    /// and DOT diagrams as images with the external renderers. The default is <see langword="null"/>, no images.
    /// </summary>
    public ImageRenderOptions? Images { get; init; }

    /// <summary>
    /// Gets the most terms a truth table has in this export: the default cap unless a larger table is allowed, otherwise
    /// the term limit.
    /// </summary>
    internal int EffectiveTruthTableTermLimit
    {
        get
        {
            TruthTableOptions table = this.ToTruthTableOptions(TruthTableFormat.Markdown);
            return table.AllowLargerTable ? table.TermLimit : Math.Min(TruthTableOptions.DefaultTermCap, table.TermLimit);
        }
    }

    /// <summary>Gets the <see cref="TruthTableOptions"/> for a truth table in <paramref name="format"/>.</summary>
    /// <param name="format">The table format.</param>
    /// <returns>The table options that carry <see cref="AllowLargerTruthTable"/> and <see cref="TruthTableTermLimit"/>.</returns>
    internal TruthTableOptions ToTruthTableOptions(TruthTableFormat format)
    {
        return new TruthTableOptions
        {
            Format = format,
            AllowLargerTable = this.AllowLargerTruthTable || this.TruthTableTermLimit is not null,
            TermLimit = this.TruthTableTermLimit ?? TruthTableOptions.HardTermLimit,
        };
    }
}
