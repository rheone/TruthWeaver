namespace TruthWeaver.Tool.Tui.Controllers;

using TruthWeaver.Tool.Tui.Theme;

/// <summary>
/// Finds the predicate call that surrounds a place in rule text, so the editor cursor can name a term. The search is lexical
/// (it reads <see cref="SyntaxMarks"/>), so it also works while the rule has errors; the caller decides whether to use it then.
/// </summary>
internal static class TermCalls
{
    /// <summary>Finds the innermost predicate call that contains an offset.</summary>
    /// <param name="text">The rule text.</param>
    /// <param name="offset">The cursor offset. The place right after the call counts as inside it.</param>
    /// <returns>The text of the call, such as <c>hasRole(role: "admin")</c>, or <see langword="null"/> when the offset is in no call.</returns>
    public static string? At(string text, int offset)
    {
        ArgumentNullException.ThrowIfNull(text);

        SyntaxMarks marks = SyntaxMarks.Compute(text);
        string? found = null;
        int foundLength = int.MaxValue;
        for (int start = 0; start < text.Length; start++)
        {
            // The first character of a run of predicate characters starts a call or a bare term.
            if (marks.At(start) != SyntaxKind.Predicate || (start > 0 && marks.At(start - 1) == SyntaxKind.Predicate))
            {
                continue;
            }

            int nameEnd = start;
            while (nameEnd < text.Length && marks.At(nameEnd) == SyntaxKind.Predicate)
            {
                nameEnd++;
            }

            int end = nameEnd < text.Length && text[nameEnd] == '(' ? CloseOf(text, nameEnd) : nameEnd;
            if (end < 0)
            {
                // No closing parenthesis: the call is unfinished, so it names no term.
                continue;
            }

            if (offset >= start && offset <= end && end - start < foundLength)
            {
                found = text[start..end];
                foundLength = end - start;
            }
        }

        return found;
    }

    // The offset after the parenthesis that closes the one at an offset, or -1. Strings are skipped, with backslash escapes.
    private static int CloseOf(string text, int open)
    {
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];
            if (inString)
            {
                // A backslash escapes the next character, so an escaped quote does not end the string.
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
