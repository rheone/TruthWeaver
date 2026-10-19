namespace TruthWeaver.Tool.Tui.Panes;

using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The diagnostics of the editor text, one per line with severity, code, line and column. Enter on a diagnostic puts the
/// editor cursor at its span.
/// </summary>
internal sealed class DiagnosticsPane : IWorkbenchPane
{
    /// <summary>The line the pane shows when the text has no diagnostics.</summary>
    public const string NoDiagnostics = "No diagnostics.";

    /// <summary>The <see cref="View.Id"/> of the list, so the context menu finds the pane.</summary>
    public const string ViewId = "diagnostics";

    /// <inheritdoc />
    public string Title => "Diagnostics";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Bottom;

    /// <summary>Gets the lines the pane shows for a document: one per diagnostic, most severe first.</summary>
    /// <param name="text">The text the spans point into.</param>
    /// <param name="diagnostics">The diagnostics.</param>
    /// <returns>The lines.</returns>
    public static IReadOnlyList<string> LinesOf(string text, IReadOnlyList<Diagnostic> diagnostics)
    {
        // An empty rule is not an error to show: the compiler reports TRE0001, but the author only needs to know how to start.
        if (string.IsNullOrWhiteSpace(text))
        {
            return EmptyRuleHint();
        }

        if (diagnostics.Count == 0)
        {
            return [NoDiagnostics];
        }

        return [.. Order(diagnostics).Select(d => Describe(text, d))];
    }

    /// <summary>Gets the lines the pane shows for an empty or whitespace-only rule, with the real keys of the sample list and the menus.</summary>
    /// <returns>The lines.</returns>
    public static IReadOnlyList<string> EmptyRuleHint()
    {
        return
        [
            "The rule is empty. Type a rule in the editor,",
            $"or press {SampleCommands.SamplesKey} to choose a sample rule, or {GenerateCommands.GenerateKey} to generate one.",
            $"{WorkbenchMenus.MenuBarKey} opens the menus.",
        ];
    }

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        ObservableCollection<string> lines = [];
        ListView list = new()
        {
            Id = ViewId,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        list.SetSource(lines);
        List<Diagnostic> shown = [];

        void Show()
        {
            shown.Clear();

            // The hint is not a diagnostic: Enter on it has no span to show.
            if (!string.IsNullOrWhiteSpace(workbench.Document.Text))
            {
                shown.AddRange(Order(workbench.Document.Diagnostics));
            }

            lines.Clear();
            foreach (string line in LinesOf(workbench.Document.Text, workbench.Document.Diagnostics))
            {
                lines.Add(line);
            }
        }

        list.KeyDown += (_, key) =>
        {
            if (key == Key.Enter && list.Value is int index && index < shown.Count && shown[index].Span != SourceSpan.None)
            {
                workbench.RequestSpan(shown[index].Span);
                key.Handled = true;
            }
        };

        workbench.DocumentChanged += (_, _) => Show();
        Show();
        return list;
    }

    private static IEnumerable<Diagnostic> Order(IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics.OrderByDescending(d => d.Severity).ThenBy(d => d.Span.Start);
    }

    private static string Describe(string text, Diagnostic diagnostic)
    {
        string severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info",
        };
        string location = string.Empty;
        if (diagnostic.Span != SourceSpan.None)
        {
            SourceLocation at = diagnostic.Span.GetLocation(text);
            location = $" {at.Line}:{at.Column}";
        }

        return $"{severity} {diagnostic.Code}{location}: {diagnostic.Message}";
    }
}
