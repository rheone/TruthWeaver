namespace TruthWeaver.Generators;

/// <summary>Writes indented C# source line by line. Lines end with a line feed, so the output does not depend on the build machine.</summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder text = new();
    private int depth;

    /// <summary>Writes one line at the current indentation, or an empty line.</summary>
    /// <param name="line">The line text.</param>
    public void Line(string line = "")
    {
        if (line.Length > 0)
        {
            this.text.Append(' ', this.depth * 4).Append(line);
        }

        this.text.Append('\n');
    }

    /// <summary>Writes a header line (if any) and an opening brace, then indents.</summary>
    /// <param name="header">The line before the brace, for example a type declaration. Empty writes the brace alone.</param>
    public void Open(string header)
    {
        if (header.Length > 0)
        {
            this.Line(header);
        }

        this.Line("{");
        this.Indent();
    }

    /// <summary>Removes one indentation level and writes a closing brace.</summary>
    public void Close()
    {
        this.Dedent();
        this.Line("}");
    }

    /// <summary>Adds one indentation level.</summary>
    public void Indent()
    {
        this.depth++;
    }

    /// <summary>Removes one indentation level.</summary>
    public void Dedent()
    {
        this.depth--;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.text.ToString();
    }
}
