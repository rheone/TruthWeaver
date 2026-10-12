namespace TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>A new, empty directory under the system temporary directory, deleted with its contents on dispose.</summary>
internal sealed class TemporaryDirectory : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="TemporaryDirectory"/> class and creates the directory.</summary>
    public TemporaryDirectory()
    {
        this.FullPath = Path.Combine(Path.GetTempPath(), "truthweaver-tool-tests", Guid.NewGuid().ToString("N"));
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(this.FullPath))
        {
            Directory.Delete(this.FullPath, recursive: true);
        }
    }
}
