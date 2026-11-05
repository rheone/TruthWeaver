namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A parsed embedded-rule path: a singular JSONPath-style query that names one value in a JSON or YAML document. The
/// grammar is an optional <c>$</c>, then segments: <c>.name</c> (a letter or <c>_</c>, then letters, digits, <c>_</c> or
/// <c>-</c>), <c>['name']</c> or <c>["name"]</c> (any key; a backslash before a quote stands for that quote and a
/// <c>]</c> inside the quotes belongs to the key), and <c>[n]</c> (an array index from 0). Without <c>$</c> the
/// first name may omit its dot, so <c>rules.admin</c> and <c>$.rules.admin</c> are the same path. The path is written in the
/// form of <c>TreePath</c>: <c>.name</c> for letters, digits and <c>_</c>, and <c>['name']</c> for any other key, with
/// <c>\'</c> for each quote.
/// </summary>
internal sealed class DocumentPath
{
    private DocumentPath(IReadOnlyList<Segment> segments)
    {
        this.Segments = segments;
    }

    /// <summary>Gets the segments, from the document root down.</summary>
    public IReadOnlyList<Segment> Segments { get; }

    /// <summary>Gets the normalized text of the path: <c>$</c>, then <c>.name</c>, <c>['key']</c> or <c>[n]</c> for each segment.</summary>
    public string Text => this.Format(this.Segments.Count);

    /// <summary>Parses a path.</summary>
    /// <param name="text">The path text.</param>
    /// <param name="path">The parsed path when this method returns <see langword="true"/>.</param>
    /// <param name="problem">What is wrong, when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid path.</returns>
    public static bool TryParse(
        string text,
        [NotNullWhen(true)] out DocumentPath? path,
        [NotNullWhen(false)] out string? problem
    )
    {
        List<Segment> segments = [];
        int position = 0;
        string trimmed = text.Trim();
        if (trimmed.StartsWith('$'))
        {
            position = 1;
        }
        else if (trimmed.Length > 0 && IsNameStart(trimmed[0]))
        {
            // A bare first name: read it as if a dot came before it.
            position = ReadName(trimmed, 0, segments);
        }

        while (position < trimmed.Length)
        {
            char c = trimmed[position];
            if (c == '.' && position + 1 < trimmed.Length && IsNameStart(trimmed[position + 1]))
            {
                position = ReadName(trimmed, position + 1, segments);
            }
            else if (c == '[' && TryReadBracket(trimmed, position, segments, out int next))
            {
                position = next;
            }
            else
            {
                path = null;
                problem = $"The path '{text}' is not valid at character {position + 1}.";
                return false;
            }
        }

        path = new DocumentPath(segments);
        problem = null;
        return true;
    }

    /// <summary>Gets the normalized text of the first <paramref name="count"/> segments.</summary>
    /// <param name="count">The number of segments to include.</param>
    /// <returns>The path text, <c>$</c> for no segments.</returns>
    public string Format(int count)
    {
        StringBuilder builder = new("$");
        foreach (Segment segment in this.Segments.Take(count))
        {
            builder.Append(segment.Text);
        }

        return builder.ToString();
    }

    /// <summary>Gets the path of the node that holds this one.</summary>
    /// <returns>The path without its last segment, or <see langword="null"/> for the root.</returns>
    public DocumentPath? Parent()
    {
        return this.Segments.Count == 0 ? null : new DocumentPath([.. this.Segments.Take(this.Segments.Count - 1)]);
    }

    /// <summary>Gets the path of an operand of the rule node this path names: this path, then <c>operands</c>, then the index.</summary>
    /// <param name="index">The index of the operand.</param>
    /// <returns>The operand path.</returns>
    public DocumentPath Operand(int index)
    {
        return new DocumentPath([.. this.Segments, Segment.Key("operands"), Segment.Index(index)]);
    }

    /// <summary>Walks the segments from a root, in order, through a tree.</summary>
    /// <typeparam name="TNode">The node type of the tree.</typeparam>
    /// <param name="navigator">The steps of the tree type.</param>
    /// <param name="root">The root node.</param>
    /// <returns>The node with its parent, or the segment at which the walk stopped.</returns>
    public PathLocation<TNode> Locate<TNode>(IPathNavigator<TNode> navigator, TNode root)
        where TNode : class
    {
        TNode current = root;
        TNode? parent = null;
        Segment? last = null;
        for (int index = 0; index < this.Segments.Count; index++)
        {
            Segment segment = this.Segments[index];
            if (!navigator.TryGetChild(current, segment, out TNode? child))
            {
                return PathLocation<TNode>.Stop(navigator, segment, index);
            }

            parent = current;
            last = segment;
            current = child;
        }

        return PathLocation<TNode>.Reached(navigator, current, parent, last);
    }

    private static bool IsNameStart(char c)
    {
        return char.IsAsciiLetter(c) || c == '_';
    }

    private static bool IsNamePart(char c)
    {
        return char.IsAsciiLetterOrDigit(c) || c is '_' or '-';
    }

    private static int ReadName(string text, int start, List<Segment> segments)
    {
        int end = start;
        while (end < text.Length && IsNamePart(text[end]))
        {
            end++;
        }

        segments.Add(Segment.Key(text[start..end]));
        return end;
    }

    private static bool TryReadBracket(string text, int open, List<Segment> segments, out int next)
    {
        next = open;
        int position = SkipSpaces(text, open + 1);
        if (position >= text.Length)
        {
            return false;
        }

        Segment segment;
        if (text[position] is '\'' or '"')
        {
            if (!TryReadQuotedKey(text, ref position, out string? key))
            {
                return false;
            }

            segment = Segment.Key(key);
        }
        else
        {
            int start = position;
            while (position < text.Length && char.IsAsciiDigit(text[position]))
            {
                position++;
            }

            if (
                position == start
                || !int.TryParse(text[start..position], NumberStyles.None, CultureInfo.InvariantCulture, out int index)
            )
            {
                return false;
            }

            segment = Segment.Index(index);
        }

        position = SkipSpaces(text, position);
        if (position >= text.Length || text[position] != ']')
        {
            return false;
        }

        segments.Add(segment);
        next = position + 1;
        return true;
    }

    private static int SkipSpaces(string text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    // Reads the quoted key that starts at position. A backslash before a quote character stands for that quote, and a
    // closing bracket or a dot inside the quotes belongs to the key. On success position is just after the closing quote.
    private static bool TryReadQuotedKey(string text, ref int position, [NotNullWhen(true)] out string? key)
    {
        char quote = text[position];
        StringBuilder builder = new();
        int i = position + 1;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '\\' && i + 1 < text.Length && text[i + 1] is '\'' or '"')
            {
                builder.Append(text[i + 1]);
                i += 2;
            }
            else if (c == quote)
            {
                position = i + 1;
                key = builder.ToString();
                return true;
            }
            else
            {
                builder.Append(c);
                i++;
            }
        }

        key = null;
        return false;
    }

    /// <summary>One step of a path: a mapping key or an array index.</summary>
    /// <param name="Name">The key, or <see langword="null"/> for an index.</param>
    /// <param name="Position">The index, when <paramref name="Name"/> is <see langword="null"/>.</param>
    internal sealed record Segment(string? Name, int Position)
    {
        /// <summary>Gets the normalized text of this segment.</summary>
        public string Text =>
            this.Name switch
            {
                null => $"[{this.Position.ToString(CultureInfo.InvariantCulture)}]",
                { } name when name.Length > 0 && IsNameStart(name[0]) && name.All(IsPlainPart) => "." + name,
                { } name => "['" + name.Replace("'", "\\'", StringComparison.Ordinal) + "']",
            };

        /// <summary>Creates a key segment.</summary>
        public static Segment Key(string name)
        {
            return new Segment(name, 0);
        }

        /// <summary>Creates an index segment.</summary>
        public static Segment Index(int position)
        {
            return new Segment(null, position);
        }

        // The writer form of a name part: a hyphen needs the bracket form, as in TreePath.
        private static bool IsPlainPart(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c == '_';
        }
    }
}
