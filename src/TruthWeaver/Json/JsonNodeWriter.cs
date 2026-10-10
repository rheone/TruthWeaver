namespace TruthWeaver.Json;

using System.Text.Json.Nodes;
using TruthWeaver.Printing;

/// <summary>
/// The JSON adapter of <see cref="ITreeWriter"/>: builds a <see cref="JsonNode"/> tree from the writer
/// calls. It holds no knowledge of the tree-format keys; <see cref="TreeFormatEmitter"/> owns those.
/// </summary>
internal sealed class JsonNodeWriter : ITreeWriter
{
    // Open containers, innermost last. A JsonObject takes values by pending key; a JsonArray by append.
    private readonly Stack<JsonNode> open = new();
    private string? pendingKey;

    /// <summary>Gets the finished document. Set when the outermost value has been written.</summary>
    public JsonNode? Result { get; private set; }

    /// <inheritdoc />
    public void BeginObject()
    {
        this.Open(new JsonObject());
    }

    /// <inheritdoc />
    public void EndObject()
    {
        this.open.Pop();
    }

    /// <inheritdoc />
    public void BeginArray()
    {
        this.Open(new JsonArray());
    }

    /// <inheritdoc />
    public void EndArray()
    {
        this.open.Pop();
    }

    /// <inheritdoc />
    public void Property(string name)
    {
        this.pendingKey = name;
    }

    /// <inheritdoc />
    public void String(string value, bool quoted)
    {
        this.Add(JsonValue.Create(value));
    }

    /// <inheritdoc />
    public void Int64(long value)
    {
        this.Add(JsonValue.Create(value));
    }

    /// <inheritdoc />
    public void Decimal(decimal value)
    {
        this.Add(JsonValue.Create(value));
    }

    /// <inheritdoc />
    public void Boolean(bool value)
    {
        this.Add(JsonValue.Create(value));
    }

    private void Open(JsonNode container)
    {
        this.Add(container);
        this.open.Push(container);
    }

    /// <summary>Attaches a finished value to the innermost open container, or makes it the result at the top level.</summary>
    private void Add(JsonNode? value)
    {
        if (this.open.Count == 0)
        {
            this.Result = value;
        }
        else if (this.open.Peek() is JsonObject obj)
        {
            obj[this.pendingKey!] = value;
        }
        else
        {
            ((JsonArray)this.open.Peek()).Add(value);
        }
    }
}
