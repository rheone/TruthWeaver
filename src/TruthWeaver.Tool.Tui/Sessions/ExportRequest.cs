namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>Where the export dialog sends the output.</summary>
internal enum ExportAction
{
    /// <summary>To the clipboard.</summary>
    Copy,

    /// <summary>To the file (or, for the export of every artifact, the folder) that the dialog names.</summary>
    ToFile,
}

/// <summary>What the author asked for in the export dialog, with the dialog's settings.</summary>
/// <param name="Entry">The chosen entry, or <see langword="null"/> for every artifact to a folder.</param>
/// <param name="Action">Where the output goes.</param>
/// <param name="Path">The file or folder path for <see cref="ExportAction.ToFile"/>; empty for <see cref="ExportAction.Copy"/>.</param>
/// <param name="AllowLargerTruthTable">Whether truth tables over the term cap are made.</param>
internal sealed record ExportRequest(ExportEntry? Entry, ExportAction Action, string Path, bool AllowLargerTruthTable);
