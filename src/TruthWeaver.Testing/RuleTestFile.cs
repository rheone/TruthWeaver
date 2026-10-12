namespace TruthWeaver.Testing;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>
/// A rule test file: one rule and named cases, each with stub values for the terms, optional data for the
/// <c>from(...)</c> references and the expected result. A rule author writes it in YAML with no C#, and a C# test or the
/// <c>truthweaver test</c> command runs it.
/// </summary>
/// <remarks>
/// <para>The rule is inline (<c>rule</c>: rule text or a YAML rule tree) or in a file (<c>ruleFile</c>: a path relative to the
/// test file; <c>.json</c> is JSON, <c>.yaml</c> and <c>.yml</c> are YAML, any other extension is rule text). The rule
/// compiles over stand-in predicates (<see cref="StubRuleCompiler"/>), so no host registry is needed.</para>
/// <para>The JSON schema of the file is <c>rule-test.schema.json</c>, in the <c>content</c> folder of the package.</para>
/// </remarks>
public sealed class RuleTestFile
{
    /// <summary>Initializes a new instance of the <see cref="RuleTestFile"/> class.</summary>
    /// <param name="name">The name of the file, the suite name in a JUnit report.</param>
    /// <param name="ruleSource">The rule in <paramref name="ruleFormat"/>.</param>
    /// <param name="ruleFormat">The format of <paramref name="ruleSource"/>.</param>
    /// <param name="ruleFile">The full path of the rule file, or <see langword="null"/> for an inline rule.</param>
    /// <param name="cases">The cases, in file order.</param>
    internal RuleTestFile(
        string name,
        string ruleSource,
        RuleFormat ruleFormat,
        string? ruleFile,
        IReadOnlyList<RuleTestCase> cases
    )
    {
        this.Name = name;
        this.RuleSource = ruleSource;
        this.RuleFormat = ruleFormat;
        this.RuleFile = ruleFile;
        this.Cases = cases;
    }

    /// <summary>Gets the name of the file: its <c>name</c> key, or the file name without its extension.</summary>
    public string Name { get; }

    /// <summary>Gets the rule, as the file or the rule file gives it.</summary>
    public string RuleSource { get; }

    /// <summary>Gets the format of <see cref="RuleSource"/>.</summary>
    public RuleFormat RuleFormat { get; }

    /// <summary>Gets the full path of the rule file, or <see langword="null"/> when the rule is inline.</summary>
    public string? RuleFile { get; }

    /// <summary>Gets the cases, in file order.</summary>
    public IReadOnlyList<RuleTestCase> Cases { get; }

    /// <summary>Reads a rule test file from YAML text.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <param name="baseDirectory">The directory that <c>ruleFile</c> and <c>variableFiles</c> paths are relative to, or <see langword="null"/> for the current directory.</param>
    /// <param name="file">The file when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="errors">Every problem, each with its path and place in the text; empty on success.</param>
    /// <returns><see langword="true"/> when the file is usable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml"/> is <see langword="null"/>.</exception>
    public static bool TryParse(
        string yaml,
        string? baseDirectory,
        [NotNullWhen(true)] out RuleTestFile? file,
        out IReadOnlyList<RuleTestFileError> errors
    )
    {
        ArgumentNullException.ThrowIfNull(yaml);

        file = RuleTestFileReader.Read(yaml, baseDirectory ?? Environment.CurrentDirectory, "rule-test", out errors);
        return file is not null;
    }

    /// <summary>Reads a rule test file from disk. The file name without its extension is the name when the file has no <c>name</c> key.</summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="file">The file when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="errors">Every problem; empty on success. A file that cannot be read is one problem.</param>
    /// <returns><see langword="true"/> when the file is usable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static bool TryLoad(
        string path,
        [NotNullWhen(true)] out RuleTestFile? file,
        out IReadOnlyList<RuleTestFileError> errors
    )
    {
        ArgumentNullException.ThrowIfNull(path);

        string fullPath = Path.GetFullPath(path);
        string yaml;
        try
        {
            yaml = File.ReadAllText(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            file = null;
            errors = [new RuleTestFileError($"The file '{fullPath}' cannot be read: {ex.GetType().Name}.", "$", 0, 0)];
            return false;
        }

        file = RuleTestFileReader.Read(
            yaml,
            Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory,
            Path.GetFileNameWithoutExtension(fullPath),
            out errors
        );
        return file is not null;
    }

    /// <summary>Loads a rule test file and runs every case: the one call a C# test needs.</summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="options">The compile limits, or <see langword="null"/> for <see cref="CompilerOptions.Default"/>.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The report. Call <see cref="RuleTestReport.ShouldPass"/> to fail a test on a failing case.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="RuleTestException">The file is not usable. The message lists each problem. <see cref="TryLoad"/> gives them without an exception.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the run.</exception>
    public static Task<RuleTestReport> RunAsync(
        string path,
        CompilerOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryLoad(path, out RuleTestFile? file, out IReadOnlyList<RuleTestFileError> errors))
        {
            throw new RuleTestException(
                $"The rule test file '{path}' is not usable:\n" + string.Join("\n", errors.Select(e => "- " + e))
            );
        }

        return file.RunAsync(options, cancellationToken);
    }

    /// <summary>
    /// Compiles the rule over stand-in predicates and runs every case. When the rule does not compile, every case fails and
    /// <see cref="RuleTestReport.RuleDiagnostics"/> holds the reasons.
    /// </summary>
    /// <param name="options">The compile limits, or <see langword="null"/> for <see cref="CompilerOptions.Default"/>.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The report, with one result for each case in file order.</returns>
    /// <exception cref="OperationCanceledException">The token cancelled the run.</exception>
    public async Task<RuleTestReport> RunAsync(CompilerOptions? options = null, CancellationToken cancellationToken = default)
    {
        CompilationResult<StubValues> compiled = StubRuleCompiler.Compile(this.RuleSource, this.RuleFormat, options);
        if (!compiled.TryGetRule(out CompiledRule<StubValues>? rule, out EquatableArray<Diagnostic> diagnostics))
        {
            RuleTestResult[] failed =
            [
                .. this.Cases.Select(c => new RuleTestResult(
                    c,
                    null,
                    null,
                    ["The rule does not compile."],
                    RuleCompiles: false
                )),
            ];
            return new RuleTestReport(this.Name, diagnostics, failed);
        }

        RuleTerms terms = RuleTerms.Of(rule);
        List<RuleTestResult> results = [];
        foreach (RuleTestCase testCase in this.Cases)
        {
            results.Add(await RunCaseAsync(rule, terms, testCase, cancellationToken).ConfigureAwait(false));
        }

        return new RuleTestReport(this.Name, diagnostics, results);
    }

    private static async Task<RuleTestResult> RunCaseAsync(
        CompiledRule<StubValues> rule,
        RuleTerms terms,
        RuleTestCase testCase,
        CancellationToken cancellationToken
    )
    {
        DataSourceSet sources = [];
        foreach ((string name, IDataSource source) in testCase.Variables)
        {
            sources.Add(name, source);
        }

        Decision decision = await rule.EvaluateAsync(testCase.Given, dataSources: sources, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        string trace = rule.PrintPlainText(decision);

        List<string> failures = [];

        // A given term that the rule never looks up can never take effect, so it is a mistake in the case. The rule names
        // each predicate in one spelling, and a stand-in finds a value only by that spelling and those arguments.
        foreach (string term in testCase.Given.Values.Keys)
        {
            if (terms.Problem(term, rule.PredicateNames) is { } problem)
            {
                failures.Add(problem);
            }
        }

        RuleTestExpectation expect = testCase.Expect;
        if (decision.Result != expect.Result)
        {
            failures.Add($"Expected the result {expect.Result}, but the result is {decision.Result}.");
        }

        if (expect.Faults is { } faults && decision.Faults.Count != faults)
        {
            failures.Add($"Expected {faults} fault(s), but the evaluation recorded {decision.Faults.Count}.");
        }

        if (expect.Trace is { } expectedTrace && NormalizeTrace(expectedTrace) != NormalizeTrace(trace))
        {
            failures.Add(
                "The trace is not the expected trace.\nExpected:\n"
                    + NormalizeTrace(expectedTrace)
                    + "\nActual:\n"
                    + NormalizeTrace(trace)
            );
        }

        return new RuleTestResult(testCase, decision, trace, failures, RuleCompiles: true);
    }

    /// <summary>Normalizes a trace for comparison: LF line endings, no spaces at the end of a line, no empty lines at the end.</summary>
    private static string NormalizeTrace(string trace)
    {
        IEnumerable<string> lines = trace.ReplaceLineEndings("\n").Split('\n').Select(line => line.TrimEnd());
        return string.Join("\n", lines).TrimEnd('\n');
    }

    /// <summary>
    /// The terms that a rule can look up: the canonical text of each term with only literal arguments, and the predicate
    /// names of the terms with a <c>from(...)</c> argument, whose looked-up term is known only when the data answers.
    /// </summary>
    private sealed class RuleTerms
    {
        private readonly HashSet<string> literalTerms = [with(StringComparer.Ordinal)];
        private readonly HashSet<string> referencePredicates = [with(StringComparer.Ordinal)];

        /// <summary>Collects the terms of a rule from its JSON tree.</summary>
        public static RuleTerms Of(CompiledRule<StubValues> rule)
        {
            RuleTerms terms = new();
            terms.Visit(JsonNode.Parse(rule.PrintJson(includeNotes: false)));
            return terms;
        }

        /// <summary>Gives the reason that a given term can never take effect, or <see langword="null"/> when the rule can look it up.</summary>
        public string? Problem(string term, IReadOnlySet<string> predicateNames)
        {
            string predicate = PredicateOf(term);
            if (this.literalTerms.Contains(term) || this.referencePredicates.Contains(predicate))
            {
                return null;
            }

            if (predicateNames.Contains(predicate))
            {
                string[] known = [.. this.literalTerms.Where(t => PredicateOf(t) == predicate).Order(StringComparer.Ordinal)];
                return $"The given term '{term}' is not a term of the rule. The rule uses '{predicate}' as {string.Join(", ", known)}.";
            }

            string? spelling = predicateNames.FirstOrDefault(n =>
                string.Equals(n, predicate, StringComparison.OrdinalIgnoreCase)
            );
            return spelling is null
                ? $"The given term '{term}' uses the predicate '{predicate}', which the rule does not use."
                : $"The given term '{term}' spells the predicate '{predicate}', but the rule spells it '{spelling}'.";
        }

        /// <summary>Gives the predicate name of a canonical term: the text before its argument list.</summary>
        private static string PredicateOf(string term)
        {
            int open = term.IndexOf('(', StringComparison.Ordinal);
            return open < 0 ? term : term[..open];
        }

        private void Visit(JsonNode? node)
        {
            if (node is not JsonObject tree)
            {
                return;
            }

            if (tree["predicate"] is JsonValue name)
            {
                this.VisitTerm(name.GetValue<string>(), tree["args"] as JsonObject ?? []);
                return;
            }

            foreach (JsonNode? operand in tree["operands"] as JsonArray ?? [])
            {
                this.Visit(operand);
            }
        }

        private void VisitTerm(string predicate, JsonObject arguments)
        {
            // The JSON tree shows a from(...) argument as an object, and a literal as a value or a list.
            if (arguments.Any(argument => argument.Value is JsonObject))
            {
                this.referencePredicates.Add(predicate);
                return;
            }

            // The term alone, compiled the same way as a key of the stub values, gives the canonical text of the key.
            JsonObject term = new() { ["predicate"] = predicate, ["args"] = arguments.DeepClone() };
            if (
                StubRuleCompiler.TryCompile(term.ToJsonString(), RuleFormat.Json, out CompiledRule<StubValues>? compiled, out _)
            )
            {
                this.literalTerms.Add(compiled.CanonicalText);
            }
        }
    }
}
