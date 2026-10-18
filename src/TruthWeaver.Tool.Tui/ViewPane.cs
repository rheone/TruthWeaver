namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Core.Assignment;

/// <summary>
/// The view pane: the rule as a tree, rule text, equation, Mermaid, DOT, HTML, JSON or YAML, in the region at the right of
/// the editor. Keys inside the pane change the form and the options, copy the text and save it (see
/// <see cref="ViewController.Header"/>). The pane follows the document, so it shows the last valid tree while the editor
/// text has errors.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="ViewPane"/> class.</remarks>
/// <param name="services">The clipboard and dialogs, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
internal sealed class ViewPane(WorkbenchServices? services) : IWorkbenchPane
{
    private readonly WorkbenchServices? services = services;

    /// <summary>Initializes a new instance of the <see cref="ViewPane"/> class with the program's services.</summary>
    public ViewPane()
        : this(null) { }

    /// <summary>
    /// Gets or sets the source of the stub evaluation that the tree, Mermaid, DOT and HTML views mark. When it is
    /// <see langword="null"/> (the default), the pane marks the decision of the current assignment
    /// (<see cref="Workbench.Outcome"/>), so a value set in the Assign pane shows here at once. A test can set its own source.
    /// </summary>
    public Func<StubEvaluation?>? EvaluationSource { get; set; }

    /// <inheritdoc />
    public string Title => "View";

    /// <inheritdoc />
    public PaneSlot Slot => PaneSlot.Side;

    /// <summary>Gets the controller of the last view this pane created, for tests.</summary>
    internal ViewController? Controller { get; private set; }

    /// <inheritdoc />
    public View CreateView(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        // Read the assignment before the subscription below, so the outcome is fresh in the DocumentChanged handler.
        _ = workbench.Assignment;
        ViewController controller = new(
            workbench,
            this.services ?? WorkbenchServices.Current,
            () => this.EvaluationSource is { } source ? source() : workbench.Outcome.Evaluation
        );
        this.Controller = controller;

        ViewPaneView container = new(controller);

        // A key that is not a view key goes on to the window and its commands.
        container.Body.KeyDown += (_, key) =>
        {
            if (Handle(controller, key))
            {
                key.Handled = true;
            }
        };
        workbench.DocumentChanged += (_, _) => container.Show();
        workbench.AssignmentChanged += (_, _) => container.Show();
        controller.SettingsChanged += (_, _) => container.Show();
        container.Show();
        return container;
    }

    private static bool Handle(ViewController controller, Key key)
    {
        if (key.IsCtrl || key.IsAlt)
        {
            return false;
        }

        switch (key.AsRune.ToString())
        {
            case "v":
                controller.CycleKind(1);
                return true;
            case "V":
                controller.CycleKind(-1);
                return true;
            case "d":
                controller.CycleDialect();
                return true;
            case "o":
                controller.CycleOperatorStyle();
                return true;
            case "c":
                controller.CycleConstantStyle();
                return true;
            case "e":
                controller.EditConstantOverrides();
                return true;
            case "l":
                controller.ToggleSimpleVariables();
                return true;
            case "m":
                controller.ToggleDecision();
                return true;
            case "y":
                _ = controller.Copy();
                return true;
            case "f":
                _ = controller.Save();
                return true;
            default:
                return false;
        }
    }
}
