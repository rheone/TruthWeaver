namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Diagnostics;

/// <summary>The editor marks: which characters a diagnostic colors, by line and column.</summary>
public sealed class EditorMarksTests
{
    /// <summary>A span on the second line marks its characters there and nothing else.</summary>
    [Fact]
    public void Compute_SpanOnSecondLine_MarksItsCharacters_Test()
    {
        Diagnostic error = Diagnostic.Error("TRE0001", "x", new SourceSpan(8, 3));

        EditorMarks marks = EditorMarks.Compute("a AND\nb AND c", [error]);

        Assert.Equal(DiagnosticSeverity.Error, marks.At(1, 2));
        Assert.Equal(DiagnosticSeverity.Error, marks.At(1, 4));
        Assert.Null(marks.At(1, 5));
        Assert.Null(marks.At(0, 2));
    }

    /// <summary>An error wins over a warning on the same character.</summary>
    [Fact]
    public void Compute_ErrorAndWarningOverlap_MarksTheError_Test()
    {
        Diagnostic warning = Diagnostic.Warning("TRE0002", "w", new SourceSpan(0, 3));
        Diagnostic error = Diagnostic.Error("TRE0001", "e", new SourceSpan(1, 1));

        EditorMarks marks = EditorMarks.Compute("abc", [warning, error]);

        Assert.Equal(DiagnosticSeverity.Warning, marks.At(0, 0));
        Assert.Equal(DiagnosticSeverity.Error, marks.At(0, 1));
    }

    /// <summary>An empty span at the end of the text marks the last character, so the author sees where input ended.</summary>
    [Fact]
    public void Compute_EmptySpanAtTheEnd_MarksTheLastCharacter_Test()
    {
        Diagnostic error = Diagnostic.Error("TRE0001", "e", new SourceSpan(5, 0));

        EditorMarks marks = EditorMarks.Compute("a AND", [error]);

        Assert.Equal(DiagnosticSeverity.Error, marks.At(0, 4));
    }

    /// <summary>The position of an offset is its line and column.</summary>
    [Fact]
    public void PositionOf_OffsetOnSecondLine_ReturnsLineAndColumn_Test()
    {
        EditorMarks marks = EditorMarks.Compute("ab\ncd", []);

        Assert.Equal((1, 1), marks.PositionOf(4));
        Assert.Equal((0, 0), marks.PositionOf(0));
    }
}
