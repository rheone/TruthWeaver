namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.App;

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
            TextWriter writer = startup.ExitCode == Core.ToolExitCode.Success ? Console.Out : Console.Error;
            writer.WriteLine(startup.Message);
            return (int)startup.ExitCode;
        }

        using IApplication app = Application.Create();
        app.Init();
        WorkbenchServices.Current = new WorkbenchServices(new TerminalDialogs(app), new TerminalClipboard(app));
        Workbench workbench = new(document, startup.Settings, new TerminalPrompts(app));
        using WorkbenchWindow window = new(workbench);
        window.FocusRegion(PaneSlot.Editor);
        app.Run(window);
        return 0;
    }
}
