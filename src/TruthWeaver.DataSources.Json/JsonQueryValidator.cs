namespace TruthWeaver.DataSources.Json;

using TruthWeaver.Abstractions;

/// <summary>
/// Checks that a query is valid <see href="https://www.rfc-editor.org/rfc/rfc9535">RFC 9535</see> JSONPath, without any
/// document (ADR-0006 decision 4). Declare it with the source name so a malformed query in a rule is a compile diagnostic:
/// <c>new DataSourceDeclarations { ["user"] = JsonQueryValidator.Instance }</c>. It checks syntax only; whether the data holds a
/// match is known only at evaluation. It accepts exactly the queries <see cref="JsonDataSource"/> accepts, so a YAML source
/// can share it.
/// </summary>
public sealed class JsonQueryValidator : IQueryValidator
{
    private JsonQueryValidator() { }

    /// <summary>Gets the shared instance. The validator is stateless and thread-safe.</summary>
    public static JsonQueryValidator Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<QueryProblem> Validate(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return JsonPaths.TryParse(query, out _, out QueryProblem? problem) ? [] : [problem];
    }
}
