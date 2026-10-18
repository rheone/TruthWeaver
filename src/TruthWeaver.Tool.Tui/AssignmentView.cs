namespace TruthWeaver.Tool.Tui;

using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Core.Assignment;

/// <summary>
/// The view of the Assign pane: the result and policy lines on top, the term list on the left, the trace tree on the right
/// and the reason of the selected node at the bottom. It reads <see cref="Workbench.Outcome"/> and changes the assignment
/// only through <see cref="Workbench.Assignment"/> and <see cref="Workbench.NotifyAssignmentChanged"/>.
/// </summary>
internal sealed class AssignmentView : View
{
    private readonly Workbench workbench;
    private readonly Label result;
    private readonly Label policy;
    private readonly ObservableCollection<string> lines = [];
    private readonly TreeView<TraceLine> tree;
    private readonly Label reason;
    private readonly List<TermAssignment> shown = [];
    private TraceLine? rootLine;

    /// <summary>Initializes a new instance of the <see cref="AssignmentView"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public AssignmentView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // Reading the assignment first makes the workbench drop a stale outcome before this view reads it.
        _ = workbench.Assignment;
        this.workbench = workbench;
        this.Width = Dim.Fill();
        this.Height = Dim.Fill();
        this.CanFocus = true;

        this.result = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
        };
        this.policy = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
        };
        this.Terms = new ListView
        {
            X = 0,
            Y = 2,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1),
        };
        this.Terms.SetSource(this.lines);

        // T, F and U set a value, so the list must not use them to search for a term by its first letter.
        this.Terms.KeystrokeNavigator = null;
        this.tree = new TreeView<TraceLine>
        {
            X = Pos.Right(this.Terms) + 1,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            TreeBuilder = new DelegateTreeBuilder<TraceLine>(line => line.Children, line => line.Children.Count > 0),
            AspectGetter = AssignmentText.TraceLineText,
        };
        this.tree.ColorGetter = line =>
            TruthColors.SchemeOf(
                line.Value,
                this.workbench.Palette,
                this.tree.GetAttributeForRole(VisualRole.Normal).Background
            );
        this.reason = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };
        this.Add(this.result, this.policy, this.Terms, this.tree, this.reason);

        this.Terms.KeyDown += this.OnTermKeyDown;
        this.tree.SelectionChanged += (_, e) => this.reason.Text = e.NewValue?.Reason ?? this.rootLine?.Reason ?? string.Empty;
        workbench.DocumentChanged += (_, _) => this.Show();
        workbench.AssignmentChanged += (_, _) => this.Show();
        workbench.ThemeChanged += (_, _) => this.tree.SetNeedsDraw();
        this.Show();
    }

    /// <summary>Gets the term list, for tests that move through it.</summary>
    public ListView Terms { get; }

    /// <summary>Gets the text of the result line.</summary>
    public string ResultText => this.result.Text;

    /// <summary>Gets the text of the policy line.</summary>
    public string PolicyText => this.policy.Text;

    /// <summary>Gets the text of the reason line.</summary>
    public string ReasonText => this.reason.Text;

    /// <summary>Gets the line of each term in the list.</summary>
    public IReadOnlyList<string> TermLines => this.lines;

    /// <summary>Gets the line of each node of the trace tree, as drawn, from the top down, with two spaces for each level.</summary>
    public IReadOnlyList<string> TraceLines => this.rootLine is null ? [] : Flatten(this.rootLine, 0);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The base class disposes the subviews; disposing a view twice does nothing, so the fields are disposed after it.
        base.Dispose(disposing);
        if (disposing)
        {
            this.result.Dispose();
            this.policy.Dispose();
            this.Terms.Dispose();
            this.tree.Dispose();
            this.reason.Dispose();
        }
    }

    private static List<string> Flatten(TraceLine line, int depth)
    {
        List<string> flat = [new string(' ', depth * 2) + AssignmentText.TraceLineText(line)];
        foreach (TraceLine child in line.Children)
        {
            flat.AddRange(Flatten(child, depth + 1));
        }

        return flat;
    }

    // T, F and U in either case; any other key is not an assignment.
    private static TruthValue? ValueOf(Key key)
    {
        return char.ToUpperInvariant((char)key.AsRune.Value) switch
        {
            'T' => TruthValue.True,
            'F' => TruthValue.False,
            'U' => TruthValue.Unknown,
            _ => null,
        };
    }

    private void OnTermKeyDown(object? sender, Key key)
    {
        if (key == Key.Space)
        {
            this.Assign(null);
        }
        else if (!key.IsCtrl && !key.IsAlt && ValueOf(key) is { } value)
        {
            this.Assign(value);
        }
        else
        {
            return;
        }

        key.Handled = true;
    }

    // A null value moves the selected term to its next value; otherwise the term takes the value.
    private void Assign(TruthValue? value)
    {
        if (this.Terms.Value is not int index || index >= this.shown.Count)
        {
            return;
        }

        TermAssignment term = this.shown[index];
        if (term.Key is null)
        {
            this.workbench.SetStatus("This term takes its value from the data: load variables that resolve the reference.");
            return;
        }

        if (value is { } chosen)
        {
            this.workbench.Assignment.Set(term.Key, chosen);
        }
        else
        {
            _ = this.workbench.Assignment.Cycle(term.Key);
        }

        this.workbench.NotifyAssignmentChanged();
    }

    private void Show()
    {
        AssignmentOutcome outcome = this.workbench.Outcome;
        this.result.Text = AssignmentText.ResultLine(outcome);
        this.policy.Text = outcome.HasRule ? AssignmentText.PolicyLine(outcome) : string.Empty;

        int selected = this.Terms.Value ?? 0;
        this.shown.Clear();
        this.shown.AddRange(outcome.Terms);
        this.lines.Clear();
        foreach (TermAssignment term in this.shown)
        {
            this.lines.Add(AssignmentText.TermLine(term));
        }

        if (this.shown.Count > 0)
        {
            this.Terms.Value = Math.Clamp(selected, 0, this.shown.Count - 1);
        }

        this.tree.ClearObjects();
        this.rootLine = outcome.Trace;
        if (this.rootLine is not null)
        {
            this.tree.AddObject(this.rootLine);
            this.tree.ExpandAll();
            this.tree.SelectedObject = this.rootLine;
        }

        this.reason.Text = this.rootLine?.Reason ?? outcome.Reason;
    }
}
