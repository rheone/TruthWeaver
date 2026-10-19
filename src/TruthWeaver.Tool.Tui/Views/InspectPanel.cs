namespace TruthWeaver.Tool.Tui.Views;

using System.Drawing;
using System.Text;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// The box that shows the description of one part of the rule (see <c>InspectController</c>). The inspect key and the mouse
/// hover show the same box with the same text; the key puts it above the status line, and the hover puts it at the pointer.
/// The text is wrapped here, so the box knows its own height before the window lays it out.
/// </summary>
internal sealed class InspectPanel : FrameView
{
    /// <summary>The widest text, in columns, so a long description wraps and stays easy to read.</summary>
    private const int MaxTextWidth = 70;

    private readonly Label body;

    /// <summary>Initializes a new instance of the <see cref="InspectPanel"/> class. It starts hidden.</summary>
    public InspectPanel()
    {
        this.Title = "Inspect (Esc closes)";
        this.Visible = false;
        this.CanFocus = false;
        this.body = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.Add(this.body);
    }

    /// <summary>Gets the text that the box shows. It is empty while the box is hidden.</summary>
    public new string Text => this.Visible ? this.body.Text : string.Empty;

    /// <summary>Wraps text to a width. A line feed ends a line; a word longer than the width is cut.</summary>
    /// <param name="text">The text.</param>
    /// <param name="width">The width in columns.</param>
    /// <returns>The lines.</returns>
    public static IReadOnlyList<string> Wrap(string text, int width)
    {
        ArgumentNullException.ThrowIfNull(text);

        width = Math.Max(width, 8);
        List<string> lines = [];
        foreach (string paragraph in text.Split('\n'))
        {
            StringBuilder line = new();
            foreach (string word in paragraph.Split(' '))
            {
                string rest = word;
                while (line.Length + (line.Length == 0 ? 0 : 1) + rest.Length > width)
                {
                    if (line.Length > 0)
                    {
                        lines.Add(line.ToString());
                        _ = line.Clear();
                    }
                    else
                    {
                        lines.Add(rest[..width]);
                        rest = rest[width..];
                    }
                }

                _ = line.Append(line.Length == 0 ? string.Empty : " ").Append(rest);
            }

            lines.Add(line.ToString());
        }

        return lines;
    }

    /// <summary>Shows the box with a text.</summary>
    /// <param name="text">The text.</param>
    /// <param name="window">The size of the window the box sits in.</param>
    /// <param name="bottomRows">The rows at the bottom of the window that the box keeps free (the status and hint lines).</param>
    /// <param name="topRows">The rows at the top of the window that the box keeps free (the menu bar).</param>
    /// <param name="pointer">The pointer in the window, or <see langword="null"/> for the box above the status line.</param>
    public void ShowText(string text, Size window, int bottomRows, int topRows, Point? pointer)
    {
        ArgumentNullException.ThrowIfNull(text);

        IReadOnlyList<string> lines = Wrap(text, Math.Min(MaxTextWidth, Math.Max(window.Width - 4, 8)));
        int width = Math.Min(lines.Max(l => l.Length) + 2, Math.Max(window.Width, 10));
        int height = Math.Min(lines.Count + 2, Math.Max(window.Height - topRows - bottomRows, 3));
        this.body.Text = string.Join('\n', lines.Take(height - 2));
        this.Width = width;
        this.Height = height;
        if (pointer is { } at)
        {
            // Below the pointer, or above it when there is no room below.
            this.X = Math.Clamp(at.X, 0, Math.Max(window.Width - width, 0));
            int below = at.Y + 1;
            this.Y = below + height <= window.Height - bottomRows ? below : Math.Max(at.Y - height, topRows);
        }
        else
        {
            this.X = Math.Max(window.Width - width, 0);
            this.Y = Math.Max(window.Height - bottomRows - height, topRows);
        }

        this.Visible = true;
        this.SetNeedsDraw();
    }

    /// <summary>Hides the box.</summary>
    public void HideText()
    {
        this.Visible = false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.body.Dispose();
        }
    }
}
