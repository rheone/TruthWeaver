namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using TruthWeaver.Tool.Core.Export;

/// <summary>
/// A folder that stands in for the search path of the image renderers. It holds empty files named like the programs, so the
/// export dialog finds them without a real Graphviz or Mermaid CLI. A test never runs them.
/// </summary>
internal sealed class FakeRenderers : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="FakeRenderers"/> class.</summary>
    /// <param name="languages">The diagram languages whose program the folder holds.</param>
    public FakeRenderers(params DiagramLanguage[] languages)
    {
        this.Folder = Directory.CreateTempSubdirectory("tw-renderers-").FullName;
        foreach (DiagramLanguage language in languages)
        {
            // ".exe" is the first extension that the Windows search tries; other systems look for the bare name.
            string name = ImageRenderer.ProgramName(language) + (OperatingSystem.IsWindows() ? ".exe" : string.Empty);
            File.WriteAllBytes(Path.Combine(this.Folder, name), []);
        }
    }

    /// <summary>Gets the folder to use as the image search path.</summary>
    public string Folder { get; }

    /// <summary>Gets a search path that holds the programs of every diagram language.</summary>
    /// <returns>The new folder, which the caller disposes.</returns>
    public static FakeRenderers Both()
    {
        return new FakeRenderers(DiagramLanguage.Dot, DiagramLanguage.Mermaid);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.Folder, recursive: true);
    }
}
