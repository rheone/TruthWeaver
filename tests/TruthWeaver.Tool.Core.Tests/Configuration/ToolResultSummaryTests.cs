namespace TruthWeaver.Tool.Core.Tests.Configuration;

using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;

/// <summary>A result describes its own first error and its warnings, so the wording exists once.</summary>
public sealed class ToolResultSummaryTests
{
    /// <summary>The failure summary is the code and message of the first error, not of an earlier warning or a later error.</summary>
    [Fact]
    public void FailureSummary_WithErrors_DescribesTheFirstError_Test()
    {
        ToolResult<string> result = Result(
            Diagnostic.Warning("W1", "a warning", SourceSpan.None),
            Diagnostic.Error("E1", "first error", SourceSpan.None),
            Diagnostic.Error("E2", "second error", SourceSpan.None)
        );

        Assert.Equal("E1: first error", result.FailureSummary());
    }

    /// <summary>A result with no error diagnostic gives the default text, or the text that the caller states.</summary>
    [Fact]
    public void FailureSummary_WithoutAnError_GivesTheFallback_Test()
    {
        ToolResult<string> result = Result(Diagnostic.Warning("W1", "a warning", SourceSpan.None));

        Assert.Equal("unknown problem.", result.FailureSummary());
        Assert.Equal("The document cannot be used.", result.FailureSummary("The document cannot be used."));
    }

    /// <summary>The warning summary lists every warning in order and skips errors.</summary>
    [Fact]
    public void WarningSummary_WithWarnings_ListsEachWarning_Test()
    {
        ToolResult<string> result = Result(
            Diagnostic.Warning("W1", "one", SourceSpan.None),
            Diagnostic.Error("E1", "an error", SourceSpan.None),
            Diagnostic.Warning("W2", "two", SourceSpan.None)
        );

        Assert.Equal("W1: one W2: two", result.WarningSummary());
    }

    /// <summary>A result with no warning has an empty warning summary.</summary>
    [Fact]
    public void WarningSummary_WithoutWarnings_IsEmpty_Test()
    {
        ToolResult<string> result = Result(Diagnostic.Error("E1", "an error", SourceSpan.None));

        Assert.Equal(string.Empty, result.WarningSummary());
    }

    private static ToolResult<string> Result(params Diagnostic[] diagnostics)
    {
        return new ToolResult<string>(null, new EquatableArray<Diagnostic>(diagnostics));
    }
}
