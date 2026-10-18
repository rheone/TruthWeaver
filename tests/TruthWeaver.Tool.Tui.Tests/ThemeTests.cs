namespace TruthWeaver.Tool.Tui.Tests;

using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using Attribute = Terminal.Gui.Drawing.Attribute;

/// <summary>
/// The value marks, <c>NO_COLOR</c> and the light and dark themes of the workbench. The tests that build a window share
/// the process-wide Terminal.Gui theme, so they run in the Terminal.Gui collection.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class ThemeTests
{
    private static readonly TruthValue?[] EveryMark = [TruthValue.True, TruthValue.False, TruthValue.Unknown, null];

    /// <summary>A trace line starts with the text mark of its value, so no value depends on its color.</summary>
    /// <param name="theme">The name of the theme whose palette colors the value.</param>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void TraceLineText_EveryValueWithAColor_StartsWithItsTextMark_Test(string theme)
    {
        ThemePalette palette = ThemePalette.For(Enum.Parse<ThemeVariant>(theme), noColor: false);

        foreach (TruthValue? value in EveryMark)
        {
            string text = AssignmentText.TraceLineText(new TraceLine("node", value, "reason", []));

            Assert.NotEqual(Color.None, palette.Of(value));
            Assert.StartsWith(TruthColors.Mark(value) + "  ", text, StringComparison.Ordinal);
        }
    }

    /// <summary>The text marks tell the four states apart, and a term in the list shows T, F or U.</summary>
    [Fact]
    public void Mark_EveryValue_IsDistinctAndTermLinesShowTheLetter_Test()
    {
        TruthValue[] values = [TruthValue.True, TruthValue.False, TruthValue.Unknown];

        Assert.Equal(4, EveryMark.Select(TruthColors.Mark).Distinct().Count());
        Assert.Equal(4, EveryMark.Select(TruthColors.Letter).Distinct().Count());
        Assert.Equal(
            ["[T] a", "[F] a", "[U] a"],
            values.Select(v => AssignmentText.TermLine(new TermAssignment("a", "a", v, false)))
        );
    }

    /// <summary>With NO_COLOR the palette has no color, the rows keep the terminal defaults, and every value still has its mark.</summary>
    [Fact]
    public void For_NoColor_HasNoColorAndEveryValueKeepsItsMark_Test()
    {
        ThemePalette palette = ThemePalette.For(ThemeVariant.Light, noColor: true);

        Assert.False(palette.HasColor);
        foreach (TruthValue? value in EveryMark)
        {
            Scheme scheme = TruthColors.SchemeOf(value, palette, new Color(10, 20, 30));

            Assert.Equal(Color.None, palette.Of(value));
            Assert.Equal(Color.None, scheme.Normal.Foreground);
            Assert.Equal(Color.None, scheme.Normal.Background);
            Assert.Equal(TextStyle.Reverse, scheme.Focus.Style);
            Assert.False(string.IsNullOrEmpty(TruthColors.Mark(value)));
        }
    }

    /// <summary>Without color the editor still tells an error, a warning and information apart by style.</summary>
    [Fact]
    public void StyleOf_NoColor_DiffersForEachSeverity_Test()
    {
        ThemePalette palette = ThemePalette.Plain;

        TextStyle[] styles =
        [
            palette.StyleOf(DiagnosticSeverity.Error),
            palette.StyleOf(DiagnosticSeverity.Warning),
            palette.StyleOf(DiagnosticSeverity.Info),
        ];

        Assert.Equal(3, styles.Distinct().Count());
        Assert.All(styles, style => Assert.True(style.HasFlag(TextStyle.Underline)));
    }

    /// <summary>The monochrome scheme that NO_COLOR applies has no color in any role.</summary>
    [Fact]
    public void MonochromeScheme_EveryRole_HasNoColor_Test()
    {
        Scheme scheme = WorkbenchAppearance.MonochromeScheme();

        Attribute[] roles =
        [
            scheme.Normal,
            scheme.HotNormal,
            scheme.Focus,
            scheme.HotFocus,
            scheme.Active,
            scheme.HotActive,
            scheme.Highlight,
            scheme.Editable,
            scheme.ReadOnly,
            scheme.Disabled,
        ];
        Assert.All(
            roles,
            role =>
            {
                Assert.Equal(Color.None, role.Foreground);
                Assert.Equal(Color.None, role.Background);
            }
        );
    }

    /// <summary>NO_COLOR counts only when it is set and not empty.</summary>
    /// <param name="value">The value of the variable.</param>
    /// <param name="expected">Whether no color is requested.</param>
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1", true)]
    [InlineData("anything", true)]
    public void NoColorRequested_Variable_FollowsTheConvention_Test(string? value, bool expected)
    {
        bool result = ColorMode.NoColorRequested(name => name == "NO_COLOR" ? value : null);

        Assert.Equal(expected, result);
    }

    /// <summary>Every color of both palettes keeps the contrast the page promises against the reference background.</summary>
    /// <param name="theme">The name of the theme.</param>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Palette_EveryColor_KeepsTheMinimumContrast_Test(string theme)
    {
        ThemePalette palette = ThemePalette.For(Enum.Parse<ThemeVariant>(theme), noColor: false);
        Color[] colors =
        [
            palette.True,
            palette.False,
            palette.Unknown,
            palette.Skipped,
            palette.Error,
            palette.Warning,
            palette.Info,
        ];

        Assert.All(
            colors,
            color => Assert.True(ThemePalette.ContrastRatio(color, palette.Background) >= ThemePalette.MinimumContrast)
        );
    }

    /// <summary>The setting picks the color set, and auto asks the terminal and falls back to dark when it cannot tell.</summary>
    /// <param name="setting">The setting.</param>
    /// <param name="detected">The name of the theme the terminal reports, or <see langword="null"/> for no answer.</param>
    /// <param name="expected">The name of the theme in use.</param>
    [Theory]
    [InlineData(ToolTheme.Light, "Dark", "Light")]
    [InlineData(ToolTheme.Dark, "Light", "Dark")]
    [InlineData(ToolTheme.Auto, "Light", "Light")]
    [InlineData(ToolTheme.Auto, null, "Dark")]
    public void Resolve_Setting_PicksTheColorSet_Test(ToolTheme setting, string? detected, string expected)
    {
        ThemeVariant result = ThemeChoice.Resolve(setting, () => detected is null ? null : Enum.Parse<ThemeVariant>(detected));

        Assert.Equal(expected, result.ToString());
    }

    /// <summary>A fixed setting never asks the terminal.</summary>
    [Fact]
    public void Resolve_FixedSetting_DoesNotAskTheTerminal_Test()
    {
        bool asked = false;

        _ = ThemeChoice.Resolve(
            ToolTheme.Light,
            () =>
            {
                asked = true;
                return null;
            }
        );

        Assert.False(asked);
    }

    /// <summary>The reply of a terminal to the background query gives a light or a dark theme, and nonsense gives none.</summary>
    /// <param name="reply">The reply.</param>
    /// <param name="expected">The name of the theme, or <see langword="null"/>.</param>
    [Theory]
    [InlineData("\u001b]11;rgb:ffff/ffff/ffff\u001b\\", "Light")]
    [InlineData("\u001b]11;rgb:0000/0000/0000\a", "Dark")]
    [InlineData("\u001b]11;rgb:1e/1e/2e\a", "Dark")]
    [InlineData("\u001b]11;rgb:f/f/f\a", "Light")]
    [InlineData("", null)]
    [InlineData("\u001b]11;rgb:zz/zz/zz\a", null)]
    [InlineData("\u001b]11;rgb:ff/ff\a", null)]
    public void Parse_Reply_GivesTheTheme_Test(string reply, string? expected)
    {
        Assert.Equal(expected, TerminalBackground.Parse(reply)?.ToString());
    }

    /// <summary>The workbench starts with the theme of the setting, and a theme passed in wins over the setting.</summary>
    [Fact]
    public void Workbench_Theme_StartsFromTheSettingOrTheArgument_Test()
    {
        ToolSettings light = ToolSettings.Default with { Theme = ToolTheme.Light };
        ScriptedPrompts prompts = new();

        Workbench fromSetting = new(RuleDocument.Create(string.Empty), light, prompts);
        Workbench fromAuto = new(RuleDocument.Create(string.Empty), ToolSettings.Default, prompts);
        Workbench fromArgument = new(RuleDocument.Create(string.Empty), light, prompts, ThemeVariant.Dark);

        Assert.Equal(ThemeVariant.Light, fromSetting.Theme);
        Assert.Equal(ThemeVariant.Dark, fromAuto.Theme);
        Assert.Equal(ThemeVariant.Dark, fromArgument.Theme);
    }

    /// <summary>Ctrl+F1 switches the theme at once: the palette, the Terminal.Gui theme (panes and dialogs) and the status line follow.</summary>
    [Fact]
    public void CtrlF1_Theme_SwitchesPaletteAndLibraryTheme_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        ThemeVariant start = harness.Workbench.Theme;
        int changes = 0;
        harness.Workbench.ThemeChanged += (_, _) => changes++;

        harness.Press(Key.F1.WithCtrl);
        ThemeVariant switched = harness.Workbench.Theme;
        string switchedLibrary = ThemeManager.GetCurrentThemeName();
        string status = harness.Window.StatusText;
        harness.Press(Key.F1.WithCtrl);

        Assert.Equal(ThemeVariant.Dark, start);
        Assert.Equal(ThemeVariant.Light, switched);
        Assert.Equal("Light", switchedLibrary);
        Assert.Contains("Theme: light", status, StringComparison.Ordinal);
        Assert.Equal(ThemeVariant.Dark, harness.Workbench.Theme);
        Assert.Equal("Dark", ThemeManager.GetCurrentThemeName());
        Assert.Equal(2, changes);
        Assert.Same(ThemePalette.Dark, harness.Workbench.Palette);
    }

    /// <summary>The key help lists the key that switches the theme.</summary>
    [Fact]
    public void HelpText_DefaultCommands_ListsTheThemeKey_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        Assert.Contains("Ctrl+F1", help, StringComparison.Ordinal);
        Assert.Contains("theme", help, StringComparison.Ordinal);
    }
}
