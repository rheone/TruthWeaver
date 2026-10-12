namespace TruthWeaver.Json;

using System.Text;

/// <summary>
/// Removes the literal argument values from the text of a trace entry or a trace node, for a decision record written
/// without them. A term text has the form that <see cref="TermIdentity.ToString"/> gives
/// (<c>lookup(id: 42, role: "admin")</c>); each literal value becomes <see cref="Placeholder"/>. A variable argument
/// (<c>from("user", "$.age")</c>) is rule text, not data, so it stays, and so does the value a data source resolved for it
/// (<c>= 18</c>), which is in the trace only when the evaluation set <see cref="Evaluation.EvaluationOptions.IncludeResolvedValues"/>.
/// The note after a term (<c>[no match for ...]</c>) names a reference, not a value, and stays as well.
/// </summary>
/// <remarks>
/// A text that is not a term with arguments (an operator label such as <c>AND</c> or <c>Exactly(2)</c>, a constant, or a
/// term with no arguments) has no literal argument value and is returned unchanged. The placeholder itself parses as a
/// value, so redacting a redacted text gives the same text.
/// </remarks>
internal static class TraceTextRedaction
{
    /// <summary>The text written in place of each literal argument value.</summary>
    public const string Placeholder = "<omitted>";

    private const string VariablePrefix = "from(";

    /// <summary>Replaces each literal argument value in <paramref name="text"/> with <see cref="Placeholder"/>.</summary>
    /// <param name="text">The trace text.</param>
    /// <returns>The text without literal argument values; <paramref name="text"/> itself when it holds none.</returns>
    public static string Redact(string text)
    {
        int open = text.IndexOf('(', StringComparison.Ordinal);
        if (open <= 0 || !IsName(text.AsSpan(0, open)))
        {
            return text;
        }

        StringBuilder builder = new(text.Length);
        builder.Append(text, 0, open + 1);
        int position = open + 1;
        while (true)
        {
            // Each argument is "name: value"; a text without that shape (an operator label) is not a term.
            int colon = text.IndexOf(": ", position, StringComparison.Ordinal);
            if (colon < 0 || !IsName(text.AsSpan(position, colon - position)))
            {
                return text;
            }

            int start = colon + 2;
            int end = EndOfValue(text, start);
            if (end <= start)
            {
                return text;
            }

            builder.Append(text, position, start - position);
            string value = text[start..end];
            builder.Append(value.StartsWith(VariablePrefix, StringComparison.Ordinal) ? value : Placeholder);
            if (text[end] == ')')
            {
                builder.Append(text, end, text.Length - end);
                return builder.ToString();
            }

            if (!text.AsSpan(end).StartsWith(", ", StringComparison.Ordinal))
            {
                return text;
            }

            builder.Append(", ");
            position = end + 2;
        }
    }

    // A predicate or argument name: no white space and none of the characters that delimit values.
    private static bool IsName(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty)
        {
            return false;
        }

        foreach (char c in name)
        {
            if (char.IsWhiteSpace(c) || c is '(' or ')' or '[' or ']' or '"' or ',' or ':')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Finds the end of the argument value that starts at <paramref name="start"/>: the index of the <c>,</c> or <c>)</c>
    /// that follows it outside any string, bracket or parenthesis. A string literal escapes <c>\</c> and <c>"</c> with a
    /// backslash (<see cref="LiteralValue.ToString"/>), so a comma or bracket inside one is skipped.
    /// </summary>
    /// <returns>The index, or -1 when the text ends first or a bracket does not match.</returns>
    private static int EndOfValue(string text, int start)
    {
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (inString)
            {
                // The character after a backslash is part of the string, even when it is a quote.
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
                case '(' or '[':
                    depth++;
                    break;
                case ')' when depth == 0:
                    return i;
                case ']' when depth == 0:
                    return -1;
                case ')' or ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    return i;
            }
        }

        return -1;
    }
}
