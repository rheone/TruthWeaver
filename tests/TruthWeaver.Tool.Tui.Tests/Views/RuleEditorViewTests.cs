namespace TruthWeaver.Tool.Tui.Tests.Views;

using Terminal.Gui.Drawing;
using Terminal.Gui.Editor.Rendering;
using Terminal.Gui.Input;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The editor views of the workbench, which are built on the Terminal.Gui.Editor package.</summary>
[Collection(TerminalCollection.Name)]
public sealed class RuleEditorViewTests
{
    /// <summary>The characters of a diagnostic come out underlined in the severity color, and the others keep their color.</summary>
    [Fact]
    public void Marks_ErrorSpan_ColorsOnlyTheMarkedCharacters_Test()
    {
        using RuleEditorView editor = new() { Text = "a AND b" };
        editor.Marks = EditorMarks.Compute("a AND b", [Diagnostic.Error("TRE0001", "x", new SourceSpan(2, 3))]);
        Attribute normal = new(new Color(ColorName16.White), new Color(ColorName16.Black));
        CellVisualLine line = new(editor.Document!.GetLineByNumber(1));
        line.AddElement(new TextRunElement(0, 7, 0, "a AND b", normal));

        foreach (IVisualLineTransformer transformer in editor.LineTransformers)
        {
            transformer.Transform(line);
        }

        Attribute[] attributes = [.. line.Elements.Select(e => e.Attribute)];
        string[] texts = [.. line.Elements.OfType<TextRunElement>().Select(e => e.Text)];
        Assert.Equal(["a ", "AND", " b"], texts);
        Assert.Equal(normal, attributes[0]);
        Assert.Equal(ThemePalette.Dark.Of(DiagnosticSeverity.Error), attributes[1].Foreground);
        Assert.Equal(TextStyle.Underline, attributes[1].Style);
        Assert.Equal(normal, attributes[2]);
    }

    /// <summary>The cursor offset and the line and column agree after a jump to an offset in a later line.</summary>
    [Fact]
    public void MoveTo_OffsetInSecondLine_PutsTheCursorThere_Test()
    {
        using RuleEditorView editor = new() { Text = "a AND\nb AND c" };

        editor.MoveTo(8);

        Assert.Equal(new System.Drawing.Point(2, 1), editor.InsertionPoint);
    }

    /// <summary>The view pane shows the printed rule in an editor that no key can change.</summary>
    [Fact]
    public void ViewPane_TypedKey_DoesNotChangeTheShownText_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new ViewPane(services.Services)]);
        harness.Window.FocusRegion(PaneSlot.Side);
        ViewPaneView view = (ViewPaneView)harness.Window.ActivePaneViews[PaneSlot.Side];
        string before = view.BodyText;

        view.Body.NewKeyDownEvent(new Key('x'));

        Assert.True(view.Body.ReadOnly);
        Assert.Equal(before, view.BodyText);
    }

    /// <summary>The variables editor keeps the editor's own undo, because the variables have no document history.</summary>
    [Fact]
    public void VariablesEditor_CtrlZ_UndoesTheEditorsOwnEdit_Test()
    {
        using VariablesEditorView editor = new();
        editor.Text = "{}";
        editor.CaretOffset = 2;
        editor.NewKeyDownEvent(new Key('x'));
        Assert.Equal("{}x", editor.Text);

        editor.NewKeyDownEvent(Key.Z.WithCtrl);

        Assert.Equal("{}", editor.Text);
    }

    /// <summary>No source file of the terminal UI uses the obsolete TextView or suppresses its warning.</summary>
    [Fact]
    public void Sources_OfTheTerminalUi_HaveNoTextViewAndNoCs0618Suppression_Test()
    {
        string directory = Path.GetDirectoryName(typeof(Workbench).Assembly.Location)!;
        string root = directory;
        while (!Directory.Exists(Path.Combine(root, "src", "TruthWeaver.Tool.Tui")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repository root was not found.");
        }

        string[] offenders =
        [
            .. Directory
                .EnumerateFiles(Path.Combine(root, "src", "TruthWeaver.Tool.Tui"), "*.cs", SearchOption.AllDirectories)
                .Where(f =>
                    !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                )
                .Where(f =>
                {
                    string text = File.ReadAllText(f);
                    return text.Contains("TextView", StringComparison.Ordinal)
                        || text.Contains("CS0618", StringComparison.Ordinal);
                }),
        ];

        Assert.Empty(offenders);
    }
}
