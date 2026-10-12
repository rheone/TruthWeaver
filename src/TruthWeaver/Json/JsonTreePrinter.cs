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
    /// <param name="notes">The notes to write as <c>note</c> properties, or <see langword="null"/> for none.</param>
    /// <returns>The JSON text.</returns>
    public static string Print(Expression root, NoteLookup? notes = null)
    {
        return ToNode(root, notes).ToJsonString();
    }

    /// <summary>Builds the JSON document model of an expression tree.</summary>
    /// <param name="node">The tree to print.</param>
    /// <param name="notes">The notes to write as <c>note</c> properties, or <see langword="null"/> for none.</param>
    /// <returns>The root JSON node.</returns>
    internal static JsonNode ToNode(Expression node, NoteLookup? notes = null)
    {
        JsonNodeWriter writer = new();
        TreeFormatEmitter.Emit(node, writer, notes);
        return writer.Result!;
    }
}
