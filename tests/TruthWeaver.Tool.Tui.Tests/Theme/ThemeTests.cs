namespace TruthWeaver.Tool.Tui.Tests.Theme;

using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;
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

    /// <summary>
    /// Every text color of both palettes keeps the contrast the page promises against each background of its set: the
    /// panes, the dialogs and the selection.
    /// </summary>
    /// <param name="theme">The name of the theme.</param>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Palette_EveryColor_KeepsTheMinimumContrastOnEveryBackground_Test(string theme)
    {
        ThemePalette palette = ThemePalette.For(Enum.Parse<ThemeVariant>(theme), noColor: false);
        Color[] colors =
        [
            palette.Text,
            palette.True,
            palette.False,
            palette.Unknown,
            palette.Skipped,
            palette.Error,
            palette.Warning,
            palette.Info,
            palette.Predicate,
            palette.Operator,
            palette.Variable,
            palette.Constant,
            palette.Description,
        ];
        Color[] backgrounds = [palette.Background, palette.Surface, palette.Selection];

        Assert.All(
            backgrounds,
            background =>
                Assert.All(
                    colors,
                    color => Assert.True(ThemePalette.ContrastRatio(color, background) >= ThemePalette.MinimumContrast)
                )
        );
    }

    /// <summary>The light set paints light backgrounds with dark text, and the dark set paints dark backgrounds with light text.</summary>
    /// <param name="theme">The name of the theme.</param>
    /// <param name="light">Whether the backgrounds of the set are light.</param>
    [Theory]
    [InlineData("Light", true)]
    [InlineData("Dark", false)]
    public void Palette_Backgrounds_MatchTheSet_Test(string theme, bool light)
    {
        ThemePalette palette = ThemePalette.For(Enum.Parse<ThemeVariant>(theme), noColor: false);
        Color black = new(0, 0, 0);
        Color white = new(255, 255, 255);

        Assert.All(
            [palette.Background, palette.Surface, palette.Selection],
            background =>
                Assert.Equal(
                    light,
                    ThemePalette.ContrastRatio(background, black) > ThemePalette.ContrastRatio(background, white)
                )
        );
        Assert.Equal(light, ThemePalette.ContrastRatio(palette.Text, white) > ThemePalette.ContrastRatio(palette.Text, black));
    }

    /// <summary>
    /// Applying a set replaces every Terminal.Gui scheme with one that paints the background of the set, so no pane or
    /// dialog takes the background of the terminal.
    /// </summary>
    /// <param name="theme">The name of the theme.</param>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Apply_Theme_PaintsTheBackgroundOfTheSetInEveryScheme_Test(string theme)
    {
        ThemeVariant variant = Enum.Parse<ThemeVariant>(theme);
        ThemePalette palette = ThemePalette.For(variant, noColor: false);

        WorkbenchAppearance.Apply(variant, noColor: false);
        IReadOnlyCollection<string> names = [.. SchemeManager.GetSchemeNames()];
        Dictionary<string, Scheme> schemes = names.ToDictionary(name => name, SchemeManager.GetScheme);
        WorkbenchAppearance.Apply(ThemeVariant.Dark, noColor: false);

        Assert.Contains("Base", names);
        Assert.Contains("Dialog", names);
        Assert.Equal(palette.Background, schemes["Base"].Normal.Background);
        Assert.Equal(palette.Text, schemes["Base"].Normal.Foreground);
        Assert.Equal(palette.Surface, schemes["Dialog"].Normal.Background);
        Assert.All(
            schemes.Values,
            scheme =>
            {
                Assert.Contains(scheme.Normal.Background, new[] { palette.Background, palette.Surface });
                Assert.Equal(palette.Selection, scheme.Focus.Background);
                Assert.NotEqual(Color.None, scheme.Normal.Foreground);
            }
        );
    }

    /// <summary>With NO_COLOR, applying either set leaves every Terminal.Gui scheme with the terminal colors only.</summary>
    /// <param name="theme">The name of the theme.</param>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Apply_NoColor_LeavesEverySchemeMonochrome_Test(string theme)
    {
        WorkbenchAppearance.Apply(Enum.Parse<ThemeVariant>(theme), noColor: true);
        Scheme[] schemes = [.. SchemeManager.GetSchemeNames().Select(SchemeManager.GetScheme)];

        // The schemes are process-wide; the other tests in the collection expect the default dark set.
        WorkbenchAppearance.Apply(ThemeVariant.Dark, noColor: false);

        Assert.NotEmpty(schemes);
        Assert.All(schemes, scheme => Assert.Equal(WorkbenchAppearance.MonochromeScheme(), scheme));
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

    /// <summary>
    /// After Ctrl+F1 every view of the window draws on a background of the light set, and after the second Ctrl+F1 on a
    /// background of the dark set; none takes the background of the terminal. A text field shows the selection background.
    /// </summary>
    [Fact]
    public void CtrlF1_Theme_EveryViewDrawsOnTheBackgroundOfTheSet_Test()
    {
        using WorkbenchHarness harness = new("a AND b");

        harness.Press(Key.F1.WithCtrl);
        Attribute[] light = [.. Descendants(harness.Window).Select(view => view.GetAttributeForRole(VisualRole.Normal))];
        harness.Press(Key.F1.WithCtrl);
        Attribute[] dark = [.. Descendants(harness.Window).Select(view => view.GetAttributeForRole(VisualRole.Normal))];

        Assert.All(light, role => Assert.Contains(role.Background, Backgrounds(ThemePalette.Light)));
        Assert.All(dark, role => Assert.Contains(role.Background, Backgrounds(ThemePalette.Dark)));
        Assert.Equal(ThemePalette.Light.Background, light[0].Background);
        Assert.Equal(ThemePalette.Light.Text, light[0].Foreground);
    }

    /// <summary>The key help lists the key that switches the theme.</summary>
    [Fact]
    public void HelpText_DefaultCommands_ListsTheThemeKey_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        Assert.Contains("Ctrl+F1", help, StringComparison.Ordinal);
        Assert.Contains("theme", help, StringComparison.Ordinal);
    }

    /// <summary>The four syntax kinds and the description text each have a color of their own in both sets.</summary>
    /// <param name="theme">The name of the theme.</param>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Palette_EverySyntaxKind_HasItsOwnColor_Test(string theme)
    {
        ThemePalette palette = ThemePalette.For(Enum.Parse<ThemeVariant>(theme), noColor: false);

        Color[] colors = [.. SyntaxKinds.All.Select(palette.Of)];

        Assert.Equal(5, colors.Distinct().Count());
        Assert.DoesNotContain(palette.Text, colors);
        Assert.All(colors, color => Assert.NotEqual(Color.None, color));
    }

    /// <summary>With NO_COLOR the five kinds have no color and differ by weight; with color the style stays plain.</summary>
    [Fact]
    public void StyleOf_NoColorSyntaxKind_DiffersForEachKind_Test()
    {
        ThemePalette palette = ThemePalette.Plain;

        TextStyle[] styles = [.. SyntaxKinds.All.Select(palette.StyleOf)];

        Assert.Equal(5, styles.Distinct().Count());
        Assert.DoesNotContain(TextStyle.None, styles);
        Assert.All(SyntaxKinds.All, kind => Assert.Equal(Color.None, palette.Of(kind)));
        Assert.All(SyntaxKinds.All, kind => Assert.Equal(TextStyle.None, ThemePalette.Dark.StyleOf(kind)));
    }

    /// <summary>The key help writes the name of every syntax kind, so the colors are never the only mark.</summary>
    [Fact]
    public void HelpText_DefaultCommands_NamesEverySyntaxKind_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        Assert.Contains(SyntaxKinds.LegendText(), help, StringComparison.Ordinal);
        Assert.Contains("NO_COLOR", help, StringComparison.Ordinal);
    }

    /// <summary>The legend names each kind in the scheme of that kind, with a style of its own when there is no color.</summary>
    /// <param name="noColor">Whether the palette has no color.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntaxLegend_EveryKind_IsNamedInItsOwnScheme_Test(bool noColor)
    {
        ThemePalette palette = ThemePalette.For(ThemeVariant.Dark, noColor);
        using SyntaxLegend legend = new(palette);

        View[] labels = [.. legend.SubViews];

        Assert.Equal(SyntaxKinds.All.Select(SyntaxKinds.NameOf), labels.Select(l => l.Text));
        Assert.Equal(
            SyntaxKinds.All.Select(k => SyntaxLegend.SchemeOf(k, palette).Normal),
            labels.Select(l => l.GetAttributeForRole(VisualRole.Normal))
        );
    }

    private static Color[] Backgrounds(ThemePalette palette)
    {
        return [palette.Background, palette.Surface, palette.Selection];
    }

    // The view itself first, then every view inside it, depth first.
    private static IEnumerable<View> Descendants(View view)
    {
        yield return view;
        foreach (View child in view.SubViews)
        {
            foreach (View descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
