namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;

/// <summary>The shared budget policy of the verdict analyses: term cap, budget exception and reason wording.</summary>
public sealed class AnalysisVerdictsTests
{
    private static readonly TermExpression A = Term("a");
    private static readonly TermExpression B = Term("b");

    /// <summary>Several roots over the cap are undecided with the plural reason, and the work never runs.</summary>
    [Fact]
    public void Run_SeveralRootsOverCap_ReportsPluralReasonWithoutRunningWork_Test()
    {
        bool ran = false;

        AnalysisAttempt<string> attempt = AnalysisVerdicts.Run<string>(
            [A, B],
            1,
            () =>
            {
                ran = true;
                return "x";
            }
        );

        Assert.False(attempt.IsDecided);
        Assert.False(ran);
        Assert.Equal(
            "The rules have 2 distinct terms between them, which exceeds the configured cap of 1.",
            attempt.UndecidedReason
        );
    }

    /// <summary>A single root over the cap is undecided with the singular reason.</summary>
    [Fact]
    public void Run_OneRootOverCap_ReportsSingularReason_Test()
    {
        Expression root = new AndExpression(new([A, B]));

        AnalysisAttempt<string> attempt = AnalysisVerdicts.Run<string>([root], 1, () => "x");

        Assert.False(attempt.IsDecided);
        Assert.Equal("The rule has 2 distinct terms, which exceeds the configured cap of 1.", attempt.UndecidedReason);
    }

    /// <summary>A node-budget exception from the work becomes the reason, with its message unchanged.</summary>
    [Fact]
    public void Run_BudgetExceptionInWork_ReportsExceptionMessageAsReason_Test()
    {
        AnalysisAttempt<string> attempt = AnalysisVerdicts.Run<string>(
            [A],
            null,
            () => throw new BddNodeBudgetExceededException(7)
        );

        Assert.False(attempt.IsDecided);
        Assert.Equal("Building the shared BDD would exceed its budget of 7 decision nodes.", attempt.UndecidedReason);
    }

    /// <summary>A decided null value is told from undecided by the reason, not the value.</summary>
    [Fact]
    public void Run_DecidedNullValue_IsDecidedWithNoReason_Test()
    {
        AnalysisAttempt<string> attempt = AnalysisVerdicts.Run<string>([A, B], null, () => null);

        Assert.True(attempt.IsDecided);
        Assert.Null(attempt.Value);
        Assert.Null(attempt.UndecidedReason);
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
