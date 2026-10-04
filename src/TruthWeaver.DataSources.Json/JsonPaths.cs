namespace TruthWeaver.DataSources.Json;

using System.Diagnostics.CodeAnalysis;
using global::Json.Path;
using TruthWeaver.Abstractions;

/// <summary>
/// The one place a JSONPath string is parsed, so <see cref="JsonQueryValidator"/> (compile time) and
/// <see cref="JsonDataSource"/> (evaluation time) accept exactly the same queries.
/// </summary>
internal static class JsonPaths
{
    /// <summary>Parses a query as strict RFC 9535 JSONPath.</summary>
    /// <param name="query">The query text.</param>
    /// <param name="path">The parsed path when successful.</param>
    /// <param name="problem">What is wrong, with the offset where the parser stopped, when unsuccessful.</param>
    /// <returns><see langword="true"/> when <paramref name="query"/> is valid.</returns>
    public static bool TryParse(string query, [NotNullWhen(true)] out JsonPath? path, [NotNullWhen(false)] out QueryProblem? problem)
    {
        try
        {
            // JsonPath.Net signals a syntax error by throwing; here that becomes a result. Default options are the strict RFC 9535 grammar.
            path = JsonPath.Parse(query);
            problem = null;
            return true;
        }
        catch (PathParseException ex)
        {
            path = null;
            problem = new QueryProblem(ex.Message, ex.Index);
            return false;
        }
        catch (IndexOutOfRangeException)
        {
            // JsonPath.Net 2.2.0 reads past the end of a query that stops right after a member dot ("$." or "$.a."), where it
            // should raise a PathParseException. That is a malformed query like any other, ending where the text does.
            path = null;
            problem = new QueryProblem("The query ends unexpectedly after '.'; a member name, '*' or a bracketed selector must follow.", query.Length);
            return false;
        }
    }
}
