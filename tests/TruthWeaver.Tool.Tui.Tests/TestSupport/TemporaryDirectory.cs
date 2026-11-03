namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>A folder with a fake <c>dot</c> shell script, used as the image search path.</summary>
internal sealed class TemporaryDirectory : IDisposable
{
    private TemporaryDirectory(string path)
    {
        this.FullPath = path;
    }

    /// <summary>Gets the folder.</summary>
    public string FullPath { get; }

    /// <summary>Makes a folder with a <c>dot</c> script that runs a command line.</summary>
    /// <param name="body">The shell commands. <c>$3</c> is the output file of a <c>dot -Tsvg -o file input</c> call.</param>
    /// <returns>The new folder, which the caller disposes.</returns>
    public static TemporaryDirectory WithRenderer(string body)
    {
        string path = Directory.CreateTempSubdirectory("tw-dot-").FullName;
        string program = Path.Combine(path, "dot");
        File.WriteAllText(program, "#!/bin/sh\n" + body + "\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(program, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return new TemporaryDirectory(path);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.FullPath, recursive: true);
    }
}
