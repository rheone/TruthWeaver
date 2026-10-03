namespace TruthWeaver.Architecture.Tests;

using System.Diagnostics;

/// <summary>
/// Exercises <c>scripts/check-line-endings.ps1</c>, the pre-commit guard that fails when a text file
/// has LF line endings in a repository whose <c>.gitattributes</c> declares <c>eol=crlf</c>.
/// </summary>
public sealed class LineEndingGuardTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("line-ending-guard-").FullName;

    /// <summary>Removes the temporary files written by each test.</summary>
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    /// <summary>A file written with LF line endings must make the guard exit non-zero and name the file.</summary>
    [Fact]
    public async Task Check_FileWithLfLineEndings_FailsAndNamesTheFile_Test()
    {
        string file = this.WriteFile("lf.cs", "line one\nline two\n");

        (int exitCode, string output) = await RunGuardAsync(file);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("lf.cs", output, StringComparison.Ordinal);
    }

    /// <summary>A file with CRLF line endings throughout passes the guard.</summary>
    [Fact]
    public async Task Check_FileWithCrlfLineEndings_Passes_Test()
    {
        string file = this.WriteFile("crlf.cs", "line one\r\nline two\r\n");

        (int exitCode, string _) = await RunGuardAsync(file);

        Assert.Equal(0, exitCode);
    }

    /// <summary>A file mixing CRLF and bare LF endings fails, since a scripted edit may only repair part of a file.</summary>
    [Fact]
    public async Task Check_FileWithMixedLineEndings_Fails_Test()
    {
        string file = this.WriteFile("mixed.cs", "line one\r\nline two\nline three\r\n");

        (int exitCode, string _) = await RunGuardAsync(file);

        Assert.NotEqual(0, exitCode);
    }

    /// <summary>A file with no line breaks at all has nothing to get wrong and passes.</summary>
    [Fact]
    public async Task Check_FileWithNoLineBreaks_Passes_Test()
    {
        string file = this.WriteFile("single.cs", "one line");

        (int exitCode, string _) = await RunGuardAsync(file);

        Assert.Equal(0, exitCode);
    }

    private static async Task<(int ExitCode, string Output)> RunGuardAsync(params string[] files)
    {
        string script = Path.Combine(FindRepositoryRoot(), "scripts", "check-line-endings.ps1");
        ProcessStartInfo startInfo = new("pwsh") { RedirectStandardOutput = true, RedirectStandardError = true };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(script);
        foreach (string file in files)
        {
            startInfo.ArgumentList.Add(file);
        }

        using Process process = Process.Start(startInfo)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdout + await stderr);
    }

    // Walks up from the test binary to the directory holding the solution file.
    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TruthWeaver.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("TruthWeaver.slnx was not found above the test binary.");
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(this.directory, name);
        File.WriteAllText(path, content);
        return path;
    }
}
