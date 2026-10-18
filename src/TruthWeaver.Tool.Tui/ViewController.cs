namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// The logic behind the view pane, with no view of its own: the toggles, the printed output, and copy and save. The pane
/// maps keys to these methods, and a test calls them directly.
/// </summary>
internal sealed class ViewController
{
    private static readonly OperatorStyle?[] OperatorStyles =
    [
        null,
        OperatorStyle.Word,
        OperatorStyle.Symbolic,
        OperatorStyle.CStyle,
    ];

    private static readonly ConstantStyle?[] ConstantStyles =
    [
        null,
        ConstantStyle.Letters,
        ConstantStyle.Word,
        ConstantStyle.LogicSymbols,
    ];

    private readonly Workbench workbench;
    private readonly WorkbenchServices services;
    private readonly Func<StubEvaluation?> evaluation;

    /// <summary>Initializes a new instance of the <see cref="ViewController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The clipboard and the dialogs.</param>
    /// <param name="evaluation">Gives the current stub evaluation to mark in the views, or <see langword="null"/>.</param>
    public ViewController(Workbench workbench, WorkbenchServices services, Func<StubEvaluation?>? evaluation = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(services);

        this.workbench = workbench;
        this.services = services;
        this.evaluation = evaluation ?? (() => null);
    }

    /// <summary>Occurs after a toggle changes the settings.</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>Gets the toggles.</summary>
    public ViewSettings Settings { get; } = new();

    /// <summary>Gets what the view shows now.</summary>
    public ViewOutput Output => RuleViewRenderer.Render(this.workbench.Document, this.Settings, this.evaluation());

    /// <summary>Gets the line above the view: the form and the toggles that apply to it.</summary>
    public string Header
    {
        get
        {
            ViewSettings s = this.Settings;
            List<string> parts = [KindName(s)];
            if (s.Kind is ViewKind.RuleText or ViewKind.Mermaid or ViewKind.Dot or ViewKind.Html)
            {
                parts.Add("operators: " + (s.OperatorStyle?.ToString() ?? "default"));
            }

            if (s.Kind == ViewKind.Equation && s.Dialect == EquationDialect.LaTeX)
            {
                parts.Add("envelope: " + s.LatexWrap);
            }

            if (s.Kind is not (ViewKind.Json or ViewKind.Yaml))
            {
                string constants = s.ConstantStyle?.ToString() ?? "default";
                if (s.TrueText is not null || s.FalseText is not null || s.UnknownText is not null)
                {
                    constants += " (overrides)";
                }

                parts.Add("constants: " + constants);
            }

            if (s.Kind is ViewKind.Equation or ViewKind.Html)
            {
                parts.Add("simple variables: " + (s.SimpleVariables ? "on" : "off"));
            }

            if (s.Kind is ViewKind.Tree or ViewKind.Mermaid or ViewKind.Dot or ViewKind.Html)
            {
                string decision = "shown";
                if (!s.ShowDecision)
                {
                    decision = "off";
                }
                else if (this.evaluation() is null)
                {
                    decision = "none yet";
                }

                parts.Add("decision: " + decision);
            }

            return string.Join(" | ", parts)
                + "   [v view, d dialect, o operators, c constants, e overrides, l variables, m decision, y copy, f save]";
        }
    }

    /// <summary>Gets the file extension that a save of the current view suggests, with the dot.</summary>
    public string Extension =>
        this.Settings.Kind switch
        {
            ViewKind.RuleText => ".rule",
            ViewKind.Equation => this.Settings.Dialect switch
            {
                EquationDialect.LaTeX => ".tex",
                EquationDialect.MathML => ".mml",
                EquationDialect.Typst => ".typ",
                _ => ".txt",
            },
            ViewKind.Mermaid => ".mmd",
            ViewKind.Dot => ".dot",
            ViewKind.Html => ".html",
            ViewKind.Json => ".json",
            ViewKind.Yaml => ".yaml",
            _ => ".txt",
        };

    /// <summary>Shows the next or the previous form.</summary>
    /// <param name="direction">1 for the next form; -1 for the previous one.</param>
    public void CycleKind(int direction)
    {
        this.Settings.Kind = Cycle(Enum.GetValues<ViewKind>(), this.Settings.Kind, direction);
        this.Raise();
    }

    /// <summary>Shows the next equation dialect. Shows the equation view when another form is open.</summary>
    public void CycleDialect()
    {
        if (this.Settings.Kind != ViewKind.Equation)
        {
            this.Settings.Kind = ViewKind.Equation;
        }
        else if (this.Settings.Dialect == EquationDialect.LaTeX && this.Settings.LatexWrap != LatexWrapMode.MathJaxSafe)
        {
            // The LaTeX dialect has three envelopes. Walk through them before the next dialect.
            this.Settings.LatexWrap = Cycle(Enum.GetValues<LatexWrapMode>(), this.Settings.LatexWrap, 1);
        }
        else
        {
            this.Settings.Dialect = Cycle(Enum.GetValues<EquationDialect>(), this.Settings.Dialect, 1);
            this.Settings.LatexWrap = LatexWrapMode.None;
        }

        this.Raise();
    }

    /// <summary>Uses the next operator style: the printer default, word, symbolic, C-style.</summary>
    public void CycleOperatorStyle()
    {
        this.Settings.OperatorStyle = Cycle(OperatorStyles, this.Settings.OperatorStyle, 1);
        this.Raise();
    }

    /// <summary>Uses the next constant style: the printer default, letters, words, logic symbols.</summary>
    public void CycleConstantStyle()
    {
        this.Settings.ConstantStyle = Cycle(ConstantStyles, this.Settings.ConstantStyle, 1);
        this.Raise();
    }

    /// <summary>Turns the simple variables on or off.</summary>
    public void ToggleSimpleVariables()
    {
        this.Settings.SimpleVariables = !this.Settings.SimpleVariables;
        this.Raise();
    }

    /// <summary>Turns the marks of the current decision on or off.</summary>
    public void ToggleDecision()
    {
        this.Settings.ShowDecision = !this.Settings.ShowDecision;
        this.Raise();
    }

    /// <summary>Asks for the text of each constant. An empty answer clears the override; a cancel changes nothing.</summary>
    public void EditConstantOverrides()
    {
        ViewSettings s = this.Settings;
        IWorkbenchDialogs dialogs = this.services.Dialogs;
        string? trueText = dialogs.AskText("Text for True (empty for the style default)", s.TrueText ?? string.Empty);
        string? falseText = trueText is null
            ? null
            : dialogs.AskText("Text for False (empty for the style default)", s.FalseText ?? string.Empty);
        string? unknownText = falseText is null
            ? null
            : dialogs.AskText("Text for Unknown (empty for the style default)", s.UnknownText ?? string.Empty);
        if (trueText is null || falseText is null || unknownText is null)
        {
            return;
        }

        (string? previousTrue, string? previousFalse, string? previousUnknown) = (s.TrueText, s.FalseText, s.UnknownText);
        (s.TrueText, s.FalseText, s.UnknownText) = (NullIfEmpty(trueText), NullIfEmpty(falseText), NullIfEmpty(unknownText));

        // The constant format validates the texts. A bad override is refused here, and the old ones stay.
        try
        {
            _ = RuleViewRenderer.ConstantsOf(s);
        }
        catch (ArgumentException ex)
        {
            (s.TrueText, s.FalseText, s.UnknownText) = (previousTrue, previousFalse, previousUnknown);
            this.workbench.SetStatus("Constant text not used: " + ex.Message);
            return;
        }

        this.Raise();
    }

    /// <summary>Puts the printed text on the clipboard.</summary>
    /// <returns><see langword="true"/> when the clipboard took the text.</returns>
    public bool Copy()
    {
        ViewOutput output = this.Output;
        if (output.Text is not { } text)
        {
            this.workbench.SetStatus("Nothing to copy: " + output.Reason);
            return false;
        }

        if (!this.services.Clipboard.TrySet(text))
        {
            this.workbench.SetStatus("The clipboard is not available here. Save the view to a file with f instead.");
            return false;
        }

        this.workbench.SetStatus($"Copied the {KindName(this.Settings)} view to the clipboard.");
        return true;
    }

    /// <summary>Asks for a path and writes the printed text there.</summary>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool Save()
    {
        ViewOutput output = this.Output;
        if (output.Text is not { } text)
        {
            this.workbench.SetStatus("Nothing to save: " + output.Reason);
            return false;
        }

        string suggested = Path.ChangeExtension(
            this.workbench.Document.Path ?? Path.Combine(Environment.CurrentDirectory, "rule"),
            this.Extension
        );
        if (this.workbench.Prompts.AskSavePath(suggested) is not { } path)
        {
            return false;
        }

        ToolResult<string> written = OutputWriter.WriteFile(path, text);
        if (!written.TryGetValue(out string? file, out EquatableArray<Diagnostic> diagnostics))
        {
            Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            this.workbench.SetStatus(first is null ? $"Not saved to '{path}'." : $"{first.Code}: {first.Message}");
            return false;
        }

        this.workbench.SetStatus($"Saved the {KindName(this.Settings)} view to {file}.");
        return true;
    }

    private static string KindName(ViewSettings s)
    {
        return s.Kind switch
        {
            ViewKind.Tree => "Tree",
            ViewKind.RuleText => "Rule text",
            ViewKind.Equation => "Equation (" + s.Dialect + ")",
            ViewKind.Mermaid => "Mermaid",
            ViewKind.Dot => "DOT",
            ViewKind.Html => "HTML",
            ViewKind.Json => "JSON",
            _ => "YAML",
        };
    }

    private static string? NullIfEmpty(string text)
    {
        return text.Length == 0 ? null : text;
    }

    private static T Cycle<T>(IReadOnlyList<T> values, T current, int direction)
    {
        int index = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], current))
            {
                index = i;
                break;
            }
        }

        return values[(index + direction + values.Count) % values.Count];
    }

    private void Raise()
    {
        this.SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
