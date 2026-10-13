namespace TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>Finds the fixture files that the build copies next to the test assembly.</summary>
internal static class Fixtures
{
    /// <summary>Gets the full path of a fixture file.</summary>
    /// <param name="name">The path below the <c>Fixtures</c> directory, with <c>/</c> separators.</param>
    /// <returns>The full path.</returns>
    public static string Path(string name)
    {
        return System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            name.Replace('/', System.IO.Path.DirectorySeparatorChar)
        );
    }

    /// <summary>Gets the text of a fixture file.</summary>
    /// <param name="name">The path below the <c>Fixtures</c> directory.</param>
    /// <returns>The text.</returns>
    public static string Read(string name)
    {
        return File.ReadAllText(Path(name));
    }
}
