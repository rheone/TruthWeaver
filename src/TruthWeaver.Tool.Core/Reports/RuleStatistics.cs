namespace TruthWeaver.Tool.Core.Reports;

using System.Numerics;
using System.Text.Json.Nodes;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;

/// <summary>
/// The statistics of a compiled rule: its size, its terms and predicates, the operators it uses, the data it reads, the
/// cost of its analysis, whether it is constant, and the size of its truth table. Every list is in ordinal order, so
/// output that prints the statistics is the same on each run.
/// </summary>
public sealed record RuleStatistics
{
    /// <summary>Gets the number of nodes in the tree, counted as a tree (see <see cref="RuleMetrics.NodeCount"/>).</summary>
    public required long NodeCount { get; init; }

    /// <summary>Gets the number of nodes on the longest path from the root to a leaf. A rule that is one term has depth 1.</summary>
    public required int Depth { get; init; }

    /// <summary>Gets the number of term nodes. A term used twice counts twice.</summary>
    public required int TermCount { get; init; }

    /// <summary>Gets the number of distinct terms. A term used twice counts once.</summary>
    public required int DistinctTermCount { get; init; }

    /// <summary>Gets the distinct predicate names, in ordinal order.</summary>
    public required IReadOnlyList<string> Predicates { get; init; }

    /// <summary>
    /// Gets the number of nodes of each operator, keyed by the operator name of the JSON tree format (such as <c>and</c> or
    /// <c>atLeast</c>), in ordinal order of the name. Terms and constants are not operators.
    /// </summary>
    public required IReadOnlyList<KeyValuePair<string, int>> OperatorHistogram { get; init; }

    /// <summary>Gets the distinct variable references, in ordinal order of the source and then the query.</summary>
    public required IReadOnlyList<VariableUse> Variables { get; init; }

    /// <summary>Gets the distinct data source names that the variable references use, in ordinal order.</summary>
    public required IReadOnlyList<string> DataSources { get; init; }

    /// <summary>
    /// Gets the number of decision nodes of the Strong K3 analysis (see <see cref="RuleMetrics.BddNodeCount"/>), or
    /// <see langword="null"/> when the rule has more distinct terms than the analysis limit.
    /// </summary>
    public required int? BddNodeCount { get; init; }

    /// <summary>Gets whether the whole rule is a tautology, a contradiction, neither, or undecided.</summary>
    public required RuleConstancy Constancy { get; init; }

    /// <summary>Gets the number of rows of the Strong K3 truth table: 3 to the power of <see cref="DistinctTermCount"/>.</summary>
    public required BigInteger TruthTableRows { get; init; }

    /// <summary>Gets the number of rows of the classical truth table: 2 to the power of <see cref="DistinctTermCount"/>.</summary>
    public required BigInteger ClassicalTruthTableRows { get; init; }

    /// <summary>
    /// Gets a value indicating whether the truth table is within the default cap of
    /// <see cref="TruthTableOptions.DefaultTermCap"/> terms, so <c>PrintTruthTable</c> prints it without
    /// <see cref="TruthTableOptions.AllowLargerTable"/>.
    /// </summary>
    public required bool TruthTableWithinCap { get; init; }

    /// <summary>Computes the statistics of a compiled rule.</summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms for which the constancy check runs, or <see langword="null"/> for the compiler default. The
    /// BDD node count uses the limit that the rule was compiled with.
    /// </param>
    /// <returns>The statistics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static RuleStatistics Compute<TContext>(CompiledRule<TContext> rule, int? maxAnalysisTerms = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        // The JSON tree is the public form of the tree that names each operator and keeps each variable reference, so the
        // walk counts what the tree format shows, without the internal expression types.
        Walk walk = new();
        walk.Visit(JsonNode.Parse(rule.PrintJson(includeNotes: false)));

        RuleMetrics metrics = rule.Metrics;
        return new RuleStatistics
        {
            NodeCount = metrics.NodeCount,
            Depth = metrics.Depth,
            TermCount = walk.TermCount,
            DistinctTermCount = metrics.DistinctTermCount,
            Predicates = [.. rule.PredicateNames.Order(StringComparer.Ordinal)],
            OperatorHistogram = [.. walk.Operators.OrderBy(o => o.Key, StringComparer.Ordinal)],
            Variables =
            [
                .. walk.Variables.OrderBy(v => v.Source, StringComparer.Ordinal).ThenBy(v => v.Query, StringComparer.Ordinal),
            ],
            DataSources = [.. walk.Variables.Select(v => v.Source).Distinct().Order(StringComparer.Ordinal)],
            BddNodeCount = metrics.BddNodeCount,
            Constancy = ConstancyOf(rule, maxAnalysisTerms),
            TruthTableRows = BigInteger.Pow(3, metrics.DistinctTermCount),
            ClassicalTruthTableRows = BigInteger.Pow(2, metrics.DistinctTermCount),
            TruthTableWithinCap = metrics.DistinctTermCount <= TruthTableOptions.DefaultTermCap,
        };
    }

    // A rule is a tautology when it is equivalent to the constant True, and a contradiction when it is equivalent to False.
    private static RuleConstancy ConstancyOf<TContext>(CompiledRule<TContext> rule, int? maxAnalysisTerms)
    {
        RuleCompiler<TContext> compiler = new(PredicateRegistry<TContext>.CreateBuilder().Build());
        foreach (
            (string constant, RuleConstancy constancy) in new[]
            {
                ("True", RuleConstancy.Tautology),
                ("False", RuleConstancy.Contradiction),
            }
        )
        {
            CompiledRule<TContext> value = compiler.Compile(constant).GetRuleOrThrow();
            RuleEquivalenceOutcome outcome = RuleEquivalence.Compare(rule, value, maxAnalysisTerms).Outcome;
            if (outcome == RuleEquivalenceOutcome.Undecided)
            {
                return RuleConstancy.Undecided;
            }

            if (outcome == RuleEquivalenceOutcome.Equivalent)
            {
                return constancy;
            }
        }

        return RuleConstancy.Contingent;
    }

    /// <summary>Counts the term nodes, the operators and the variable references of a JSON rule tree.</summary>
    private sealed class Walk
    {
        public int TermCount { get; private set; }

        public Dictionary<string, int> Operators { get; } = [with(StringComparer.Ordinal)];

        public HashSet<VariableUse> Variables { get; } = [];

        public void Visit(JsonNode? node)
        {
            if (node is not JsonObject tree)
            {
                return;
            }

            if (tree.ContainsKey("predicate"))
            {
                this.TermCount++;
                IEnumerable<JsonObject> references = (tree["args"] as JsonObject ?? [])
                    .Select(argument => argument.Value)
                    .OfType<JsonObject>()
                    .Where(value => value.ContainsKey("from") && value.ContainsKey("query"));
                foreach (JsonObject reference in references)
                {
                    _ = this.Variables.Add(
                        new VariableUse(reference["from"]!.GetValue<string>(), reference["query"]!.GetValue<string>())
                    );
                }
            }
            else if (tree["op"] is JsonValue op)
            {
                string name = op.GetValue<string>();
                this.Operators[name] = this.Operators.GetValueOrDefault(name) + 1;
                foreach (JsonNode? operand in tree["operands"] as JsonArray ?? [])
                {
                    this.Visit(operand);
                }
            }
        }
    }
}
