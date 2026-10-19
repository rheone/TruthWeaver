namespace TruthWeaver.Tool.Tui.Views;

using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Sessions;

/// <summary>
/// The export dialog: the output types under a heading, the formats of the chosen type under a second heading, the
/// options, and the destination. The chosen type and format stay highlighted. The buttons are Copy, To File and Cancel; the
/// path for To File is a field of the same dialog, so no second dialog opens. A double click on a list only selects.
/// </summary>
internal sealed class ExportDialog : Dialog
{
    /// <summary>The heading of the entry that exports every artifact to a folder.</summary>
    public const string FolderType = "Every artifact, to a folder";

    /// <summary>The line under the term limit that states which of the two truth-table options applies.</summary>
    public const string LimitNoteText =
        "A term limit overrides the box above. The box only lifts the cap while the limit is empty.";

    private readonly ExportDialogModel model;
    private readonly List<string> types;
    private readonly List<ExportEntry> variants = [];
    private readonly List<ExportEntry> equations;
    private readonly Label typeHeading;
    private readonly Label optionsHeading;
    private readonly Label imageNote;
    private readonly Label hint;
    private readonly Label limitLabel;
    private readonly Label limitNote;
    private readonly Label destinationHeading;
    private readonly Label formatHeading;
    private readonly Label[] equationLabels;
    private bool pathEdited;
    private bool settingPath;

    /// <summary>Initializes a new instance of the <see cref="ExportDialog"/> class.</summary>
    /// <param name="model">What the dialog starts with.</param>
    public ExportDialog(ExportDialogModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        this.model = model;
        this.types = [.. model.Entries.Select(e => e.Group).Distinct(StringComparer.Ordinal), FolderType];
        this.equations = [.. model.Entries.Where(e => string.Equals(e.Group, ExportGroups.Equation, StringComparison.Ordinal))];
        string[] dialects = [.. this.equations.Select(e => OptionOf(e, "dialect")).Distinct(StringComparer.Ordinal)];
        string[] constants = [.. this.equations.Select(e => OptionOf(e, "constants")).Distinct(StringComparer.Ordinal)];
        string[] envelopes =
        [
            "none",
            .. this.equations.Select(e => OptionOf(e, "latexWrap")).Where(w => w.Length > 0).Distinct(StringComparer.Ordinal),
        ];

        this.Title = "Export";
        this.Width = Dim.Percent(85);
        this.Height = 22;

        this.typeHeading = new Label
        {
            Text = "Output type",
            X = 0,
            Y = 0,
        };
        this.formatHeading = new Label
        {
            Text = "Format",
            X = 30,
            Y = 0,
        };
        this.Types = new ListView
        {
            X = 0,
            Y = 1,
            Width = 28,
            Height = 8,
        };
        this.Types.SetSource(new ObservableCollection<string>(this.types));
        this.Variants = new ListView
        {
            X = 30,
            Y = 1,
            Width = Dim.Fill(),
            Height = 8,
        };

        this.Dialect = NewDropDown(dialects, 46, 1);
        this.Envelope = NewDropDown(envelopes, 46, 3);
        this.Constants = NewDropDown(constants, 46, 5);
        this.equationLabels =
        [
            new Label
            {
                Text = "Dialect",
                X = 30,
                Y = 1,
            },
            new Label
            {
                Text = "LaTeX envelope",
                X = 30,
                Y = 3,
            },
            new Label
            {
                Text = "Constants",
                X = 30,
                Y = 5,
            },
        ];

        // Row 9 holds the note about a missing image renderer, always shown; row 10 holds the hint of the chosen entry.
        this.imageNote = new Label
        {
            X = 0,
            Y = 9,
            Width = Dim.Fill(),
            Text = model.ImageNote,
        };
        this.hint = new Label
        {
            X = 0,
            Y = 10,
            Width = Dim.Fill(),
            Text = string.Empty,
        };
        this.optionsHeading = new Label
        {
            Text = "Options",
            X = 0,
            Y = 11,
        };
        this.AllowLarger = new CheckBox
        {
            Text = model.LargerTruthTableText,
            X = 0,
            Y = 12,
            Value = model.AllowLargerTruthTable ? CheckState.Checked : CheckState.UnChecked,
        };
        this.limitLabel = new Label
        {
            Text = "Truth table term limit (empty for none)",
            X = 0,
            Y = 13,
        };
        this.TermLimit = new TextField
        {
            X = 40,
            Y = 13,
            Width = 6,
            Text = model.TruthTableTermLimit?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        };
        this.limitNote = new Label
        {
            Text = LimitNoteText,
            X = 0,
            Y = 14,
        };
        this.destinationHeading = new Label
        {
            Text = "File or folder for To File",
            X = 0,
            Y = 15,
        };
        this.PathField = new TextField
        {
            X = 0,
            Y = 16,
            Width = Dim.Fill(),
        };

        this.CopyButton = new Button { Text = "_Copy", IsDefault = true };
        this.FileButton = new Button { Text = "_To File" };
        this.CancelButton = new Button { Text = "Cance_l" };

        // A double click selects the row like a single click; it must never accept the dialog (Enter still does).
        foreach (ListView list in new[] { this.Types, this.Variants })
        {
            list.MouseBindings.ReplaceCommands(MouseFlags.LeftButtonDoubleClicked, Command.Activate);
            list.RowRender += (_, args) => HighlightChosen(list, args);
            list.ValueChanged += (_, _) => this.Refresh();
        }

        // Enter in the types moves on to the formats, the dropdowns of the equation or, for the folder export, which has neither,
        // the path field. It never writes: Enter in the path field or To File does.
        this.Types.Accepting += (_, args) =>
        {
            args.Handled = true;
            if (this.ChosenType == ExportGroups.Equation)
            {
                this.Dialect.SetFocus();
            }
            else if (this.ChosenType == FolderType)
            {
                this.PathField.SetFocus();
            }
            else
            {
                this.Variants.SetFocus();
            }
        };

        // Enter in the formats takes the default action: Copy, or To File when the entry cannot be copied.
        this.Variants.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Finish(this.CopyButton.Enabled ? ExportAction.Copy : ExportAction.ToFile);
        };

        this.Dialect.ValueChanged += (_, _) => this.Refresh();
        this.Envelope.ValueChanged += (_, _) => this.Refresh();
        this.Constants.ValueChanged += (_, _) => this.Refresh();
        this.PathField.TextChanged += (_, _) =>
        {
            if (!this.settingPath)
            {
                this.pathEdited = true;
            }
        };
        this.PathField.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Finish(ExportAction.ToFile);
        };
        this.CopyButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Finish(ExportAction.Copy);
        };
        this.FileButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Finish(ExportAction.ToFile);
        };
        this.CancelButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Closed?.Invoke(this, EventArgs.Empty);
        };

        this.Add(this.typeHeading, this.formatHeading, this.Types, this.Variants);
        foreach (Label label in this.equationLabels)
        {
            this.Add(label);
        }

        this.Add(this.Dialect, this.Envelope, this.Constants);
        this.Add(
            this.imageNote,
            this.hint,
            this.optionsHeading,
            this.AllowLarger,
            this.limitLabel,
            this.TermLimit,
            this.limitNote
        );
        this.Add(this.destinationHeading, this.PathField);
        this.AddButton(this.CopyButton);
        this.AddButton(this.FileButton);
        this.AddButton(this.CancelButton);
        this.Types.Value = 0;
        this.Refresh();
    }

    /// <summary>Raised when the author chose Copy or To File with a usable path (<see cref="Request"/> is set) or Cancel (it is not).</summary>
    public event EventHandler? Closed;

    /// <summary>Gets the line under the image note: the hint of the chosen entry, or a message about a refused term limit.</summary>
    public string HintText => this.hint.Text;

    /// <summary>Gets the line under the lists that names the image renderers to install. It shows for every chosen entry; it is empty when none is missing.</summary>
    public string ImageNoteText => this.imageNote.Text;

    /// <summary>Gets the list of output types.</summary>
    public ListView Types { get; }

    /// <summary>Gets the list of formats of the chosen type. It is hidden for the equation and the folder export.</summary>
    public ListView Variants { get; }

    /// <summary>Gets the dropdown for the equation dialect.</summary>
    public DropDownList Dialect { get; }

    /// <summary>Gets the dropdown for the LaTeX envelope, which only the LaTeX dialect uses.</summary>
    public DropDownList Envelope { get; }

    /// <summary>Gets the dropdown for the constant style of the equation.</summary>
    public DropDownList Constants { get; }

    /// <summary>Gets the option that allows truth tables over the term cap.</summary>
    public CheckBox AllowLarger { get; }

    /// <summary>Gets the field for the most terms of a truth table. Empty means no limit of its own.</summary>
    public TextField TermLimit { get; }

    /// <summary>Gets the field for the file or folder path of To File.</summary>
    public TextField PathField { get; }

    /// <summary>Gets the Copy button, the default action.</summary>
    public Button CopyButton { get; }

    /// <summary>Gets the To File button.</summary>
    public Button FileButton { get; }

    /// <summary>Gets the Cancel button.</summary>
    public Button CancelButton { get; }

    /// <summary>Gets what the author asked for, or <see langword="null"/> while the dialog is open or after Cancel.</summary>
    public ExportRequest? Request { get; private set; }

    /// <summary>Gets the name of the chosen output type.</summary>
    public string ChosenType => this.types[this.Types.Value ?? 0];

    /// <summary>Gets the chosen entry, or <see langword="null"/> for the export of every artifact to a folder.</summary>
    public ExportEntry? ChosenEntry
    {
        get
        {
            if (this.ChosenType == FolderType)
            {
                return null;
            }

            return this.ChosenType == ExportGroups.Equation
                ? this.EquationEntry()
                : this.variants[Math.Clamp(this.Variants.Value ?? 0, 0, this.variants.Count - 1)];
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.typeHeading.Dispose();
            this.formatHeading.Dispose();
            this.imageNote.Dispose();
            this.hint.Dispose();
            this.limitLabel.Dispose();
            this.limitNote.Dispose();
            this.TermLimit.Dispose();
            this.optionsHeading.Dispose();
            this.destinationHeading.Dispose();
            foreach (Label label in this.equationLabels)
            {
                label.Dispose();
            }

            this.Types.Dispose();
            this.Variants.Dispose();
            this.Dialect.Dispose();
            this.Envelope.Dispose();
            this.Constants.Dispose();
            this.AllowLarger.Dispose();
            this.PathField.Dispose();
            this.CopyButton.Dispose();
            this.FileButton.Dispose();
            this.CancelButton.Dispose();
        }
    }

    // The row that the list has chosen is painted with the active attribute, so it stays visible when the focus is elsewhere.
    private static void HighlightChosen(ListView list, ListViewRowEventArgs args)
    {
        if (list.Value == args.Row)
        {
            args.RowAttribute = list.GetAttributeForRole(VisualRole.Active);
        }
    }

    private static DropDownList NewDropDown(string[] items, int x, int y)
    {
        return new DropDownList
        {
            X = x,
            Y = y,
            Width = 24,
            ReadOnly = true,
            Source = new ListWrapper<string>([.. items]),
            Text = items.Length > 0 ? items[0] : string.Empty,
        };
    }

    private static string OptionOf(ExportEntry entry, string name)
    {
        return entry.Artifact?.Options.FirstOrDefault(o => o.Key == name).Value ?? string.Empty;
    }

    private ExportEntry EquationEntry()
    {
        string dialect = this.Dialect.Text ?? string.Empty;
        string wrap = this.Envelope.Text ?? "none";
        string style = this.Constants.Text ?? string.Empty;
        bool wrapped = string.Equals(dialect, "latex", StringComparison.Ordinal) && wrap != "none";
        return this.equations.FirstOrDefault(e =>
                OptionOf(e, "dialect") == dialect
                && OptionOf(e, "constants") == style
                && (wrapped ? OptionOf(e, "latexWrap") == wrap : OptionOf(e, "latexWrap").Length == 0)
            ) ?? this.equations[0];
    }

    // Shows the formats or the equation dropdowns for the chosen type, and updates the buttons and the suggested path.
    private void Refresh()
    {
        string type = this.ChosenType;
        bool equation = type == ExportGroups.Equation;
        bool folder = type == FolderType;
        if (!equation && !folder)
        {
            List<ExportEntry> entries = [.. this.model.Entries.Where(e => e.Group == type)];
            if (!this.variants.SequenceEqual(entries))
            {
                this.variants.Clear();
                this.variants.AddRange(entries);
                this.Variants.SetSource(new ObservableCollection<string>(entries.Select(e => e.Label)));
                this.Variants.Value = 0;
            }
        }

        this.Variants.Visible = !equation && !folder;
        this.formatHeading.Visible = !folder;
        this.Dialect.Visible = this.Envelope.Visible = this.Constants.Visible = equation;
        foreach (Label label in this.equationLabels)
        {
            label.Visible = equation;
        }

        this.Envelope.Enabled = string.Equals(this.Dialect.Text, "latex", StringComparison.Ordinal);
        ExportEntry? entry = this.ChosenEntry;

        // The hint line is the chosen entry's own; the image note has its own line, so no entry hides it.
        this.hint.Text = entry is { Hint.Length: > 0 } ? entry.Hint : string.Empty;
        bool canCopy = entry is { Kind: ExportEntryKind.Artifact };
        this.CopyButton.Enabled = canCopy;
        this.CopyButton.IsDefault = canCopy;
        this.FileButton.IsDefault = !canCopy;
        if (!this.pathEdited)
        {
            this.settingPath = true;
            this.PathField.Text = Path.Combine(
                this.model.SuggestedDirectory,
                entry is null ? "truthweaver-export" : entry.FileName
            );
            this.settingPath = false;
        }
    }

    private void Finish(ExportAction action)
    {
        string limitText = (this.TermLimit.Text ?? string.Empty).Trim();
        int? limit = null;
        if (limitText.Length > 0)
        {
            if (!int.TryParse(limitText, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < 1)
            {
                // A limit that is not a whole number of 1 or more keeps the dialog open on the field, like an empty path does.
                this.hint.Text = "The term limit must be a whole number of 1 or more, or empty.";
                _ = this.TermLimit.SetFocus();
                this.App?.Invoke(() => this.TermLimit.SetFocus());
                return;
            }

            limit = parsed;
        }

        string path = action == ExportAction.ToFile ? (this.PathField.Text ?? string.Empty).Trim() : string.Empty;
        if (action == ExportAction.ToFile && path.Length == 0)
        {
            // To File needs a path: the field takes the focus and the dialog stays open. A hotkey puts the focus on its button
            // after the command, so a running application moves the focus again once the key has been handled.
            _ = this.PathField.SetFocus();
            this.App?.Invoke(() => this.PathField.SetFocus());
            return;
        }

        this.Request = new ExportRequest(this.ChosenEntry, action, path, this.AllowLarger.Value == CheckState.Checked, limit);
        this.Closed?.Invoke(this, EventArgs.Empty);
    }
}
