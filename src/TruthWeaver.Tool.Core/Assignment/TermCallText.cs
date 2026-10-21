namespace TruthWeaver.Tool.Core.Assignment;

using System.Text;

/// <summary>
/// Reads a predicate call out of rule text by its lexical shape alone (a run of name characters, optionally followed by a
/// parenthesized argument list), and tells whether two call texts name the same call once formatting differs. This is
/// lexical, not a parse: it works on text that has errors, and on a span before a rule compiles, which is why
/// <see cref="TermOutcomeLookup.FindBySpan"/> uses it rather than requiring a compiled tree.
/// </summary>
public static class TermCallText
{
    /// <summary>Finds the shortest predicate call or bare name that contains an offset of rule text.</summary>
    /// <param name="text">The rule text.</param>
    /// <param name="offset">The offset. The place right after a call counts as inside it.</param>
    /// <returns>The call text, such as <c>hasRole(role: "admin")</c>, or <see langword="null"/> when the offset is in no call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static string? At(string text, int offset)
    {
        ArgumentNullException.ThrowIfNull(text);

        (int Start, int End)? best = null;
        int depth = 0;
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '"')
            {
                // The content of a string argument is never itself a call name, escaped quote or not.
                i = SkipString(text, i);
                continue;
            }

            if (!char.IsLetter(c) && c != '_')
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')' && depth > 0)
                {
                    depth--;
                }

                i++;
                continue;
            }

            int nameEnd = i + 1;
            while (nameEnd < text.Length && IsNamePart(text[nameEnd]))
            {
                nameEnd++;
            }

            string name = text[i..nameEnd];
            int after = nameEnd;
            while (after < text.Length && char.IsWhiteSpace(text[after]))
            {
                after++;
            }

            char afterChar = after < text.Length ? text[after] : '\0';

            // A name directly followed by ':' while inside an open call is the name of an argument, not a call; "from"
            // directly followed by '(' is a data reference, not a term: neither names a term on its own (SyntaxMarks has
            // the same two exceptions for the editor's syntax coloring).
            bool isArgumentName = afterChar == ':' && depth > 0 && !(after + 1 < text.Length && text[after + 1] == ':');
            bool isDataReference = afterChar == '(' && string.Equals(name, "from", StringComparison.OrdinalIgnoreCase);

            if (afterChar != '(' && !isArgumentName)
            {
                Consider(ref best, i, nameEnd, offset);
            }
            else if (afterChar == '(' && !isDataReference)
            {
                int callEnd = OffsetAfterCloseParen(text, nameEnd);
                if (callEnd >= 0)
                {
                    Consider(ref best, i, callEnd, offset);
                }
            }

            i = nameEnd;
        }

        return best is { } found ? text[found.Start..found.End] : null;
    }

    /// <summary>
    /// Tells whether two call texts are the same call once white space outside their string arguments is ignored. The
    /// display of a term is normalized to one canonical spacing, but a call read from an editor is the text as typed, so a
    /// plain string comparison would call the same call different. White space inside a string argument is kept, so a
    /// query text that depends on its own spacing still compares correctly.
    /// </summary>
    /// <param name="first">The first call text, such as the display of a term.</param>
    /// <param name="second">The second call text, such as a call read from rule text.</param>
    /// <returns><see langword="true"/> when the calls differ at most in white space outside their strings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.</exception>
    public static bool SameCall(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return WithoutSpacesOutsideStrings(first) == WithoutSpacesOutsideStrings(second);
    }

    private static void Consider(ref (int Start, int End)? best, int start, int end, int offset)
    {
        if (offset >= start && offset <= end && (best is null || end - start < best.Value.End - best.Value.Start))
        {
            best = (start, end);
        }
    }

    private static bool IsNamePart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private static string WithoutSpacesOutsideStrings(string text)
    {
        StringBuilder result = new(text.Length);
        bool inString = false;
        bool escaped = false;
        foreach (char c in text)
        {
            if (inString)
            {
                result.Append(c);
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
                result.Append(c);
                continue;
            }

            if (!char.IsWhiteSpace(c))
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    // The offset one past the closing quote of the string that starts at `start`, with backslash escapes, or the end of
    // the text when the string is unterminated.
    private static int SkipString(string text, int start)
    {
        int i = start + 1;
        while (i < text.Length && text[i] != '"')
        {
            i += text[i] == '\\' ? 2 : 1;
        }

        return Math.Min(i + 1, text.Length);
    }

    // The offset after the parenthesis that closes the one at `open`, or -1 when the call is unterminated. Strings are
    // skipped with backslash escapes, so a quote or a parenthesis inside a string argument does not end the call early.
    private static int OffsetAfterCloseParen(string text, int open)
    {
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    if (depth == 0)
                    {
                        return i + 1;
                    }

                    break;
            }
        }

        return -1;
    }
}
