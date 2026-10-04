namespace TruthWeaver.Abstractions;

/// <summary>
/// A variable reference (ADR-0006): a term argument given as <c>from("source", "query")</c> instead of a literal. It
/// names a data source and a query in that source's own dialect, and is resolved afresh on every evaluation. Equality
/// compares the source name and the query text exactly, never the value the query would resolve to, so two terms are
/// the same variable only when both strings match.
/// </summary>
/// <param name="Source">The name of the data source, as declared to the compiler and supplied in <see cref="DataSources"/>.</param>
/// <param name="Query">The query, an opaque string interpreted by the data source.</param>
public sealed record VariableReference(string Source, string Query)
{
    /// <summary>Renders this reference as it is written in the canonical DSL: <c>from("user", "$.minAge")</c>.</summary>
    /// <returns>The canonical DSL text.</returns>
    public override string ToString()
    {
        return $"from(\"{Escape(this.Source)}\", \"{Escape(this.Query)}\")";
    }

    /// <summary>Escapes <c>\</c> and <c>"</c> so the text is valid inside a DSL quoted string.</summary>
    private static string Escape(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
