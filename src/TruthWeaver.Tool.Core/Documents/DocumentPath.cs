namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A parsed embedded-rule path: a singular JSONPath-style query that names one value in a JSON or YAML document. The
/// grammar is an optional <c>$</c>, then segments: <c>.name</c> (a letter or <c>_</c>, then letters, digits, <c>_</c> or
/// <c>-</c>), <c>['name']</c> or <c>["name"]</c> (any key), and <c>[n]</c> (an array index from 0). Without <c>$</c> the
/// first name may omit its dot, so <c>rules.admin</c> and <c>$.rules.admin</c> are the same path.
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
        int close = text.IndexOf(']', open + 1);
        if (close < 0)
        {
            return false;
        }

        string inner = text[(open + 1)..close].Trim();
        if (inner.Length >= 2 && inner[0] is '\'' or '"' && inner[^1] == inner[0])
        {
            segments.Add(Segment.Key(inner[1..^1]));
        }
        else if (
            inner.Length > 0
            && inner.All(char.IsAsciiDigit)
            && int.TryParse(inner, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
        )
        {
            segments.Add(Segment.Index(index));
        }
        else
        {
            return false;
        }

        next = close + 1;
        return true;
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
                { } name when name.Length > 0 && IsNameStart(name[0]) && name.All(IsNamePart) => "." + name,
                { } name => "['" + name + "']",
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
    }
}
