namespace TruthWeaver.Tool.Core.Assignment;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>
/// The values a rule is tried with, and the decision that follows from them: a stub value for each term, the data for its
/// <c>from(...)</c> references and the policy that turns an <c>Unknown</c> result into an answer. It is the UI-free model of
/// the assignment pane of the terminal UI. It evaluates through <see cref="StubEvaluator"/>, so the decision equals that of
/// <c>truthweaver evaluate</c> for the same values.
/// </summary>
/// <remarks>
/// <para>A term with no value is <see cref="TruthValue.Unknown"/>. The session keeps the values by the canonical text of a
/// term, so they survive an edit of the rule: a value applies again when its term comes back. A term with a <c>from(...)</c>
/// argument takes the value of the term with the value that the data gives, as for the command line.</para>
/// <para>The session has no thread affinity and no locks: call it from one thread.</para>
/// </remarks>
/// <remarks>Initializes a new instance of the <see cref="AssignmentSession"/> class.</remarks>
/// <param name="settings">The settings (limits and operator style) for compiling, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
public sealed partial class AssignmentSession(ToolSettings? settings = null)
{
    private readonly ToolSettings settings = settings ?? ToolSettings.Default;
    private readonly Dictionary<string, TruthValue> values = [with(StringComparer.Ordinal)];
    private DataSourceSet sources = [];

    /// <summary>Gets the terms that have a definite value, by canonical text. A term that is not here is <see cref="TruthValue.Unknown"/>.</summary>
    public IReadOnlyDictionary<string, TruthValue> Values => this.values;

    /// <summary>Gets the text of the variables document: a JSON or YAML object that maps each source name to its data.</summary>
    public string VariablesText { get; private set; } = string.Empty;

    /// <summary>Gets the problems of <see cref="VariablesText"/>. While it has an error, the last data that was well-formed applies.</summary>
    public IReadOnlyList<Diagnostic> VariableDiagnostics { get; private set; } = [];

    /// <summary>Gets or sets the policy that the decision is collapsed and projected under.</summary>
    public CollapsePolicy Policy { get; set; } = CollapsePolicy.UnknownAsFalse;

    /// <summary>Gets the value of a term.</summary>
    /// <param name="key">The canonical text of the term.</param>
    /// <returns>The value; <see cref="TruthValue.Unknown"/> when the term has none.</returns>
    public TruthValue ValueOf(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return this.values.GetValueOrDefault(key, TruthValue.Unknown);
    }

    /// <summary>Gives a term a value. <see cref="TruthValue.Unknown"/> removes the value.</summary>
    /// <param name="key">The canonical text of the term.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, TruthValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (value == TruthValue.Unknown)
        {
            _ = this.values.Remove(key);
        }
        else
        {
            this.values[key] = value;
        }
    }

    /// <summary>Moves a term to its next value: <c>Unknown</c> to <c>True</c> to <c>False</c> and back to <c>Unknown</c>.</summary>
    /// <param name="key">The canonical text of the term.</param>
    /// <returns>The new value.</returns>
    public TruthValue Cycle(string key)
    {
        TruthValue next = this.ValueOf(key) switch
        {
            TruthValue.Unknown => TruthValue.True,
            TruthValue.True => TruthValue.False,
            _ => TruthValue.Unknown,
        };
        this.Set(key, next);
        return next;
    }

    /// <summary>Moves the policy to the next one: <c>UnknownAsFalse</c>, <c>UnknownAsTrue</c>, <c>UnknownIsError</c>.</summary>
    /// <returns>The new policy.</returns>
    public CollapsePolicy CyclePolicy()
    {
        this.Policy = this.Policy switch
        {
            CollapsePolicy.UnknownAsFalse => CollapsePolicy.UnknownAsTrue,
            CollapsePolicy.UnknownAsTrue => CollapsePolicy.UnknownIsError,
            _ => CollapsePolicy.UnknownAsFalse,
        };
        return this.Policy;
    }

    /// <summary>Gives every term the value <see cref="TruthValue.Unknown"/>. The variables stay.</summary>
    public void ClearValues()
    {
        this.values.Clear();
    }

    /// <summary>Replaces the values and the variables, for example with the assignment of a saved case.</summary>
    /// <param name="newValues">The value of each term by canonical text.</param>
    /// <param name="variablesText">The variables document, or <see langword="null"/> for none.</param>
    public void Load(IEnumerable<KeyValuePair<string, TruthValue>> newValues, string? variablesText)
    {
        ArgumentNullException.ThrowIfNull(newValues);

        this.values.Clear();
        foreach ((string key, TruthValue value) in newValues)
        {
            this.Set(key, value);
        }

        this.sources = [];
        _ = this.SetVariables(variablesText ?? string.Empty);
    }

    /// <summary>
    /// Replaces the variables document and reads it. A document with an error keeps the data of the last well-formed document
    /// and reports the problem in <see cref="VariableDiagnostics"/>, so the data does not vanish while the author types.
    /// </summary>
    /// <param name="text">The JSON or YAML text.</param>
    /// <returns><see langword="true"/> when the text is well-formed.</returns>
    public bool SetVariables(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        this.VariablesText = text;
        ToolResult<DataSourceSet> parsed = AssignmentVariables.Parse(text);
        this.VariableDiagnostics = [.. parsed.Diagnostics];
        if (!parsed.TryGetValue(out DataSourceSet? next, out _))
        {
            return false;
        }

        this.sources = next;
        return true;
    }

    /// <summary>Reads a variables file (JSON or YAML) in place of the variables document.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The result of reading the file; the diagnostics of the file text itself are in <see cref="VariableDiagnostics"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public ToolResult<SourceText> LoadVariablesFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        ToolResult<SourceText> read = SourceReader.ReadFile(path);
        if (read.TryGetValue(out SourceText? source, out _))
        {
            _ = this.SetVariables(source.Text);
        }

        return read;
    }

    /// <summary>Evaluates the rule of a document under the current values, variables and policy.</summary>
    /// <param name="document">The document. Its last tree that compiled is evaluated.</param>
    /// <param name="cancellationToken">Cancels the evaluation.</param>
    /// <returns>The outcome.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public async Task<AssignmentOutcome> EvaluateAsync(RuleDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Rule is not { } rule)
        {
            return this.Empty(document, "No rule yet: the rule text has errors.", []);
        }

        // The document may show an older tree than its text (the text has errors), so the canonical text of the tree is
        // evaluated, not the text of the editor.
        LoadedRule loaded = RuleLoader
            .LoadString(rule.CanonicalText, "<tree>", new RuleLoadOptions { Format = RuleFormat.Text })
            .Value!;
        JsonObject stubs = [];
        foreach ((string key, TruthValue value) in this.values)
        {
            stubs[key] = value.ToString();
        }

        StubEvaluationOptions options = new()
        {
            Stubs = StubAssignment.Parse(stubs.ToJsonString(), RuleFormat.Json).Value ?? StubAssignment.Empty,
            DataSources = this.sources.Count == 0 ? null : this.sources,
            Evaluation = new EvaluationOptions(IncludeResolvedValues: true),
            Settings = this.settings,
        };
        ToolResult<StubEvaluation> evaluated = await StubEvaluator
            .EvaluateAsync(loaded, options, cancellationToken)
            .ConfigureAwait(false);
        if (!evaluated.TryGetValue(out StubEvaluation? result, out _))
        {
            return this.Empty(document, "The rule could not be evaluated.", [.. evaluated.Diagnostics.Select(d => d.Message)]);
        }

        Decision decision = result.Decision;
        Dictionary<string, string> faultText = [with(StringComparer.Ordinal)];
        foreach (Fault fault in decision.Faults)
        {
            _ = faultText.TryAdd(fault.Term.ToString(), fault.Exception.Message);
        }

        TraceLine trace = Build(decision.TraceTree!, faultText);
        TruthValue? projected = this.Policy switch
        {
            CollapsePolicy.UnknownAsFalse => decision.Project(TruthValue.False),
            CollapsePolicy.UnknownAsTrue => decision.Project(TruthValue.True),
            _ => null,
        };
        return new AssignmentOutcome(
            HasRule: true,
            document.IsRuleCurrent,
            this.TermsOf(rule, trace),
            decision,
            trace,
            trace.Reason,
            [.. decision.Faults.Select(f => $"{f.Term}: {f.Exception.Message}")],
            this.Policy,
            decision.Collapse(this.Policy),
            projected,
            this.VariableDiagnostics
        )
        {
            Evaluation = result,
        };
    }

    private static TraceLine Build(TraceNode node, Dictionary<string, string> faults)
    {
        if (node.NotEvaluated || node.Result is not { } value)
        {
            return new TraceLine(node.Text, null, TraceReasons.Skipped, []);
        }

        if (node.Children.Count == 0)
        {
            string? fault = faults.FirstOrDefault(f => node.Text.StartsWith(f.Key, StringComparison.Ordinal)).Value;
            return new TraceLine(node.Text, value, TraceReasons.ForLeaf(value, fault), []);
        }

        TraceLine[] children = [.. node.Children.Select(child => Build(child, faults))];
        return new TraceLine(
            node.Text,
            value,
            TraceReasons.ForOperator(node.Text, value, [.. children.Select(c => c.Value)]),
            children
        );
    }

    private static void CollectLeaves(TraceLine line, List<string> leaves)
    {
        if (line.Children.Count == 0 && !line.NotEvaluated)
        {
            leaves.Add(line.Text);
        }

        foreach (TraceLine child in line.Children)
        {
            CollectLeaves(child, leaves);
        }
    }

    // The trace names a resolved term as `name(arg: from("source", "query") = value) [from(...): resolved]`. Without the
    // outcome notes and the `from(...) = ` parts, that is the canonical text of the term with the value of the data: the key.
    private static string? ResolvedKey(string display, List<string> leaves)
    {
        foreach (string leaf in leaves)
        {
            string text = OutcomeNotes().Replace(leaf, string.Empty);
            if (Reference().IsMatch(text) && ReferenceAndValue().Replace(text, "${reference}") == display)
            {
                return Reference().Replace(text, string.Empty);
            }
        }

        return null;
    }

    [GeneratedRegex(@"( \[[^\]]*\])+$")]
    private static partial Regex OutcomeNotes();

    [GeneratedRegex(@"(?<reference>from\(""(?:[^""\\]|\\.)*"",\s*""(?:[^""\\]|\\.)*""\)) = (?:""(?:[^""\\]|\\.)*""|[^,)\s]+)")]
    private static partial Regex ReferenceAndValue();

    [GeneratedRegex(@"from\(""(?:[^""\\]|\\.)*"",\s*""(?:[^""\\]|\\.)*""\) = ")]
    private static partial Regex Reference();

    private AssignmentOutcome Empty(RuleDocument document, string reason, IReadOnlyList<string> faults)
    {
        return new AssignmentOutcome(
            HasRule: false,
            document.IsRuleCurrent,
            [],
            null,
            null,
            reason,
            faults,
            this.Policy,
            null,
            null,
            this.VariableDiagnostics
        );
    }

    private List<TermAssignment> TermsOf(CompiledRule<StubAssignment> rule, TraceLine trace)
    {
        List<string> leaves = [];
        CollectLeaves(trace, leaves);
        List<TermAssignment> terms = [];
        foreach (RuleTermList.Entry entry in RuleTermList.Of(rule, this.settings))
        {
            string? key = entry.HasDataReference ? ResolvedKey(entry.Canonical, leaves) : entry.Canonical;
            terms.Add(
                new TermAssignment(
                    entry.Canonical,
                    key,
                    key is null ? TruthValue.Unknown : this.ValueOf(key),
                    entry.HasDataReference
                )
            );
        }

        return terms;
    }
}
