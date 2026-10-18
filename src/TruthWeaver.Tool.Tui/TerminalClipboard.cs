namespace TruthWeaver.Tool.Tui;

using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.App;

/// <summary>The clipboard of the running application.</summary>
/// <param name="app">The running application.</param>
internal sealed class TerminalClipboard(IApplication app) : IClipboardAccess
{
    /// <inheritdoc />
    public bool TrySet(string text)
    {
        return app.Clipboard is { IsSupported: true } clipboard && clipboard.TrySetClipboardData(text);
    }

    /// <inheritdoc />
    public bool TryGet([NotNullWhen(true)] out string? text)
    {
        text = null;
        return app.Clipboard is { IsSupported: true } clipboard && clipboard.TryGetClipboardData(out text) && text is not null;
    }
}
