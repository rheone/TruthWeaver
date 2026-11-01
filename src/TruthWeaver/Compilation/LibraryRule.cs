namespace TruthWeaver.Compilation;

/// <summary>One named rule of a <see cref="RuleLibrary"/>: its name, its rule text and the content hash of that text.</summary>
public sealed class LibraryRule
{
    /// <summary>Initializes a new instance of the <see cref="LibraryRule"/> class.</summary>
    /// <param name="name">The rule name.</param>
    /// <param name="text">The rule text.</param>
    /// <param name="hash">The content hash of <paramref name="text"/>.</param>
    internal LibraryRule(string name, string text, string hash)
    {
        this.Name = name;
        this.Text = text;
        this.Hash = hash;
    }

    /// <summary>Gets the rule name, as the library was given it.</summary>
    public string Name { get; }

    /// <summary>Gets the rule text.</summary>
    public string Text { get; }

    /// <summary>
    /// Gets the content hash of <see cref="Text"/>: the SHA-256 hash of its UTF-8 bytes, as 64 upper-case hexadecimal
    /// digits, after every CRLF and lone CR is read as LF. It is the value that a pinned reference
    /// <c>rule("name", hash: "...")</c> names.
    /// </summary>
    public string Hash { get; }
}
