namespace TruthWeaver.Tool;

using System.CommandLine;
using System.Text.Json.Nodes;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>
/// The <c>--output</c> and <c>--check</c> options of a command that makes one text, and the delivery of that text: to
/// standard output, to a file, or compared with a file. Every output goes through <see cref="OutputWriter"/>, so it has LF
/// line endings and one final newline on every platform.
/// </summary>
internal sealed class OutputTarget
{
    /// <summary>Gets the output file option.</summary>
    public Option<string?> Output { get; } =
        new("--output", "-o") { Description = "Write the output to this file instead of standard output.", HelpName = "file" };

    /// <summary>Gets the check option.</summary>
    public Option<bool> Check { get; } =
        new("--check")
        {
            Description =
                "Do not write: compare the output with the --output file, and fail (exit code 1) when it differs or is missing.",
        };

    /// <summary>Records the result of a check: the JSON members, the text line, and a failure when the file does not match.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="check">The check.</param>
    public static void Report(CommandOutcome outcome, OutputCheck check)
    {
        outcome.Result["check"] = CheckJson(check);
        outcome.Text = (outcome.Text is null ? string.Empty : outcome.Text + "\n") + CheckLine(check);
        outcome.Failed |= !check.IsIdentical;
    }

    /// <summary>Gives the JSON object of a check: the path, the status and the first different line.</summary>
    /// <param name="check">The check.</param>
    /// <returns>The object.</returns>
    public static JsonObject CheckJson(OutputCheck check)
    {
        return new JsonObject
        {
            ["path"] = check.Path,
            ["status"] = check.Status.ToString().ToLowerInvariant(),
            ["firstDifferentLine"] = check.FirstDifferentLine,
        };
    }

    /// <summary>Compares two texts after <see cref="OutputWriter.Normalize"/>, for a check that has no file to compare with.</summary>
    /// <param name="origin">The name to report.</param>
    /// <param name="actual">The text that exists.</param>
    /// <param name="expected">The output text.</param>
    /// <returns>The comparison.</returns>
    public static OutputCheck CompareText(string origin, string actual, string expected)
    {
        string[] actualLines = OutputWriter.Normalize(actual).Split('\n');
        string[] expectedLines = OutputWriter.Normalize(expected).Split('\n');
        int common = Math.Min(actualLines.Length, expectedLines.Length);
        for (int i = 0; i < common; i++)
        {
            if (!string.Equals(actualLines[i], expectedLines[i], StringComparison.Ordinal))
            {
                return new OutputCheck(origin, OutputCheckStatus.Differs, i + 1);
            }
        }

        return actualLines.Length == expectedLines.Length
            ? new OutputCheck(origin, OutputCheckStatus.Identical, null)
            : new OutputCheck(origin, OutputCheckStatus.Differs, common + 1);
    }

    /// <summary>Gives the text line of a check: the path and identical, missing, or the first line that differs.</summary>
    /// <param name="check">The check.</param>
    /// <returns>The line.</returns>
    public static string CheckLine(OutputCheck check)
    {
        return check.Status switch
        {
            OutputCheckStatus.Identical => $"{check.Path}: identical",
            OutputCheckStatus.Missing => $"{check.Path}: missing",
            _ => string.Create(CultureInfo.InvariantCulture, $"{check.Path}: differs at line {check.FirstDifferentLine}"),
        };
    }

    /// <summary>Adds the options to a command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="checkNeedsOutput"><see langword="true"/> when <c>--check</c> needs <c>--output</c> to compare with.</param>
    public void AddTo(Command command, bool checkNeedsOutput = true)
    {
        command.Add(this.Output);
        command.Add(this.Check);
        if (checkNeedsOutput)
        {
            command.Validators.Add(result =>
            {
                if (result.GetValue(this.Check) && result.GetValue(this.Output) is null)
                {
                    result.AddError("--check needs --output: the file to compare the output with.");
                }
            });
        }
    }

    /// <summary>
    /// Delivers a text: compares it with the <c>--output</c> file for <c>--check</c>, writes it to the file, or puts it on
    /// standard output.
    /// </summary>
    /// <param name="parse">The parse result.</param>
    /// <param name="console">The console.</param>
    /// <param name="outcome">The outcome that takes the text, the result members and the diagnostics.</param>
    /// <param name="text">The output text.</param>
    public void Deliver(ParseResult parse, CliConsole console, CommandOutcome outcome, string text)
    {
        string? output = parse.GetValue(this.Output) is { } path ? console.FullPath(path) : null;
        if (output is null)
        {
            outcome.Text = text;
            outcome.Result["text"] = OutputWriter.Normalize(text);
            return;
        }

        outcome.Result["output"] = output;
        if (parse.GetValue(this.Check))
        {
            ToolResult<OutputCheck> check = OutputWriter.Check(output, text);
            outcome.Add(output, check.Diagnostics);
            if (check.Value is { } value)
            {
                Report(outcome, value);
            }

            return;
        }

        ToolResult<string> written = OutputWriter.WriteFile(output, text);
        outcome.Add(output, written.Diagnostics);
    }
}
