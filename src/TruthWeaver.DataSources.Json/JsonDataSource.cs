namespace TruthWeaver.DataSources.Json;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using global::Json.Path;
using TruthWeaver.Abstractions;

/// <summary>
/// An <see cref="IDataSource"/> over a JSON document that answers JSONPath (<see href="https://www.rfc-editor.org/rfc/rfc9535">RFC 9535</see>)
/// queries (ADR-0006 decision 5). Each matched node is converted to the <see cref="LiteralValue"/> of its natural kind: a
/// JSON string is a string, an integer that fits is an <see cref="long"/>, any other number is a <see cref="decimal"/> and
/// <c>true</c>/<c>false</c> are booleans. The engine then applies the conversions ADR-0006 decision 7 allows (a string to a
/// date or GUID, an integer to a decimal, a whole decimal to an integer), so this source never guesses what the argument
/// wants. An object, an array and <c>null</c> have no literal equivalent and are reported as
/// <see cref="DataQueryErrorKind.UnsupportedType"/>; select their members instead (<c>$.roles[*]</c>).
/// </summary>
/// <remarks>
/// The document is held as a <see cref="JsonNode"/> tree and is never modified, so one instance is safe to share across
/// concurrent evaluations as long as nobody mutates the node it was created from. A YAML document reads into the same
/// data model, which is how <c>TruthWeaver.Yaml</c> reuses this source.
/// </remarks>
public sealed class JsonDataSource : IDataSource
{
    private readonly JsonNode? root;

    private JsonDataSource(JsonNode? root)
    {
        this.root = root;
    }

    /// <summary>Creates a source over JSON text.</summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>A source whose queries are absolute within <paramref name="json"/>.</returns>
    /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not well-formed JSON. The document comes from the host, not from a rule, so a malformed one is a programming or configuration error rather than data.</exception>
    public static JsonDataSource Parse([StringSyntax(StringSyntaxAttribute.Json)] string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return new JsonDataSource(JsonNode.Parse(json));
    }

    /// <summary>Creates a source over an already-parsed document.</summary>
    /// <param name="root">The document's root node, or <see langword="null"/> for a document that is the JSON <c>null</c> literal. The source reads it and never changes it; do not mutate it while the source is in use.</param>
    /// <returns>A source whose queries are absolute within <paramref name="root"/>.</returns>
    public static JsonDataSource Create(JsonNode? root)
    {
        return new JsonDataSource(root);
    }

    /// <inheritdoc />
    public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(this.Query(query));
    }

    /// <summary>Returns a new source rooted at the single node <paramref name="query"/> matches.</summary>
    /// <param name="query">A JSONPath query that matches exactly one node, for example <c>$.orders[?@.id=='A7']</c>. The node may be of any kind, an object, an array or a scalar.</param>
    /// <param name="cancellationToken">A token to honour.</param>
    /// <returns>A source over a copy of the matched node, so it stays valid and independent of the enclosing document; its queries are absolute within that node.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is not valid JSONPath.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="query"/> matches no node or more than one. Scoping is host code run outside an evaluation, so a wrong query fails loudly instead of narrowing to a guess.</exception>
    public ValueTask<IDataSource> ScopeAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!JsonPaths.TryParse(query, out JsonPath? path, out QueryProblem? problem))
        {
            throw new ArgumentException($"The scope query is not valid JSONPath: {problem.Message} (at position {problem.Position})", nameof(query));
        }

        NodeList matches = path.Evaluate(this.root).Matches;
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                $"A scope query must match exactly one node, but '{query}' matched {matches.Count}."
            );
        }

        // A node belongs to its parent, so the scoped source holds an independent copy as its own root.
        return ValueTask.FromResult<IDataSource>(new JsonDataSource(matches[0].Value?.DeepClone()));
    }

    private static DataQueryResult Convert(IReadOnlyList<JsonNode?> nodes)
    {
        List<LiteralValue> literals = new(nodes.Count);
        foreach (JsonNode? node in nodes)
        {
            if (!TryConvert(node, out LiteralValue literal, out string? kind))
            {
                // The message names only the JSON kind, never the value: the data may be sensitive (ADR-0006 decision 13).
                return DataQueryResult.Failure(
                    DataQueryErrorKind.UnsupportedType,
                    $"A matched node is {kind}, which has no literal equivalent. Select a scalar, for example with a member name or [*]."
                );
            }

            literals.Add(literal);
        }

        return DataQueryResult.Success(literals);
    }

    private static bool TryConvert(JsonNode? node, out LiteralValue literal, [NotNullWhen(false)] out string? unsupportedKind)
    {
        literal = default;
        unsupportedKind = null;
        switch (node)
        {
            case JsonValue value:
                switch (value.GetValueKind())
                {
                    case JsonValueKind.String:
                        literal = LiteralValue.OfString(value.GetValue<string>());
                        return true;
                    case JsonValueKind.True or JsonValueKind.False:
                        literal = LiteralValue.OfBoolean(value.GetValue<bool>());
                        return true;
                    case JsonValueKind.Number:
                        return TryConvertNumber(value, out literal, out unsupportedKind);
                    default:
                        unsupportedKind = "null";
                        return false;
                }

            case JsonArray:
                unsupportedKind = "an array";
                return false;
            case JsonObject:
                unsupportedKind = "an object";
                return false;
            default:
                unsupportedKind = "null";
                return false;
        }
    }

    // The number is read from its JSON text so the result does not depend on how the node stores it (parsed text or a CLR
    // number): "18" is an Int64, "18.0" and "1.5" are Decimal, and a number too large for a decimal has no literal.
    private static bool TryConvertNumber(JsonValue value, out LiteralValue literal, [NotNullWhen(false)] out string? unsupportedKind)
    {
        string text = value.ToJsonString();
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long integer))
        {
            literal = LiteralValue.OfInt64(integer);
            unsupportedKind = null;
            return true;
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number))
        {
            literal = LiteralValue.OfDecimal(number);
            unsupportedKind = null;
            return true;
        }

        literal = default;
        unsupportedKind = "a number outside the range of a decimal";
        return false;
    }

    private DataQueryResult Query(string query)
    {
        if (!JsonPaths.TryParse(query, out JsonPath? path, out QueryProblem? problem))
        {
            return DataQueryResult.Failure(
                DataQueryErrorKind.MalformedQuery,
                $"The query is not valid JSONPath: {problem.Message} (at position {problem.Position})"
            );
        }

        NodeList matches = path.Evaluate(this.root).Matches;
        return Convert([.. matches.Select(match => match.Value)]);
    }
}
