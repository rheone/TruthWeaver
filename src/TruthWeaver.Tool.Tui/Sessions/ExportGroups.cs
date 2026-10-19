namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>The headings under which the export dialog lists its entries.</summary>
internal static class ExportGroups
{
    /// <summary>The rule as rule text, JSON or YAML.</summary>
    public const string Rule = "Rule";

    /// <summary>The equation in a dialect, an envelope and a constant style.</summary>
    public const string Equation = "Equation";

    /// <summary>The text tree and the diagrams.</summary>
    public const string Diagram = "Tree and diagram";

    /// <summary>The truth table in every format.</summary>
    public const string TruthTable = "Truth table";

    /// <summary>The statistics and the diagnostics.</summary>
    public const string Analysis = "Statistics and diagnostics";

    /// <summary>The Markdown report.</summary>
    public const string Report = "Report";

    /// <summary>The diagrams as images.</summary>
    public const string Images = "Image";
}
