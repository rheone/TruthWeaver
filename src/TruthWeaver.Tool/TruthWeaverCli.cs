namespace TruthWeaver.Tool;

using System.CommandLine;
using TruthWeaver.Tool.Commands;
using TruthWeaver.Tool.Core;

/// <summary>
/// The <c>truthweaver</c> command line: the root command, its subcommands, and one run over a console. The commands are
/// thin: each one parses its options, calls the tool core and writes a <see cref="CommandOutcome"/>.
/// </summary>
internal static class TruthWeaverCli
{
    /// <summary>Creates the root command with every subcommand.</summary>
    /// <param name="console">The console that the commands read and write.</param>
    /// <returns>The root command.</returns>
    public static RootCommand CreateRootCommand(CliConsole console)
    {
        return
        [
            with("Validate, convert, format, print, export, report, compare and evaluate TruthWeaver rules."),
            ValidateCommand.Create(console),
            ConvertCommand.Create(console),
            FormatCommand.Create(console),
            PrintCommand.Create(console),
            ExportCommand.Create(console),
            ReportCommand.Create(console),
            DiffCommand.Create(console),
            EvaluateCommand.Create(console),
        ];
    }

    /// <summary>
    /// Runs one command line. A usage error (an unknown option, a missing value, options that cannot go together) writes
    /// the errors to standard error and gives exit code 2, like any other input that the tool cannot use.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="console">The console.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(string[] args, CliConsole console, CancellationToken cancellationToken = default)
    {
        ParseResult parse = CreateRootCommand(console).Parse(args);
        if (parse.Errors.Count > 0)
        {
            foreach (System.CommandLine.Parsing.ParseError error in parse.Errors)
            {
                await console.Error.WriteAsync((error.Message + "\n").AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            await console
                .Error.WriteAsync(
                    "Run 'truthweaver --help' or 'truthweaver <command> --help' for the options.\n".AsMemory(),
                    cancellationToken
                )
                .ConfigureAwait(false);
            return (int)ToolExitCode.Unusable;
        }

        InvocationConfiguration invocation = new() { Output = console.Out, Error = console.Error };
        return await parse.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
    }
}
