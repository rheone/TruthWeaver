namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>
/// The workbench window: three regions (editor on the left, side view on the right, a region below), a status line and
/// the key help overlay. Each region holds the panes that <see cref="WorkbenchPanes"/> registers for it and shows one at a
/// time. The keys come from <see cref="WorkbenchCommands"/>. Every region can take focus, be maximised and be resized with
/// the keys or by dragging its border with the mouse.
/// </summary>
internal sealed class WorkbenchWindow : Window
{
    private static readonly PaneSlot[] SlotOrder = [PaneSlot.Editor, PaneSlot.Side, PaneSlot.Bottom];

    private readonly Dictionary<PaneSlot, Region> regions = [];
    private readonly IReadOnlyList<WorkbenchCommand> commands;
    private readonly Label status;
    private readonly FrameView help;
    private readonly Label helpText;

    /// <summary>Initializes a new instance of the <see cref="WorkbenchWindow"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="panes">The panes, or <see langword="null"/> for <see cref="WorkbenchPanes.Default"/>.</param>
    /// <param name="commands">The keys, or <see langword="null"/> for <see cref="WorkbenchCommands.Default"/>.</param>
    public WorkbenchWindow(
        Workbench workbench,
        IReadOnlyList<IWorkbenchPane>? panes = null,
        IReadOnlyList<WorkbenchCommand>? commands = null
    )
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.Workbench = workbench;
        this.commands = commands ?? WorkbenchCommands.Default;
        this.Title = "TruthWeaver workbench";

        foreach (PaneSlot slot in SlotOrder)
        {
            List<(IWorkbenchPane Pane, View View)> slotPanes =
            [
                .. (panes ?? WorkbenchPanes.Default).Where(p => p.Slot == slot).Select(p => (p, p.CreateView(workbench))),
            ];
            if (slotPanes.Count == 0)
            {
                continue;
            }

            Region region = new(slot, slotPanes);
            this.regions[slot] = region;
            this.Add(region.Frame);
        }

        this.status = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
        };
        this.help = new FrameView
        {
            Title = "Keys (F1 or Esc to close)",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            Visible = false,
            CanFocus = true,
        };
        this.helpText = new Label { Text = HelpText(this.commands) };
        this.help.Add(this.helpText);
        this.Add(this.status, this.help);

        this.KeyDown += this.OnWorkbenchKeyDown;
        workbench.DocumentChanged += (_, _) => this.ShowStatus();
        workbench.StatusChanged += (_, _) => this.ShowStatus();

        this.ApplyLayout();
        this.ShowStatus();
    }

    /// <summary>Gets the shared state.</summary>
    public Workbench Workbench { get; }

    /// <summary>Gets a value indicating whether the key help overlay shows.</summary>
    public bool IsHelpVisible => this.help.Visible;

    /// <summary>Gets the text of the status line.</summary>
    public string StatusText => this.status.Text;

    /// <summary>Gets the view of the pane that each region shows. The window owns the views.</summary>
    public IReadOnlyDictionary<PaneSlot, View> ActivePaneViews =>
        this.regions.ToDictionary(pair => pair.Key, pair => pair.Value.Active.View);

    /// <summary>Gets the text of the key help overlay: one line per command, the key and what it does.</summary>
    /// <param name="commands">The commands.</param>
    /// <returns>The text.</returns>
    public static string HelpText(IEnumerable<WorkbenchCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        WorkbenchCommand[] list = [.. commands];
        int width = list.Length == 0 ? 0 : list.Max(c => c.Key.ToString().Length);
        return string.Join('\n', list.Select(c => $"{c.Key.ToString().PadRight(width)}  {c.Description}"));
    }

    /// <summary>Gets the status line for the state of a workbench: file, format, unsaved changes, diagnostics and the last message.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The status line.</returns>
    public static string StatusOf(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        RuleDocument document = workbench.Document;
        string name = workbench.DocumentLabel ?? (document.Path is null ? "(new rule)" : Path.GetFileName(document.Path));
        int errors = document.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        int warnings = document.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        List<string> parts =
        [
            $"{name} [{Workbench.FormatName(document.Format)}]{(document.IsDirty ? " *" : string.Empty)}",
            $"{errors} error{(errors == 1 ? string.Empty : "s")}, {warnings} warning{(warnings == 1 ? string.Empty : "s")}",
        ];
        if (!document.IsRuleCurrent)
        {
            parts.Add(document.Rule is null ? "no valid tree" : "showing the last valid tree");
        }

        if (workbench.Status.Length > 0)
        {
            parts.Add(workbench.Status);
        }

        parts.Add("F1 keys");
        return string.Join(" | ", parts);
    }

    /// <summary>Gets the slot of the region that has the focus, or <see langword="null"/> when none has it.</summary>
    /// <returns>The slot.</returns>
    public PaneSlot? FocusedSlot()
    {
        return this.regions.Values.FirstOrDefault(r => r.Frame.HasFocus)?.Slot;
    }

    /// <summary>Gets the title of the pane a region shows.</summary>
    /// <param name="slot">The region.</param>
    /// <returns>The title, or <see langword="null"/> when the window has no such region.</returns>
    public string? ActivePaneTitle(PaneSlot slot)
    {
        return this.regions.TryGetValue(slot, out Region? region) ? region.Active.Pane.Title : null;
    }

    /// <summary>Gets a value indicating whether a region shows.</summary>
    /// <param name="slot">The region.</param>
    /// <returns><see langword="true"/> when the window has the region and it is visible.</returns>
    public bool IsRegionVisible(PaneSlot slot)
    {
        return this.regions.TryGetValue(slot, out Region? region) && region.Frame.Visible;
    }

    /// <summary>Puts the focus in a region.</summary>
    /// <param name="slot">The region.</param>
    public void FocusRegion(PaneSlot slot)
    {
        if (this.regions.TryGetValue(slot, out Region? region) && region.Frame.Visible)
        {
            region.Active.View.SetFocus();
        }
    }

    /// <summary>Shows or hides the key help overlay.</summary>
    public void ToggleHelp()
    {
        this.help.Visible = !this.help.Visible;
        if (this.help.Visible)
        {
            this.help.SetFocus();
        }
        else
        {
            this.FocusRegion(PaneSlot.Editor);
        }
    }

    /// <summary>Moves the focus to the next visible region, in the order editor, side, bottom.</summary>
    /// <param name="direction">1 for the next region; -1 for the previous one.</param>
    public void FocusNextRegion(int direction)
    {
        PaneSlot[] visible = [.. SlotOrder.Where(this.IsRegionVisible)];
        if (visible.Length == 0)
        {
            return;
        }

        int current = this.FocusedSlot() is { } slot ? Array.IndexOf(visible, slot) : -1;
        int next = current < 0 ? 0 : (current + direction + visible.Length) % visible.Length;
        this.FocusRegion(visible[next]);
    }

    /// <summary>Shows the next pane of the focused region, when the region has more than one.</summary>
    /// <param name="direction">1 for the next pane; -1 for the previous one.</param>
    public void SwitchPane(int direction)
    {
        if (this.FocusedSlot() is { } slot && this.regions[slot].Switch(direction))
        {
            this.regions[slot].Active.View.SetFocus();
        }
    }

    /// <summary>Shows a pane in its region and puts the focus in it.</summary>
    /// <param name="title">The title of the pane.</param>
    /// <returns><see langword="true"/> when the window has a pane with that title.</returns>
    public bool ShowPane(string title)
    {
        foreach (Region region in this.regions.Values)
        {
            int index = region.Panes.FindIndex(p => string.Equals(p.Pane.Title, title, StringComparison.Ordinal));
            if (index >= 0)
            {
                region.ShowPane(index);
                if (region.Frame.Visible)
                {
                    region.Active.View.SetFocus();
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>Maximises the focused region (the editor when no region has focus), or shows every region again.</summary>
    public void ToggleMaximised()
    {
        PaneSlot slot = this.FocusedSlot() ?? PaneSlot.Editor;
        this.Workbench.Layout.ToggleMaximised(slot);
        this.ApplyLayout();
        this.FocusRegion(slot);
    }

    /// <summary>Moves the line between the editor and side regions.</summary>
    /// <param name="steps">The number of steps; negative is left.</param>
    public void MoveVerticalDivider(int steps)
    {
        this.Workbench.Layout.MoveVerticalDivider(steps);
        this.ApplyLayout();
    }

    /// <summary>Moves the line above the bottom region.</summary>
    /// <param name="steps">The number of steps; negative is up.</param>
    public void MoveHorizontalDivider(int steps)
    {
        this.Workbench.Layout.MoveHorizontalDivider(steps);
        this.ApplyLayout();
    }

    /// <summary>Stops the application. Unsaved changes need the author's consent first (<see cref="OnIsRunningChanging"/>).</summary>
    public void Quit()
    {
        this.RequestStop();
    }

    /// <summary>Asks for consent to close when the document has unsaved changes.</summary>
    /// <returns><see langword="true"/> when the window may close.</returns>
    public bool ConfirmClose()
    {
        return !this.Workbench.Document.IsDirty || this.Workbench.Prompts.ConfirmDiscard("quit");
    }

    /// <summary>Runs the command bound to a key.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when a command ran.</returns>
    public bool RunCommand(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (this.help.Visible && key == Key.Esc)
        {
            this.ToggleHelp();
            return true;
        }

        WorkbenchCommand? command = this.commands.FirstOrDefault(c => c.Key == key);
        if (command is null)
        {
            return false;
        }

        command.Execute(this);
        return true;
    }

    /// <inheritdoc />
    /// <remarks>Every way to stop (Ctrl+Q, the quit key of the terminal library) asks first when there are unsaved changes.</remarks>
    protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    {
        if (oldIsRunning && !newIsRunning && !this.ConfirmClose())
        {
            return true;
        }

        return base.OnIsRunningChanging(oldIsRunning, newIsRunning);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The base class disposes the subviews; disposing a view twice does nothing, so the fields are disposed after it.
        base.Dispose(disposing);
        if (disposing)
        {
            this.status.Dispose();
            this.helpText.Dispose();
            this.help.Dispose();
            foreach (Region region in this.regions.Values)
            {
                region.Dispose();
            }
        }
    }

    // Keys reach the focused pane first; a key the pane does not take reaches the window and its commands.
    private void OnWorkbenchKeyDown(object? sender, Key key)
    {
        if (!key.Handled && this.RunCommand(key))
        {
            key.Handled = true;
        }
    }

    private void ShowStatus()
    {
        this.status.Text = StatusOf(this.Workbench);
    }

    // Places the regions from the layout model. A region with no pane is absent, and its neighbors take its space.
    private void ApplyLayout()
    {
        PaneLayout layout = this.Workbench.Layout;
        bool hasSide = this.regions.ContainsKey(PaneSlot.Side);
        bool hasBottom = this.regions.ContainsKey(PaneSlot.Bottom);
        Dim topHeight = hasBottom ? Dim.Percent(layout.TopHeightPercent) : Dim.Fill(1);

        foreach (Region region in this.regions.Values)
        {
            region.Frame.Visible = layout.IsVisible(region.Slot);
            if (layout.Maximised == region.Slot)
            {
                region.Frame.X = 0;
                region.Frame.Y = 0;
                region.Frame.Width = Dim.Fill();
                region.Frame.Height = Dim.Fill(1);
                continue;
            }

            View? editor = this.regions.TryGetValue(PaneSlot.Editor, out Region? left) ? left.Frame : null;
            switch (region.Slot)
            {
                case PaneSlot.Editor:
                    region.Frame.X = 0;
                    region.Frame.Y = 0;
                    region.Frame.Width = hasSide ? Dim.Percent(layout.EditorWidthPercent) : Dim.Fill();
                    region.Frame.Height = topHeight;
                    break;
                case PaneSlot.Side:
                    region.Frame.X = editor is null ? 0 : Pos.Right(editor);
                    region.Frame.Y = 0;
                    region.Frame.Width = Dim.Fill();
                    region.Frame.Height = editor is null ? topHeight : Dim.Height(editor);
                    break;
                default:
                    region.Frame.X = 0;
                    region.Frame.Y = editor is null ? 0 : Pos.Bottom(editor);
                    region.Frame.Width = Dim.Fill();
                    region.Frame.Height = Dim.Fill(1);
                    break;
            }
        }

        this.SetNeedsLayout();
    }

    /// <summary>One region: its frame, its panes and the pane it shows.</summary>
    private sealed class Region : IDisposable
    {
        private int active;

        public Region(PaneSlot slot, List<(IWorkbenchPane Pane, View View)> panes)
        {
            this.Slot = slot;
            this.Panes = panes;
            this.Frame = new FrameView
            {
                CanFocus = true,

                // The editor region can be resized with the mouse by dragging its right or bottom border; the other
                // regions follow it.
                Arrangement =
                    slot == PaneSlot.Editor
                        ? ViewArrangement.RightResizable | ViewArrangement.BottomResizable
                        : ViewArrangement.Fixed,
            };
            foreach ((IWorkbenchPane _, View view) in panes)
            {
                view.Visible = false;
                this.Frame.Add(view);
            }

            this.Show(0);
        }

        public PaneSlot Slot { get; }

        public List<(IWorkbenchPane Pane, View View)> Panes { get; }

        public FrameView Frame { get; }

        public (IWorkbenchPane Pane, View View) Active => this.Panes[this.active];

        public bool Switch(int direction)
        {
            if (this.Panes.Count < 2)
            {
                return false;
            }

            this.Show((this.active + direction + this.Panes.Count) % this.Panes.Count);
            return true;
        }

        public void ShowPane(int index)
        {
            this.Show(index);
        }

        public void Dispose()
        {
            this.Frame.Dispose();
        }

        private void Show(int index)
        {
            this.Active.View.Visible = false;
            this.active = index;
            this.Active.View.Visible = true;
            this.Frame.Title =
                this.Panes.Count < 2 ? this.Active.Pane.Title : $"{this.Active.Pane.Title} ({index + 1}/{this.Panes.Count})";
        }
    }
}
