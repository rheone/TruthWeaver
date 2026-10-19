namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>What the export dialog starts with.</summary>
/// <param name="Entries">Every entry, grouped by <see cref="ExportEntry.Group"/> in order.</param>
/// <param name="SuggestedDirectory">The folder for the suggested file names, normally that of the open rule.</param>
/// <param name="AllowLargerTruthTable">The starting state of the larger-table option.</param>
/// <param name="LargerTruthTableText">The label of the larger-table option.</param>
/// <param name="TruthTableTermLimit">The starting term limit of a truth table, or <see langword="null"/> for none.</param>
internal sealed record ExportDialogModel(
    IReadOnlyList<ExportEntry> Entries,
    string SuggestedDirectory,
    bool AllowLargerTruthTable,
    string LargerTruthTableText,
    int? TruthTableTermLimit = null
);
