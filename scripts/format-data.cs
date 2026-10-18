#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs
#:include shared/ConsoleProcess.cs

// Formats the JSON, YAML and YML files with Prettier, and checks that test fixtures parse.
//
// Files outside the test data folders are formatted (or, with --check-only, only checked) by Prettier. The settings
// are in .prettierrc.json and the skipped paths are in .prettierignore. Test fixtures feed tests that read exact text
// and line/column spans, so Prettier does not reformat them. They only have to parse: a syntax error is a failure.
// Needs Node.js. The Prettier version is pinned below so every machine and CI formats the same way.
//
// Usage: dotnet run scripts/format-data.cs -- [--check-only] [path ...]
//   --check-only  Rewrite nothing: exit with code 1 when a file needs formatting.
//   path          Limit the run to some files (the pre-commit hook passes the staged files). Paths are relative to
//                 the repository root.
using System.ComponentModel;
using System.Text.RegularExpressions;
using TruthWeaver.Scripts;

try
{
    bool checkOnly = false;
    List<string> paths = [];
    foreach (string argument in args)
    {
        if (argument == "--check-only")
        {
            checkOnly = true;
        }
        else if (argument.StartsWith("--", StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync($"format-data: unknown option {argument}");
            return 1;
        }
        else
        {
            paths.Add(argument);
        }
    }

    // Paths are relative to the repository root, wherever the script starts.
    string root = await Repo.FindRootAsync();
    Directory.SetCurrentDirectory(root);

    // Prettier runs through npx, so Node.js has to be installed. Fail early with a clear message.
    try
    {
        await Repo.RunAsync("node", "--version");
    }
    catch (Exception exception) when (exception is ScriptFailedException or Win32Exception)
    {
        await Console.Error.WriteLineAsync(
            "format-data: Node.js is required (it provides npx, which runs Prettier). Install Node.js and make sure 'node' is on PATH."
        );
        return 1;
    }

    const string Prettier = "prettier@3.9.7";

    // Same data folders that normalize-text skips.
    Regex fixtureFolders = new("(^|/)tests/.*/(Fixtures|Samples|RuleTests)/", RegexOptions.None, TimeSpan.FromSeconds(5));

    // --no-install is not used: npx downloads the pinned version on first use and caches it.
    static Task<int> InvokePrettierAsync(IEnumerable<string> prettierArguments)
    {
        return ConsoleProcess.RunAsync("npx", ["--yes", Prettier, .. prettierArguments]);
    }

    List<string> fixtures;
    List<string> formatted;
    if (paths.Count > 0)
    {
        string[] dataExtensions = [".json", ".yaml", ".yml"];
        List<string> data =
        [
            .. paths
                .Where(path => dataExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .Select(path => path.Replace('\\', '/')),
        ];
        fixtures = [.. data.Where(path => fixtureFolders.IsMatch(path))];
        formatted = [.. data.Where(path => !fixtureFolders.IsMatch(path))];
    }
    else
    {
        fixtures = ["tests/**/{Fixtures,Samples,RuleTests}/**/*.{json,yaml,yml}"];
        formatted = ["**/*.{json,yaml,yml}"];
    }

    bool failed = false;

    if (formatted.Count > 0)
    {
        string mode = checkOnly ? "--check" : "--write";
        await Console.Out.WriteLineAsync($"==> prettier {mode}");
        if (await InvokePrettierAsync([mode, "--log-level", "warn", .. formatted]) != 0)
        {
            failed = true;
        }
    }

    if (fixtures.Count > 0)
    {
        // An empty ignore file turns off .prettierignore, which lists the fixture folders. Exit code 1 means "would be
        // reformatted", which is fine for a fixture; only exit code 2 (a parse error) fails.
        string emptyIgnore = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await File.WriteAllTextAsync(emptyIgnore, string.Empty);
            await Console.Out.WriteLineAsync("==> prettier syntax check of test fixtures");
            int code = await InvokePrettierAsync([
                "--check",
                "--log-level",
                "error",
                "--ignore-path",
                emptyIgnore,
                .. fixtures,
            ]);
            if (code == 2)
            {
                failed = true;
            }
        }
        finally
        {
            File.Delete(emptyIgnore);
        }
    }

    if (failed)
    {
        await Console.Error.WriteLineAsync(
            "format-data: JSON/YAML check failed. Run scripts/format-data.cs to reformat; a syntax error needs a manual fix."
        );
        return 1;
    }

    await Console.Out.WriteLineAsync("format-data: JSON and YAML are formatted.");
    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}
