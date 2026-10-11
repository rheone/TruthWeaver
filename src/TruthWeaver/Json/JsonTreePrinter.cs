namespace TruthWeaver.Json;

using System.Text.Json.Nodes;
using TruthWeaver.Ast;
using TruthWeaver.Printing;

/// <summary>
/// Prints a compiled expression tree to the flat, key-discriminated JSON tree shape (ADR-0003) —
/// the JSON-direction half of ticket 07's round-trip guarantee.
/// </summary>
internal static class JsonTreePrinter
{
    /// <summary>Prints an expression tree to JSON tree text.</summary>
    /// <param name="root">The tree to print.</param>
    /// <returns>The JSON text.</returns>
    public static string Print(Expression root)
    {
        return ToNode(root).ToJsonString();
    }

    internal static JsonNode ToNode(Expression node)
    {
        JsonNodeWriter writer = new();
        TreeFormatEmitter.Emit(node, writer);
        return writer.Result!;
    }
}
