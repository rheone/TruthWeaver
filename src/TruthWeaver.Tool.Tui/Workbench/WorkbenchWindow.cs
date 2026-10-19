namespace TruthWeaver.Tool.Tui.Workbench;

using System.Collections.ObjectModel;
using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;

/// <summary>
/// The workbench window: the menu bar on the first row, three regions (editor on the left, side view on the right, a region
/// below), a status line, a hint line with the most common keys, and the key reference overlay. Each region holds the panes
/// that <see cref="WorkbenchPanes"/> registers for it and shows one at a time. The commands and their keys come from
/// <see cref="WorkbenchCommands"/>, and <see cref="WorkbenchMenus"/> puts them in the menus. Every region can take focus, be
/// maximised and be resized with the keys or by dragging a divider (the borders between the regions) with the mouse.
/// </summary>
internal sealed class WorkbenchWindow : Window
{
    /// <summary>The last line of the key reference: the mouse is an extra way to do what the keys do.</summary>
    private const string MouseHelp =
        "Mouse: click a menu to open it, a pane to focus it or a tree node to select it, right click a pane for its menu, turn the wheel to scroll, drag a divider to resize. Every action has a key.";

    /// <summary>The first line of the key reference: how to reach the menus.</summary>
    private const string MenuHelp = "F1 or F10 opens the menus; Alt and the underlined letter opens one menu.";

    // The rows above the regions (the menu bar) and below them (the status line and the hint line).
    private const int TopRows = 1;
    private const int BottomRows = 2;

    private static readonly PaneSlot[] SlotOrder = [PaneSlot.Editor, PaneSlot.Side, PaneSlot.Bottom];

    private readonly Dictionary<PaneSlot, Region> regions = [];
    private readonly IReadOnlyList<WorkbenchCommand> commands;
    private readonly Label status;
    private readonly Label hint;
    private readonly FrameView help;
    private readonly ListView helpList;

    // The divider that a mouse drag moves, and the distance from the pointer to the divider line when the drag started,
    // so a grab on either of the two border lines of a divider does not make it jump.
    private Divider dragging;
    private int dragOffset;

    // The context menu that a right click opened last; the next one and the disposal of the window release it.
    private PopoverMenu? contextMenu;
    private IMouse? mouseHooked;

    /// <summary>Initializes a new instance of the <see cref="WorkbenchWindow"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="panes">The panes, or <see langword="null"/> for <see cref="WorkbenchPanes.Default"/>.</param>
    /// <param name="commands">The commands, or <see langword="null"/> for <see cref="WorkbenchCommands.Default"/>.</param>
    /// <param name="choices">The choices of the menus, or <see langword="null"/> for <see cref="WorkbenchCommands.Choices"/>.</param>
    public WorkbenchWindow(
        Workbench workbench,
        IReadOnlyList<IWorkbenchPane>? panes = null,
        IReadOnlyList<WorkbenchCommand>? commands = null,
        IReadOnlyList<WorkbenchChoice>? choices = null
    )
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.Workbench = workbench;
        this.commands = commands ?? WorkbenchCommands.Default;
        this.Title = TitleOf(workbench);

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

        this.MenuBar = WorkbenchMenus.Create(this, this.commands, choices ?? WorkbenchCommands.Choices);
        this.status = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(BottomRows),
            Width = Dim.Fill(),
            Height = 1,
        };
        this.hint = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = HintText(this.commands),
        };

        // The reference is longer than a small terminal, so it is a list that scrolls with the keys and the wheel.
        this.help = new FrameView
        {
            Title = "Key reference (Shift+F1 or Esc to close)",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(90),
            Height = Dim.Percent(90),
            Visible = false,
            CanFocus = true,
        };
        this.helpList = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        this.helpList.SetSource(new ObservableCollection<string>(HelpText(this.commands).Split('\n')));
        this.help.Add(this.helpList);
        this.Add(this.MenuBar, this.status, this.hint, this.help);

        this.KeyDown += this.OnWorkbenchKeyDown;

        // The status line shows the value of the selected term. Reading Assignment first puts its reset of the outcome ahead
        // of these handlers, so the value is never stale.
        _ = workbench.Assignment;
        workbench.DocumentChanged += (_, _) => this.ShowStatus();
        workbench.SelectedNodeChanged += (_, _) => this.ShowStatus();
        workbench.AssignmentChanged += (_, _) => this.ShowStatus();
        workbench.StatusChanged += (_, _) => this.ShowStatus();

        // The theme is process-wide in Terminal.Gui, so one call restyles every pane and every dialog; the window then draws again.
        WorkbenchAppearance.Apply(workbench.Theme, workbench.NoColor);
        workbench.ThemeChanged += (_, _) =>
        {
            WorkbenchAppearance.Apply(workbench.Theme, workbench.NoColor);
            this.SetNeedsDraw();
        };

        this.ApplyLayout();
        this.ShowStatus();
    }

    /// <summary>A line between the regions that a mouse drag moves.</summary>
    private enum Divider
    {
        /// <summary>No divider: no drag is in progress.</summary>
        None,

        /// <summary>The line between the editor and side regions; it sets the width of the editor region.</summary>
        Vertical,

        /// <summary>The line above the bottom region; it sets the height of the top regions.</summary>
        Horizontal,
    }

    /// <summary>Gets the shared state.</summary>
    public Workbench Workbench { get; }

    /// <summary>Gets a value indicating whether the key reference overlay shows.</summary>
    public bool IsHelpVisible => this.help.Visible;

    /// <summary>Gets a value indicating whether a context menu shows.</summary>
    public bool IsContextMenuOpen => this.contextMenu?.Visible is true;

    /// <summary>Gets the text of the status line.</summary>
    public string StatusText => this.status.Text;

    /// <summary>Gets the text of the hint line: the most common keys.</summary>
    public string HintLineText => this.hint.Text;

    /// <summary>Gets the menu bar on the first row of the window.</summary>
    public MenuBar MenuBar { get; }

    /// <summary>Gets the view of the pane that each region shows. The window owns the views.</summary>
    public IReadOnlyDictionary<PaneSlot, View> ActivePaneViews =>
        this.regions.ToDictionary(pair => pair.Key, pair => pair.Value.Active.View);

    /// <summary>
    /// Gets the text of the key reference overlay: a line about the menus, then for each menu its name and one line per
    /// command (the key and what it does), then a line about the mouse.
    /// </summary>
    /// <param name="commands">The commands.</param>
    /// <returns>The text.</returns>
    public static string HelpText(IEnumerable<WorkbenchCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        WorkbenchCommand[] list = [.. commands];
        int width = list.Length == 0 ? 0 : list.Max(c => KeyText(c.Key).Length);
        List<string> lines = [MenuHelp];
        foreach (WorkbenchMenuGroup menu in WorkbenchMenus.GroupsOf(list, []))
        {
            lines.Add(string.Empty);
            lines.Add(menu.Group.ToString());
            lines.AddRange(menu.Commands.Select(c => $"  {KeyText(c.Key).PadRight(width)}  {c.Description}"));
        }

        lines.Add(string.Empty);
        lines.Add(MouseHelp);
        return string.Join('\n', lines);
    }

    /// <summary>Gets the hint line: the key and the hint word of each command that has one, in the order of the commands.</summary>
    /// <param name="commands">The commands.</param>
    /// <returns>The text, such as <c>F1 Menus  Ctrl+S Save</c>.</returns>
    public static string HintText(IEnumerable<WorkbenchCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        return string.Join("  ", commands.Where(c => c.Hint is not null).Select(c => $"{KeyText(c.Key)} {c.Hint}"));
    }

    /// <summary>
    /// Gets the title of the window: the file name (or the label of the document) and the product name, with a leading
    /// <c>*</c> while the rule has unsaved changes.
    /// </summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The title.</returns>
    public static string TitleOf(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return $"{(workbench.HasUnsavedChanges ? "* " : string.Empty)}{DocumentName(workbench)} - TruthWeaver workbench";
    }

    /// <summary>Gets the status line for the state of a workbench: file, format, unsaved changes, diagnostics and the last message.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The status line.</returns>
    public static string StatusOf(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // The committed document: reading Document would apply the pending text and end the pause.
        RuleDocument document = workbench.CommittedDocument;
        string name = DocumentName(workbench);
        int errors = document.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        int warnings = document.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        List<string> parts =
        [
            $"{name} [{Workbench.FormatName(document.Format)}]{(document.IsDirty ? " *" : string.Empty)}",
            $"{errors} error{(errors == 1 ? string.Empty : "s")}, {warnings} warning{(warnings == 1 ? string.Empty : "s")}",
        ];
        if (workbench.IsChecking)
        {
            parts.Add("checking...");
        }

        if (!document.IsRuleCurrent)
        {
            parts.Add(document.Rule is null ? "no valid tree" : "showing the last valid tree");
        }

        // The value of the term under the cursor. Not while a compile waits: reading the tree would end the pause.
        if (!workbench.IsChecking && StructureController.For(workbench).SelectedValueText is { } value)
        {
            parts.Add(value);
        }

        if (workbench.Status.Length > 0)
        {
            parts.Add(workbench.Status);
        }

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

    /// <summary>
    /// Opens the menu bar on its first menu, as <see cref="WorkbenchMenus.MenuBarKey"/> does. With no running application,
    /// such as in a test with no terminal, there is nowhere to show a menu, and nothing happens.
    /// </summary>
    public void OpenMenus()
    {
        if (this.App is not null && !this.MenuBar.IsOpen())
        {
            _ = this.MenuBar.NewKeyDownEvent(this.MenuBar.Key);
        }
    }

    /// <summary>Shows or hides the key reference overlay.</summary>
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
        this.Workbench.Preferences?.SaveLayout(this.Workbench.Layout);
        this.ApplyLayout();
    }

    /// <summary>Moves the line above the bottom region.</summary>
    /// <param name="steps">The number of steps; negative is up.</param>
    public void MoveHorizontalDivider(int steps)
    {
        this.Workbench.Layout.MoveHorizontalDivider(steps);
        this.Workbench.Preferences?.SaveLayout(this.Workbench.Layout);
        this.ApplyLayout();
    }

    /// <summary>Stops the application. Unsaved changes need the author's consent first (<see cref="OnIsRunningChanging"/>).</summary>
    public void Quit()
    {
        this.RequestStop();
    }

    /// <summary>
    /// Opens a context menu at a point of the screen. With no running application, such as in a test with no terminal, there
    /// is nowhere to show a menu, and nothing happens.
    /// </summary>
    /// <param name="owner">The view that binds the pane keys of the entries.</param>
    /// <param name="entries">The entries.</param>
    /// <param name="screen">The screen position of the menu.</param>
    public void ShowContextMenu(View owner, IReadOnlyList<ContextMenuEntry> entries, Point screen)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(entries);

        if (this.App is not { } app)
        {
            return;
        }

        this.ReleaseContextMenu();
        PopoverMenu menu = new(ContextMenus.ItemsOf(this, owner, entries));

        // Registered only while it shows: a registered popover takes the keys of its items (such as W) from the whole window.
        _ = app.Popovers?.Register(menu);
        menu.VisibleChanged += (_, _) =>
        {
            if (!menu.Visible)
            {
                _ = app.Popovers?.DeRegister(menu);
            }
        };
        this.contextMenu = menu;
        menu.MakeVisible(screen);
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

        // A command with no key is in the menus only.
        WorkbenchCommand? command = this.commands.FirstOrDefault(c => c.Key != Key.Empty && c.Key == key);
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
    /// <remarks>
    /// The window moves the dividers itself. A press on a divider reaches the window because the region borders do not
    /// handle it. The window then holds the mouse, so every move goes to it until the release, which saves the sizes as the
    /// divider keys do. The release position also moves the divider, so a terminal that reports no moves while a button is
    /// held still resizes on press and release.
    /// </remarks>
    protected override bool OnMouseEvent(Mouse mouse)
    {
        ArgumentNullException.ThrowIfNull(mouse);

        Point point = this.ScreenToViewport(mouse.ScreenPosition);
        if (this.dragging != Divider.None)
        {
            if (mouse.IsReleased || mouse.IsSingleDoubleOrTripleClicked)
            {
                this.DragTo(point);
                this.EndDrag();
            }
            else if (mouse.Flags.HasFlag(MouseFlags.PositionReport))
            {
                this.DragTo(point);
            }

            return true;
        }

        if (
            mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed)
            && !mouse.Flags.HasFlag(MouseFlags.PositionReport)
            && this.DividerAt(point) is { } hit
        )
        {
            this.dragging = hit.Divider;
            this.dragOffset = (hit.Divider == Divider.Vertical ? point.X : point.Y) - hit.Line;
            this.App?.Mouse.GrabMouse(this);
            return true;
        }

        return base.OnMouseEvent(mouse);
    }

    /// <inheritdoc />
    protected override void OnIsRunningChanged(bool newIsRunning)
    {
        base.OnIsRunningChanged(newIsRunning);

        // The mouse reaches the application before any view, which is where a right click in any pane is seen.
        if (newIsRunning && this.App is { } app && !ReferenceEquals(this.mouseHooked, app.Mouse))
        {
            this.mouseHooked = app.Mouse;
            app.Mouse.MouseEvent += this.OnApplicationMouse;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // A drag that is still in progress holds the mouse; let it go so no event goes to a disposed window.
        if (disposing && this.App?.Mouse.IsGrabbed(this) is true)
        {
            this.App.Mouse.UngrabMouse();
        }

        // The base class disposes the subviews; disposing a view twice does nothing, so the fields are disposed after it.
        base.Dispose(disposing);
        if (disposing)
        {
            if (this.mouseHooked is { } mouse)
            {
                mouse.MouseEvent -= this.OnApplicationMouse;
            }

            this.ReleaseContextMenu();
            this.MenuBar.Dispose();
            this.status.Dispose();
            this.hint.Dispose();
            this.helpList.Dispose();
            this.help.Dispose();
            foreach (Region region in this.regions.Values)
            {
                region.Dispose();
            }
        }
    }

    // The text of a key for the hint line and the reference, such as "Ctrl+S"; a command with no key has none.
    private static string KeyText(Key key)
    {
        return key == Key.Empty ? string.Empty : key.ToString();
    }

    // The label of the document, or its file name, or the placeholder for a rule that has no file.
    private static string DocumentName(Workbench workbench)
    {
        string? path = workbench.CommittedDocument.Path;
        return workbench.DocumentLabel ?? (path is null ? "(new rule)" : Path.GetFileName(path));
    }

    // A right click on a pane with a context menu opens it. The click also selects the line under the pointer, so the menu
    // acts on what the author pointed at.
    private void OnApplicationMouse(object? sender, Mouse mouse)
    {
        if (mouse.Handled || mouse.Flags != MouseFlags.RightButtonClicked || ContextMenus.For(mouse.View) is not { } menu)
        {
            return;
        }

        if (mouse.View is ListView { Source: { } source } list)
        {
            int row = list.Viewport.Y + list.ScreenToViewport(mouse.ScreenPosition).Y;
            if (row >= 0 && row < source.Count && ReferenceEquals(list, menu.Owner))
            {
                list.Value = row;
            }
        }

        mouse.Handled = true;
        this.ShowContextMenu(menu.Owner, menu.Entries, mouse.ScreenPosition);
    }

    private void ReleaseContextMenu()
    {
        if (this.contextMenu is { } old)
        {
            this.contextMenu = null;
            _ = this.App?.Popovers?.DeRegister(old);
            old.Dispose();
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
        this.Title = TitleOf(this.Workbench);
    }

    // Places the regions from the layout model. A region with no pane is absent, and its neighbors take its space.
    private void ApplyLayout()
    {
        PaneLayout layout = this.Workbench.Layout;
        bool hasSide = this.regions.ContainsKey(PaneSlot.Side);
        bool hasBottom = this.regions.ContainsKey(PaneSlot.Bottom);
        Dim topHeight = hasBottom ? Dim.Percent(layout.TopHeightPercent) : Dim.Fill(BottomRows);

        foreach (Region region in this.regions.Values)
        {
            region.Frame.Visible = layout.IsVisible(region.Slot);
            if (layout.Maximised == region.Slot)
            {
                region.Frame.X = 0;
                region.Frame.Y = TopRows;
                region.Frame.Width = Dim.Fill();
                region.Frame.Height = Dim.Fill(BottomRows);
                continue;
            }

            View? editor = this.regions.TryGetValue(PaneSlot.Editor, out Region? left) ? left.Frame : null;
            switch (region.Slot)
            {
                case PaneSlot.Editor:
                    region.Frame.X = 0;
                    region.Frame.Y = TopRows;
                    region.Frame.Width = hasSide ? Dim.Percent(layout.EditorWidthPercent) : Dim.Fill();
                    region.Frame.Height = topHeight;
                    break;
                case PaneSlot.Side:
                    region.Frame.X = editor is null ? 0 : Pos.Right(editor);
                    region.Frame.Y = TopRows;
                    region.Frame.Width = Dim.Fill();
                    region.Frame.Height = editor is null ? topHeight : Dim.Height(editor);
                    break;
                default:
                    region.Frame.X = 0;
                    region.Frame.Y = editor is null ? TopRows : Pos.Bottom(editor);
                    region.Frame.Width = Dim.Fill();
                    region.Frame.Height = Dim.Fill(BottomRows);
                    break;
            }
        }

        this.SetNeedsLayout();
    }

    // Finds the divider under a point of the viewport, with the position of its line: the last column or row of the editor
    // region. Each divider is two border lines side by side (the border of the region on each side), and both grab it.
    // The vertical divider runs down the top regions; the horizontal divider runs across the whole window.
    private (Divider Divider, int Line)? DividerAt(Point point)
    {
        if (this.Workbench.Layout.Maximised is not null || !this.regions.TryGetValue(PaneSlot.Editor, out Region? editor))
        {
            return null;
        }

        Rectangle frame = editor.Frame.Frame;
        int right = frame.Right - 1;
        int bottom = frame.Bottom - 1;
        if (this.regions.ContainsKey(PaneSlot.Bottom) && (point.Y == bottom || point.Y == bottom + 1))
        {
            return (Divider.Horizontal, bottom);
        }

        if (
            this.regions.ContainsKey(PaneSlot.Side)
            && (point.X == right || point.X == right + 1)
            && point.Y >= frame.Y
            && point.Y < bottom
        )
        {
            return (Divider.Vertical, right);
        }

        return null;
    }

    // Moves the dragged divider so that its line is under the pointer, as a whole percent of the window, inside the limits of
    // the layout. The editor region has the width or height of the line plus one.
    private void DragTo(Point point)
    {
        Size size = this.Viewport.Size;
        if (this.dragging == Divider.Vertical && size.Width > 0)
        {
            int width = point.X - this.dragOffset + 1;
            this.Workbench.Layout.Restore((int)Math.Round(width * 100.0 / size.Width), null);
        }
        else if (this.dragging == Divider.Horizontal && size.Height > 0)
        {
            // The editor region starts below the menu bar.
            int height = point.Y - this.dragOffset - TopRows + 1;
            this.Workbench.Layout.Restore(null, (int)Math.Round(height * 100.0 / size.Height));
        }

        this.ApplyLayout();
    }

    // Ends a drag: the window lets the mouse go and saves the sizes, as the divider keys do.
    private void EndDrag()
    {
        this.dragging = Divider.None;
        if (this.App?.Mouse.IsGrabbed(this) is true)
        {
            this.App.Mouse.UngrabMouse();
        }

        this.Workbench.Preferences?.SaveLayout(this.Workbench.Layout);
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

                // Fixed: the window moves the dividers itself (OnMouseEvent), so the sizes stay percentages of the
                // layout and are saved. The resize of Terminal.Gui would set an absolute size that is never saved,
                // would take only the border of the editor region, and would take Ctrl+F5 for its arrange mode.
                Arrangement = ViewArrangement.Fixed,
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
