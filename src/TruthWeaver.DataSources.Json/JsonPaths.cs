namespace TruthWeaver.DataSources.Json;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Meziantou.Framework.Json;
using TruthWeaver.Abstractions;

/// <summary>
/// The one place a JSONPath string is parsed, so <see cref="JsonQueryValidator"/> (compile time) and
/// <see cref="JsonDataSource"/> (evaluation time) accept exactly the same queries.
/// </summary>
internal static partial class JsonPaths
{
    /// <summary>Parses a query as strict RFC 9535 JSONPath.</summary>
    /// <param name="query">The query text.</param>
    /// <param name="path">The parsed path when successful.</param>
    /// <param name="problem">What is wrong, with the offset where the parser stopped, when unsuccessful.</param>
    /// <returns><see langword="true"/> when <paramref name="query"/> is valid.</returns>
    public static bool TryParse(
        string query,
        [NotNullWhen(true)] out JsonPath? path,
        [NotNullWhen(false)] out QueryProblem? problem
    )
    {
        try
        {
            // Meziantou.Framework.JsonPath signals a syntax error by throwing a FormatException; here that becomes a result.
            // The parser is the strict RFC 9535 grammar, and its TryParse variant would drop the message and offset.
            path = JsonPath.Parse(query);
            problem = null;
            return true;
        }
        catch (FormatException ex)
        {
            path = null;
            problem = new QueryProblem(ex.Message, ReadPosition(ex.Message));
            return false;
        }
    }

    // The parser reports the offset only inside its message ("... at position 9"); it exposes no property for it. A message
    // without that phrase gives no position rather than a wrong one.
    private static int? ReadPosition(string message)
    {
        Match match = PositionPattern().Match(message);
        return
            match.Success
            && int.TryParse(match.Groups[1].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int position)
            ? position
            : null;
    }

    [GeneratedRegex(@"at position (\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex PositionPattern();
}
