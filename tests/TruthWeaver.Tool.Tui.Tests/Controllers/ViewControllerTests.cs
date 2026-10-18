namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The toggles, copy and save of the view pane, with no terminal.</summary>
public sealed class ViewControllerTests
{
    /// <summary>Copy puts the printed text on the clipboard and says so on the status line.</summary>
    [Fact]
    public void Copy_ValidRule_PutsThePrintedTextOnTheClipboard_Test()
    {
        (ViewController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");

        bool copied = controller.Copy();

        Assert.True(copied);
        Assert.Equal(controller.Output.Text, services.Clipboard.Text);
        Assert.StartsWith("Copied the Tree view", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A view that cannot print copies nothing and shows the reason.</summary>
    [Fact]
    public void Copy_ViewThatCannotPrint_CopiesNothingAndSaysWhy_Test()
    {
        (ViewController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        controller.Settings.Kind = ViewKind.RuleText;
        controller.Settings.OperatorStyle = OperatorStyle.CStyle;

        bool copied = controller.Copy();

        Assert.False(copied);
        Assert.Null(services.Clipboard.Text);
        Assert.StartsWith("Nothing to copy: ", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>When the clipboard is not available, copy says so and points to the file.</summary>
    [Fact]
    public void Copy_ClipboardNotAvailable_SaysSo_Test()
    {
        (ViewController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        services.Clipboard.Available = false;

        bool copied = controller.Copy();

        Assert.False(copied);
        Assert.Contains("clipboard is not available", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Save writes the printed text to the chosen file with line feeds and a final newline.</summary>
    [Fact]
    public void Save_ValidRule_WritesThePrintedTextToTheFile_Test()
    {
        (ViewController controller, _, Workbench workbench) = Create("a AND b");
        controller.Settings.Kind = ViewKind.Json;
        string path = Path.Combine(Path.GetTempPath(), $"view-{Guid.NewGuid():N}.json");
        ((ScriptedPrompts)workbench.Prompts).SavePath = path;
        try
        {
            bool saved = controller.Save();

            Assert.True(saved);
            Assert.Equal(OutputWriter.Normalize(controller.Output.Text!), File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A cancelled save dialog writes no file.</summary>
    [Fact]
    public void Save_DialogCancelled_WritesNothing_Test()
    {
        (ViewController controller, _, Workbench workbench) = Create("a AND b");
        ((ScriptedPrompts)workbench.Prompts).SavePath = null;

        Assert.False(controller.Save());
    }

    /// <summary>The dialect key walks the three LaTeX envelopes before it moves to the next dialect.</summary>
    [Fact]
    public void CycleDialect_FromLatex_WalksTheEnvelopesThenTheNextDialect_Test()
    {
        (ViewController controller, _, _) = Create("a AND b");
        controller.Settings.Kind = ViewKind.Equation;
        controller.Settings.Dialect = EquationDialect.LaTeX;

        controller.CycleDialect();
        Assert.Equal(
            (EquationDialect.LaTeX, LatexWrapMode.DoubleDollar),
            (controller.Settings.Dialect, controller.Settings.LatexWrap)
        );
        controller.CycleDialect();
        Assert.Equal(
            (EquationDialect.LaTeX, LatexWrapMode.MathJaxSafe),
            (controller.Settings.Dialect, controller.Settings.LatexWrap)
        );
        controller.CycleDialect();

        Assert.NotEqual(EquationDialect.LaTeX, controller.Settings.Dialect);
        Assert.Equal(LatexWrapMode.None, controller.Settings.LatexWrap);
    }

    /// <summary>The constant overrides come from the dialogs, and an empty answer clears one.</summary>
    [Fact]
    public void EditConstantOverrides_Answers_SetAndClearTheTexts_Test()
    {
        (ViewController controller, ScriptedServices services, _) = Create("a OR True");
        controller.Settings.Kind = ViewKind.RuleText;
        foreach (string? answer in new[] { "TRUE", string.Empty, string.Empty })
        {
            services.Dialogs.Texts.Enqueue(answer);
        }

        controller.EditConstantOverrides();

        Assert.Equal("TRUE", controller.Settings.TrueText);
        Assert.Null(controller.Settings.FalseText);
        Assert.Equal("a OR TRUE", controller.Output.Text);
    }

    /// <summary>A cancelled override dialog changes nothing.</summary>
    [Fact]
    public void EditConstantOverrides_Cancelled_ChangesNothing_Test()
    {
        (ViewController controller, _, _) = Create("a OR True");

        controller.EditConstantOverrides();

        Assert.False(controller.Settings.HasConstantFormat);
    }

    /// <summary>The header names only the toggles that apply to the form on show.</summary>
    [Fact]
    public void Header_JsonView_HasNoConstantOrOperatorToggles_Test()
    {
        (ViewController controller, _, _) = Create("a AND b");
        controller.Settings.Kind = ViewKind.Json;

        Assert.DoesNotContain("constants:", controller.Header, StringComparison.Ordinal);
        Assert.DoesNotContain("operators:", controller.Header, StringComparison.Ordinal);
    }

    private static (ViewController Controller, ScriptedServices Services, Workbench Workbench) Create(string text)
    {
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        ScriptedServices services = new();
        return (new ViewController(workbench, services.Services), services, workbench);
    }
}
