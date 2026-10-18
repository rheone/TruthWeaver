namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// The canonical view: the rule as the canonical rule text of its tree. When the editor text has an error, the pane keeps
/// showing the last tree that compiled and says so on its first line.
/// </summary>
internal sealed class CanonicalTextPane : IWorkbenchPane
{
    /// <summary>The first line of the pane while the editor text has errors.</summary>
    public const string StaleNotice = "Last valid tree (the rule text has errors):";

    /// <summary>The text of the pane while no text of the document compiled.</summary>
    public const string NoTreeNotice = "No valid tree yet: the rule text has errors.";

    /// <inheritdoc />
    public string Title => "Canonical";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Side;

    /// <summary>Gets the text the pane shows for the state of a workbench.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The text.</returns>
    public static string TextOf(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        if (workbench.Document.Rule is not { } rule)
        {
            return NoTreeNotice;
        }

        return workbench.Document.IsRuleCurrent ? rule.CanonicalText : StaleNotice + "\n\n" + rule.CanonicalText;
    }

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // A label is read-only and needs no cursor; the pane can still take focus, so the keys that switch and maximise a
        // pane work here too.
        Label view = new()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            Text = TextOf(workbench),
        };
        view.TextFormatter.WordWrap = true;
        workbench.DocumentChanged += (_, _) => view.Text = TextOf(workbench);
        return view;
    }
}
