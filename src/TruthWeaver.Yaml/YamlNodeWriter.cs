namespace TruthWeaver.Yaml;

using TruthWeaver.Printing;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// The YAML adapter of <see cref="ITreeWriter"/>: builds a <see cref="YamlNode"/> tree from the writer
/// calls. It holds no knowledge of the tree-format keys or structure; <see cref="TreeFormatEmitter"/> owns
/// those, so a rule-tree schema change needs an edit in the emitter only.
/// </summary>
internal sealed class YamlNodeWriter : ITreeWriter
{
    // Open containers, innermost last. A mapping takes values by pending key; a sequence by append.
    private readonly Stack<YamlNode> open = new();
    private string? pendingKey;

    /// <summary>Gets the finished document root. Set when the outermost value has been written.</summary>
    public YamlNode? Result { get; private set; }

    /// <inheritdoc />
    public void BeginObject()
    {
        this.Open(new YamlMappingNode());
    }

    /// <inheritdoc />
    public void EndObject()
    {
        this.open.Pop();
    }

    /// <inheritdoc />
    public void BeginArray()
    {
        this.Open(new YamlSequenceNode());
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
        // User data is double-quoted so a string such as "1" or "true" is never re-read as another kind.
        this.Add(new YamlScalarNode(value) { Style = quoted ? ScalarStyle.DoubleQuoted : ScalarStyle.Plain });
    }

    /// <inheritdoc />
    public void Int64(long value)
    {
        this.Add(new YamlScalarNode(value.ToString(CultureInfo.InvariantCulture)) { Style = ScalarStyle.Plain });
    }

    /// <inheritdoc />
    public void Decimal(decimal value)
    {
        this.Add(new YamlScalarNode(value.ToString(CultureInfo.InvariantCulture)) { Style = ScalarStyle.Plain });
    }

    /// <inheritdoc />
    public void Boolean(bool value)
    {
        this.Add(new YamlScalarNode(value ? "true" : "false") { Style = ScalarStyle.Plain });
    }

    private void Open(YamlNode container)
    {
        this.Add(container);
        this.open.Push(container);
    }

    /// <summary>Attaches a finished value to the innermost open container, or makes it the result at the top level.</summary>
    private void Add(YamlNode value)
    {
        if (this.open.Count == 0)
        {
            this.Result = value;
        }
        else if (this.open.Peek() is YamlMappingNode mapping)
        {
            mapping.Add(new YamlScalarNode(this.pendingKey), value);
        }
        else
        {
            ((YamlSequenceNode)this.open.Peek()).Add(value);
        }
    }
}
