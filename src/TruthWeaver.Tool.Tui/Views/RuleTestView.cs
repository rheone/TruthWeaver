namespace TruthWeaver.Tool.Tui.Views;

using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The view of the Tests pane: a field for the path of the rule test file, a field for the name of the case to save, the
/// list of the cases and a line with the reasons the selected case failed. It reads and changes the file only through
/// <see cref="Workbench.RuleTests"/>.
/// </summary>
internal sealed class RuleTestView : View
{
    private readonly Workbench workbench;
    private readonly Label fileLabel;
    private readonly TextField file;
    private readonly Label caseLabel;
    private readonly TextField caseName;
    private readonly ObservableCollection<string> lines = [];
    private readonly Label detail;
    private bool syncing;

    /// <summary>Initializes a new instance of the <see cref="RuleTestView"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public RuleTestView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.Width = Dim.Fill();
        this.Height = Dim.Fill();
        this.CanFocus = true;

        this.fileLabel = new Label
        {
            X = 0,
            Y = 0,
            Text = "Test file:",
        };
        this.file = new TextField
        {
            X = 11,
            Y = 0,
            Width = Dim.Fill(),
        };
        this.caseLabel = new Label
        {
            X = 0,
            Y = 1,
            Text = "Case name:",
        };
        this.caseName = new TextField
        {
            X = 11,
            Y = 1,
            Width = Dim.Fill(),
        };
        this.Cases = new ListView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };
        this.Cases.SetSource(this.lines);
        this.detail = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };
        this.Add(this.fileLabel, this.file, this.caseLabel, this.caseName, this.Cases, this.detail);

        this.file.TextChanged += (_, _) => this.ReadFields();
        this.caseName.TextChanged += (_, _) => this.ReadFields();
        this.file.KeyDown += this.OnFileKeyDown;
        this.Cases.KeyDown += this.OnCaseKeyDown;
        this.Cases.ValueChanged += (_, _) => this.ShowDetail();
        workbench.RuleTestsChanged += (_, _) => this.Show();
        this.Show();
    }

    /// <summary>Gets the field for the path of the rule test file, for tests that type into it.</summary>
    public View FileField => this.file;

    /// <summary>Gets the field for the name of the case to save, for tests that type into it.</summary>
    public View CaseNameField => this.caseName;

    /// <summary>Gets the list of the cases, for tests that move through it.</summary>
    public ListView Cases { get; }

    /// <summary>Gets the line of each case in the list.</summary>
    public IReadOnlyList<string> CaseLines => this.lines;

    /// <summary>Gets the text under the list: the reasons the selected case failed, or the last message.</summary>
    public string DetailText => this.detail.Text;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The base class disposes the subviews; disposing a view twice does nothing, so the fields are disposed after it.
        base.Dispose(disposing);
        if (disposing)
        {
            this.fileLabel.Dispose();
            this.file.Dispose();
            this.caseLabel.Dispose();
            this.caseName.Dispose();
            this.Cases.Dispose();
            this.detail.Dispose();
        }
    }

    private void ReadFields()
    {
        if (this.syncing)
        {
            return;
        }

        this.workbench.RuleTests.FilePath = this.file.Text;
        this.workbench.RuleTests.CaseName = this.caseName.Text;
    }

    // Enter in the file field reads the cases of the file.
    private void OnFileKeyDown(object? sender, Key key)
    {
        if (key == Key.Enter)
        {
            this.workbench.OpenRuleTestFile();
            key.Handled = true;
        }
    }

    // Enter loads the selected case into the Assign pane; Delete removes it from the file.
    private void OnCaseKeyDown(object? sender, Key key)
    {
        if (key == Key.Enter)
        {
            this.workbench.LoadCase(this.Cases.Value ?? -1);
        }
        else if (key == Key.Delete)
        {
            this.workbench.DeleteCase(this.Cases.Value ?? -1);
        }
        else
        {
            return;
        }

        key.Handled = true;
    }

    private void Show()
    {
        RuleTestSession session = this.workbench.RuleTests;
        this.syncing = true;
        try
        {
            if (this.file.Text != session.FilePath)
            {
                this.file.Text = session.FilePath;
            }

            if (this.caseName.Text != session.CaseName)
            {
                this.caseName.Text = session.CaseName;
            }
        }
        finally
        {
            this.syncing = false;
        }

        int selected = this.Cases.Value ?? 0;
        this.lines.Clear();
        foreach (RuleTestRow row in session.Rows)
        {
            this.lines.Add(row.Line);
        }

        if (session.Rows.Count > 0)
        {
            this.Cases.Value = Math.Clamp(selected, 0, session.Rows.Count - 1);
        }

        this.ShowDetail();
    }

    private void ShowDetail()
    {
        RuleTestSession session = this.workbench.RuleTests;
        int index = this.Cases.Value ?? -1;
        this.detail.Text =
            index >= 0 && index < session.Rows.Count && session.Rows[index].Failures.Count > 0
                ? session.Rows[index].Failures[0].ReplaceLineEndings(" ")
                : session.Message;
    }
}
