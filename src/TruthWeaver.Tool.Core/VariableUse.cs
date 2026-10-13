namespace TruthWeaver.Tool.Core;

/// <summary>A variable reference that a rule uses: <c>from("source", "query")</c>.</summary>
/// <param name="Source">The data source name.</param>
/// <param name="Query">The query, in the dialect of the data source.</param>
public sealed record VariableUse(string Source, string Query);
