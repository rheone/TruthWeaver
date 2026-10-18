namespace TruthWeaver.Tool.Tui;

using System.Diagnostics.CodeAnalysis;

/// <summary>Reads and writes the system clipboard. The terminal implementation uses the Terminal.Gui clipboard.</summary>
internal interface IClipboardAccess
{
    /// <summary>Puts text on the clipboard.</summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when the clipboard took the text; <see langword="false"/> when it is not available.</returns>
    public bool TrySet(string text);

    /// <summary>Reads the text on the clipboard.</summary>
    /// <param name="text">The text when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the clipboard gave text.</returns>
    public bool TryGet([NotNullWhen(true)] out string? text);
}
