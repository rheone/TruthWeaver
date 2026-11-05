namespace TruthWeaver.Tool.Core.Documents;

using System.Text.Json;

/// <summary>A <see cref="JsonElement"/> in a reference type, so <see cref="DocumentPath.Locate{TNode}"/> can hold it.</summary>
/// <param name="Element">The wrapped element.</param>
internal sealed record JsonElementNode(JsonElement Element);
