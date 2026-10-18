namespace TruthWeaver.Tool.Tui.Theme;

using TruthWeaver.Diagnostics;

/// <summary>
/// The characters of the editor text that a diagnostic marks, by line and column, so the editor can color them while it
/// draws. An error wins over a warning on the same character. A diagnostic with an empty span marks the character at its
/// start, or the last character when it starts at the end of the text; a diagnostic with no location marks nothing.
/// </summary>
internal sealed class EditorMarks
{
    private readonly int[] lineStarts;
    private readonly DiagnosticSeverity?[] marks;

    private EditorMarks(int[] lineStarts, DiagnosticSeverity?[] marks)
    {
        this.lineStarts = lineStarts;
        this.marks = marks;
    }

    /// <summary>Gets marks that mark nothing.</summary>
    public static EditorMarks None { get; } = new([0], []);

    /// <summary>Computes the marks of a text.</summary>
    /// <param name="text">The text with line feeds, as the editor shows it.</param>
    /// <param name="diagnostics">The diagnostics, with spans into <paramref name="text"/>.</param>
    /// <returns>The marks.</returns>
    public static EditorMarks Compute(string text, IEnumerable<Diagnostic> diagnostics)
    {
        List<int> starts = [0];
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        DiagnosticSeverity?[] marks = new DiagnosticSeverity?[text.Length];
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Span == SourceSpan.None || text.Length == 0)
            {
                continue;
            }

            int start = Math.Clamp(diagnostic.Span.Start, 0, text.Length - 1);
            int end = Math.Clamp(diagnostic.Span.End, start + 1, text.Length);
            for (int i = start; i < end; i++)
            {
                if (marks[i] is null || diagnostic.Severity > marks[i])
                {
                    marks[i] = diagnostic.Severity;
                }
            }
        }

        return new EditorMarks([.. starts], marks);
    }

    /// <summary>Gets the most severe diagnostic that marks a character.</summary>
    /// <param name="row">The 0-based line.</param>
    /// <param name="column">The 0-based column in the line.</param>
    /// <returns>The severity, or <see langword="null"/> when no diagnostic marks the character.</returns>
    public DiagnosticSeverity? At(int row, int column)
    {
        if (row < 0 || row >= this.lineStarts.Length || column < 0)
        {
            return null;
        }

        int offset = this.lineStarts[row] + column;
        return offset < this.marks.Length ? this.marks[offset] : null;
    }

    /// <summary>Finds the 0-based line and column of an offset.</summary>
    /// <param name="offset">The offset into the text.</param>
    /// <returns>The line and column.</returns>
    public (int Row, int Column) PositionOf(int offset)
    {
        int row = Array.BinarySearch(this.lineStarts, offset);
        if (row < 0)
        {
            // BinarySearch returns the complement of the next larger start; the line is the one before it.
            row = ~row - 1;
        }

        row = Math.Max(row, 0);
        return (row, offset - this.lineStarts[row]);
    }
}
