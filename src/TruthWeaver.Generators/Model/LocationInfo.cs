namespace TruthWeaver.Generators.Model;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// A source location with value equality. A <see cref="Location"/> holds its syntax tree, so a pipeline model that
/// holds one keeps the old compilation alive and never compares equal to the next run.
/// </summary>
/// <param name="FilePath">The path of the source file.</param>
/// <param name="Span">The character span in the file.</param>
/// <param name="LineSpan">The line and column span in the file.</param>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    /// <summary>Captures a location, or returns <see langword="null"/> for a location outside source.</summary>
    /// <param name="location">The location to capture.</param>
    /// <returns>The captured location.</returns>
    public static LocationInfo? From(Location? location)
    {
        if (location?.SourceTree is null)
        {
            return null;
        }

        return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }

    /// <summary>Converts the captured value back to a location for a diagnostic.</summary>
    /// <returns>An external file location with the same span.</returns>
    public Location ToLocation()
    {
        return Location.Create(this.FilePath, this.Span, this.LineSpan);
    }
}
