namespace TruthWeaver.Tool.Tests.TestSupport;

/// <summary>A new, empty directory under the system temporary directory, deleted with its contents on dispose.</summary>
internal sealed class TemporaryDirectory : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="TemporaryDirectory"/> class and creates the directory.</summary>
    public TemporaryDirectory()
    {
        this.FullPath = Path.Combine(Path.GetTempPath(), "truthweaver-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.FullPath);
    }

    /// <summary>Gets the full path of the directory.</summary>
    public string FullPath { get; }

    /// <summary>Gets the full path of a file in the directory.</summary>
    /// <param name="name">The file name, or a relative path.</param>
    /// <returns>The full path.</returns>
    public string File(string name)
    {
        return Path.Combine(this.FullPath, name);
    }

    /// <summary>Writes a file in the directory with LF line endings.</summary>
    /// <param name="name">The file name.</param>
    /// <param name="text">The text.</param>
    /// <returns>The full path.</returns>
    public string Write(string name, string text)
    {
        string path = this.File(name);
        System.IO.File.WriteAllText(path, text.ReplaceLineEndings("\n"));
        return path;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(this.FullPath))
        {
            Directory.Delete(this.FullPath, recursive: true);
        }
    }
}
