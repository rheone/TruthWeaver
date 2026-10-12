namespace TruthWeaver.Tool.Tests.TestSupport;

/// <summary>Finds the sample rules and files that the build copies next to the test assembly.</summary>
internal static class Samples
{
    /// <summary>Gets the full path of the samples directory.</summary>
    public static string Directory { get; } = System.IO.Path.Combine(AppContext.BaseDirectory, "Samples");

    /// <summary>Gets the full path of a sample file.</summary>
    /// <param name="name">The path below the samples directory, with <c>/</c> separators.</param>
    /// <returns>The full path.</returns>
    public static string Path(string name)
    {
        return System.IO.Path.Combine(Directory, name.Replace('/', System.IO.Path.DirectorySeparatorChar));
    }
}
