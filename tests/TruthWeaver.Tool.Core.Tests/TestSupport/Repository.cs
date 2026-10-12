namespace TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>Finds files in the repository, for the tests that check the code against the documentation.</summary>
internal static class Repository
{
    /// <summary>Gets the full path of a file below the repository root, the directory that holds <c>TruthWeaver.slnx</c>.</summary>
    /// <param name="segments">The path segments below the root.</param>
    /// <returns>The full path.</returns>
    public static string Path(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "TruthWeaver.slnx")))
        {
            directory = directory.Parent;
        }

        string root =
            directory?.FullName
            ?? throw new InvalidOperationException("TruthWeaver.slnx was not found above " + AppContext.BaseDirectory);
        return System.IO.Path.Combine([root, .. segments]);
    }
}
