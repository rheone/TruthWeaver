#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs
#:include shared/ConsoleProcess.cs

// Formats the whole repository, then runs the same checks as the CI build job.
//
// Run this before you push. It applies normalize-text (ASCII dashes and ellipses, LF, no BOM, no trailing
// whitespace) and format-data (Prettier for JSON, YAML and YML), then CSharpier, then dotnet format, then CSharpier
// again (dotnet format can move a token that CSharpier then moves back). It then checks normalize-text and
// format-data again and runs the CI gates: csharpier check, dotnet format --verify-no-changes --severity info, and
// Roslynator. A failure in the CSharpier and dotnet format gates after the formatters ran means they disagree about
// some code. Reshape that code. See CLAUDE.md, section Formatting. It stops at the first failing step.
//
// Usage: dotnet run scripts/format-all.cs -- [--check-only] [--files <path>...]
//   --check-only  Skip the apply steps and only run the checks.
//   --files       Run only the CSharpier and dotnet format checks on the listed C# files, in seconds, to catch an
//                 analyzer finding before the full run. Does not apply any formatting and does not run Roslynator.
using TruthWeaver.Scripts;

try
{
    bool checkOnly = false;
    bool filesMode = false;
    List<string> files = [];
    foreach (string argument in args)
    {
        if (argument == "--check-only")
        {
            checkOnly = true;
        }
        else if (argument == "--files")
        {
            filesMode = true;
        }
        else if (filesMode)
        {
            files.Add(argument);
        }
        else
        {
            await Console.Error.WriteLineAsync($"format-all: unknown argument {argument}");
            return 1;
        }
    }

    string root = await Repo.FindRootAsync();
    Directory.SetCurrentDirectory(root);

    if (filesMode)
    {
        if (files.Count == 0)
        {
            await Console.Error.WriteLineAsync("format-all: --files needs at least one path.");
            return 1;
        }

        await StepAsync("csharpier check (files)", "dotnet", ["csharpier", "check", .. files]);
        await StepAsync(
            "dotnet format check (files)",
            "dotnet",
            ["format", "TruthWeaver.slnx", "--no-restore", "--verify-no-changes", "--severity", "info", "--include", .. files]
        );
        await Console.Out.WriteLineAsync("format-all: the listed files pass.");
        return 0;
    }

    if (!checkOnly)
    {
        await StepAsync("normalize-text", "dotnet", "run", "scripts/normalize-text.cs");
        await StepAsync("format-data", "dotnet", "run", "scripts/format-data.cs");
        await StepAsync("csharpier format", "dotnet", "csharpier", "format", ".");
        await StepAsync("dotnet format", "dotnet", "format", "TruthWeaver.slnx", "--no-restore", "--severity", "info");
        await StepAsync("csharpier format (second pass)", "dotnet", "csharpier", "format", ".");
    }

    await StepAsync("normalize-text check", "dotnet", "run", "scripts/normalize-text.cs", "--", "--check-only");
    await StepAsync("format-data check", "dotnet", "run", "scripts/format-data.cs", "--", "--check-only");

    // These three commands are the CI gates in .github/workflows/ci.yml. Keep them identical to it.
    await StepAsync("csharpier check", "dotnet", "csharpier", "check", ".");
    await StepAsync(
        "dotnet format check",
        "dotnet",
        "format",
        "TruthWeaver.slnx",
        "--no-restore",
        "--verify-no-changes",
        "--severity",
        "info"
    );
    await StepAsync("roslynator", "dotnet", "roslynator", "analyze", "TruthWeaver.slnx", "--ignore-compiler-diagnostics");

    await Console.Out.WriteLineAsync("format-all: all gates pass.");
    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}

// Runs one step with console output and fails the script, naming the step, on a non-zero exit code.
static async Task StepAsync(string description, string tool, params string[] arguments)
{
    await Console.Out.WriteLineAsync($"==> {description}");
    int exitCode = await ConsoleProcess.RunAsync(tool, arguments);
    if (exitCode != 0)
    {
        throw new ScriptFailedException($"format-all: {description} failed (exit code {exitCode}).");
    }
}
