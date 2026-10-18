namespace TruthWeaver.Tool.Tui.Theme;

/// <summary>Reads the <c>NO_COLOR</c> convention (<c>no-color.org</c>): a variable that is set and not empty asks for no color.</summary>
internal static class ColorMode
{
    /// <summary>Tells whether the author asked for no color.</summary>
    /// <param name="getEnvironmentVariable">Reads an environment variable, or <see langword="null"/> for the process environment.</param>
    /// <returns><see langword="true"/> when <c>NO_COLOR</c> is set and not empty.</returns>
    public static bool NoColorRequested(Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        return !string.IsNullOrEmpty(getEnvironmentVariable("NO_COLOR"));
    }
}
