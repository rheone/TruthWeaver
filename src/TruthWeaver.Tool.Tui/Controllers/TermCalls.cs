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
    public static string? CallTextAt(string text, int offset)
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

            int end = nameEnd < text.Length && text[nameEnd] == '(' ? OffsetAfterCloseParen(text, nameEnd) : nameEnd;
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

    /// <summary>
    /// Gets the name of a predicate call. The one rule: the name is the run of <see cref="SyntaxKind.Predicate"/> marks
    /// (see <see cref="SyntaxMarks"/>) at the start of the text. It ends at the first other character, so the opening
    /// parenthesis, an argument and white space after the name are not part of it, and a bare name is its own name.
    /// </summary>
    /// <param name="call">The call text, such as <c>hasRole(role: "admin")</c>, or a bare name.</param>
    /// <returns>The name, such as <c>hasRole</c>, or an empty string when the text does not start with a predicate name.</returns>
    public static string NameOf(string call)
    {
        ArgumentNullException.ThrowIfNull(call);

        SyntaxMarks marks = SyntaxMarks.Compute(call);
        int end = 0;
        while (end < call.Length && marks.At(end) == SyntaxKind.Predicate)
        {
            end++;
        }

        return call[..end];
    }

    /// <summary>
    /// Tells whether two call texts are the same call when white space is ignored. The display of a term is normalized
    /// (one canonical spacing), but the call at the editor cursor is the text as typed, so a plain string comparison would
    /// call the same call different. White space is removed everywhere, also inside string arguments.
    /// </summary>
    /// <param name="first">The first call text, such as the display of a term.</param>
    /// <param name="second">The second call text, such as the call at the cursor.</param>
    /// <returns><see langword="true"/> when the calls differ at most in white space.</returns>
    public static bool SameCall(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return WithoutSpaces(first) == WithoutSpaces(second);
    }

    private static string WithoutSpaces(string text)
    {
        return string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
    }

    // The offset after the parenthesis that closes the one at an offset, or -1. Strings are skipped, with backslash escapes.
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
