namespace TruthWeaver.Analysis;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// The outcome of one budgeted analysis: a decided <see cref="Value"/>, or the reason the analysis did not decide.
/// </summary>
/// <typeparam name="T">The decided value type. It can be a nullable type whose <see langword="null"/> is a real answer.</typeparam>
/// <remarks>
/// Test <see cref="UndecidedReason"/>, not <see cref="Value"/>: a decided analysis can have a <see langword="null"/>
/// value (for example, two equivalent rules have no counter-example).
/// </remarks>
internal readonly record struct AnalysisAttempt<T>(T? Value, string? UndecidedReason)
{
    /// <summary>Gets a value indicating whether the analysis decided, so that <see cref="Value"/> holds the answer.</summary>
    public bool IsDecided => this.UndecidedReason is null;
}

/// <summary>
/// The shared budget policy of the verdict analyses (<see cref="RuleEquivalence"/>, <see cref="RuleImplication"/>,
/// <see cref="RuleOverlap"/>): the term cap, the node-budget catch and the Undecided wording live here once.
/// </summary>
internal static class AnalysisVerdicts
{
    /// <summary>
    /// Runs <paramref name="work"/> when the distinct terms of all <paramref name="roots"/> fit the cap and turns a
    /// <see cref="BddNodeBudgetExceededException"/> into an Undecided reason.
    /// </summary>
    /// <typeparam name="T">The decided value type.</typeparam>
    /// <param name="roots">The rule roots analysed together. Their distinct terms are counted as one set.</param>
    /// <param name="maxAnalysisTerms">The term cap, or <see langword="null"/> for the <see cref="CompilerOptions.MaxAnalysisTerms"/> default.</param>
    /// <param name="work">The BDD analysis. It runs only when the cap holds.</param>
    /// <returns>The decided value, or an Undecided reason.</returns>
    public static AnalysisAttempt<T> Run<T>(IReadOnlyList<Expression> roots, int? maxAnalysisTerms, Func<T?> work)
    {
        int cap = maxAnalysisTerms ?? CompilerOptions.Default.MaxAnalysisTerms;
        HashSet<TermIdentity> terms = [];
        foreach (Expression root in roots)
        {
            terms.UnionWith(Analyzer.DistinctTerms(root));
        }

        if (terms.Count > cap)
        {
            string reason =
                roots.Count == 1
                    ? $"The rule has {terms.Count} distinct terms, which exceeds the configured cap of {cap}."
                    : $"The rules have {terms.Count} distinct terms between them, which exceeds the configured cap of {cap}.";
            return new AnalysisAttempt<T>(default, reason);
        }

        try
        {
            return new AnalysisAttempt<T>(work(), null);
        }
        catch (BddNodeBudgetExceededException exception)
        {
            return new AnalysisAttempt<T>(default, exception.Message);
        }
    }

    /// <summary>Keys an assignment by the printed term (such as <c>hasRole(role: "Y")</c>), so callers need no internal identity type.</summary>
    /// <param name="assignment">The assignment, or <see langword="null"/> for none.</param>
    /// <returns>The string-keyed copy, or <see langword="null"/> when <paramref name="assignment"/> is <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(assignment))]
    public static Dictionary<string, TruthValue>? Printed(IReadOnlyDictionary<TermIdentity, TruthValue>? assignment)
    {
        return assignment?.ToDictionary(p => p.Key.ToString(), p => p.Value);
    }
}
