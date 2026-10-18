namespace TruthWeaver.Tool.Tui.Sessions;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.RuleCases;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>
/// The coverage of the open rule over the saved cases of the rule test file: each case is evaluated with its values and
/// variables, and <see cref="RuleCoverage"/> of the library counts what the decisions reached. A node that no case reaches is
/// a node that every case skipped by short-circuiting. The marks and the summary read the library result and add no count of
/// their own.
/// </summary>
internal sealed class CaseCoverage
{
    private static readonly CaseCoverage NoRule = new(null, 0, 0, "No valid rule yet: coverage is not measured.");

    private readonly HashSet<string> reached;

    private CaseCoverage(RuleCoverage? coverage, int caseCount, int skippedCount, string summary)
    {
        this.Coverage = coverage;
        this.CaseCount = caseCount;
        this.SkippedCount = skippedCount;
        this.Summary = summary;
        this.reached = [.. coverage?.Nodes.Where(n => n.EvaluatedCount > 0).Select(n => n.Path) ?? []];
    }

    /// <summary>Gets the coverage of the library, or <see langword="null"/> when there is no rule or no case was evaluated.</summary>
    public RuleCoverage? Coverage { get; }

    /// <summary>Gets the number of cases that were evaluated.</summary>
    public int CaseCount { get; }

    /// <summary>Gets the number of cases that could not be evaluated, and so count for nothing.</summary>
    public int SkippedCount { get; }

    /// <summary>Gets the summary line: the share of the nodes that the cases reach, or why there is no share.</summary>
    public string Summary { get; }

    /// <summary>Gets a value indicating whether the cases measure anything, that is, whether marks show.</summary>
    public bool IsMeasured => this.Coverage is not null;

    /// <summary>Evaluates the cases on the rule of the document and measures the coverage.</summary>
    /// <param name="document">The open document. Its last tree that compiled is measured.</param>
    /// <param name="cases">The cases of the rule test file.</param>
    /// <param name="settings">The settings for compiling the rule.</param>
    /// <param name="cancellationToken">Cancels the evaluation.</param>
    /// <returns>The coverage, or a summary that says why there is none.</returns>
    public static async Task<CaseCoverage> MeasureAsync(
        RuleDocument document,
        IReadOnlyList<RuleTestRow> cases,
        ToolSettings settings,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(settings);

        if (document.Rule is not { } rule)
        {
            return NoRule;
        }

        if (cases.Count == 0)
        {
            return new CaseCoverage(null, 0, 0, "No saved cases: coverage is not measured.");
        }

        // As in AssignmentSession, the canonical text of the tree is evaluated: the text of the editor may have errors.
        if (
            !RuleLoader
                .LoadString(rule.CanonicalText, "<tree>", new RuleLoadOptions { Format = RuleFormat.Text })
                .TryGetValue(out LoadedRule? loaded, out _)
        )
        {
            return NoRule;
        }

        List<Decision> decisions = [];
        CompiledRule<StubAssignment>? evaluated = null;
        int skipped = 0;
        foreach (RuleTestCaseSpec spec in cases.Select(row => row.Spec))
        {
            JsonObject stubs = [];
            foreach ((string term, TruthValue value) in spec.Given)
            {
                stubs[term] = value.ToString();
            }

            DataSourceSet? sources = null;
            if (spec.Variables.Count > 0)
            {
                sources = AssignmentVariables.Parse(spec.Variables.ToJsonString()).Value;
            }

            StubEvaluationOptions options = new()
            {
                Stubs = StubAssignment.Parse(stubs.ToJsonString(), RuleFormat.Json).Value ?? StubAssignment.Empty,
                DataSources = sources,
                Evaluation = new EvaluationOptions(IncludeResolvedValues: true),
                Settings = settings,
            };
            ToolResult<StubEvaluation> result = await StubEvaluator
                .EvaluateAsync(loaded, options, cancellationToken)
                .ConfigureAwait(false);
            if (result.TryGetValue(out StubEvaluation? stub, out _))
            {
                evaluated ??= stub.Rule;
                decisions.Add(stub.Decision);
            }
            else
            {
                skipped++;
            }
        }

        if (evaluated is null)
        {
            return new CaseCoverage(null, 0, skipped, "No saved case could be evaluated: coverage is not measured.");
        }

        RuleCoverage coverage = RuleCoverage.FromDecisions(evaluated, decisions);
        int reachedCount = coverage.Nodes.Count(n => n.EvaluatedCount > 0);
        int percent = (int)Math.Round(100.0 * reachedCount / coverage.Nodes.Count);
        string summary =
            $"Coverage: {reachedCount} of {coverage.Nodes.Count} node(s) reached by {decisions.Count} case(s) ({percent}%).";
        if (skipped > 0)
        {
            summary += $" {skipped} case(s) could not be evaluated.";
        }

        return new CaseCoverage(coverage, decisions.Count, skipped, summary);
    }

    /// <summary>Tells whether no case reaches a node.</summary>
    /// <param name="path">The tree path of the node, such as <c>$.operands[1]</c>.</param>
    /// <returns><see langword="true"/> when coverage is measured and no case evaluates the node.</returns>
    public bool IsUncovered(string path)
    {
        return this.IsMeasured && !this.reached.Contains(path);
    }
}
