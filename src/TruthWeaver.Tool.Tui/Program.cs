namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.App;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Tui.Hosting;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The entry point of the <c>truthweaver-tui</c> command.</summary>
internal static class Program
{
    /// <summary>Opens the workbench, or prints the help or a problem.</summary>
    /// <param name="args">The command-line arguments: at most one rule file.</param>
    /// <returns>The exit code: 0 success, 2 when the arguments, the configuration or the file cannot be used.</returns>
    public static int Main(string[] args)
    {
        TuiStartupResult startup = TuiStartup.Prepare(
            args,
            Environment.CurrentDirectory,
            Console.IsInputRedirected ? Console.In : null
        );
        if (startup.Document is not { } document)
        {
            TextWriter writer = startup.ExitCode == ToolExitCode.Success ? Console.Out : Console.Error;
            writer.WriteLine(startup.Message);
            return (int)startup.ExitCode;
        }

        // The terminal is asked for its background before the terminal library takes over the input.
        bool noColor = ColorMode.NoColorRequested();

        // The theme that the author switched to in an earlier run wins over the setting; the setting applies otherwise.
        WorkbenchPreferences preferences = WorkbenchPreferences.Load(WorkbenchPreferences.DefaultPath);
        ThemeVariant theme =
            preferences.Theme
            ?? ThemeChoice.Resolve(
                startup.Settings.Theme,
                () => noColor ? null : TerminalBackground.Detect(TimeSpan.FromMilliseconds(200))
            );
        using IApplication app = Application.Create();
        app.Init();
        WorkbenchServices.Current = new WorkbenchServices(new TerminalDialogs(app), new TerminalClipboard(app));
        Workbench.Workbench workbench = new(document, startup.Settings, new TerminalPrompts(app), theme, noColor)
        {
            DiagnosticsDelay = Workbench.Workbench.DefaultDiagnosticsDelay,
            Post = app.Invoke,
            Preferences = preferences,
        };
        workbench.Layout.Restore(preferences.EditorWidthPercent, preferences.TopHeightPercent);
        using WorkbenchWindow window = new(workbench);
        window.FocusRegion(PaneSlot.Editor);
        app.Run(window);
        return 0;
    }
}
