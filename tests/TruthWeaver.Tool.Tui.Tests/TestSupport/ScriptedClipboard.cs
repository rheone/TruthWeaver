namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using System.Diagnostics.CodeAnalysis;

/// <summary>A clipboard held in memory.</summary>
internal sealed class ScriptedClipboard : IClipboardAccess
{
    /// <summary>Gets or sets the text on the clipboard.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets a value indicating whether the clipboard works.</summary>
    public bool Available { get; set; } = true;

    /// <inheritdoc />
    public bool TrySet(string text)
    {
        if (this.Available)
        {
            this.Text = text;
        }

        return this.Available;
    }

    /// <inheritdoc />
    public bool TryGet([NotNullWhen(true)] out string? text)
    {
        text = this.Available ? this.Text : null;
        return text is not null;
    }
}
