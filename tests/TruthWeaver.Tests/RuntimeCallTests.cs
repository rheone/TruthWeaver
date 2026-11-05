namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 11b-3: <c>call("x")</c> and <c>call("x", hash: "...")</c> keep a node in the compiled tree that evaluates another
/// rule of the <see cref="RuleLibrary"/> at evaluation time, and the analysis treats the node as an opaque term.
/// </summary>
public sealed class RuntimeCallTests
{
    /// <summary>The value of a call is the value of the callee.</summary>
    [Fact]
    public async Task EvaluateAsync_Call_ReturnsTheValueOfTheCallee_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("adult", "a AND NOT b")), "call(\"adult\") AND c");

        Decision decision = await EvaluateRuleAsync(rule);

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A call stays in the compiled tree: the canonical text shows <c>call</c> and not the body of the callee.</summary>
    [Fact]
    public void Compile_Call_KeepsTheCallNodeInTheTree_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("adult", "a AND b")), "call(\"adult\") OR c");

        Assert.Equal("call(\"adult\") OR c", rule.CanonicalText);
        Assert.Equal(rule.CanonicalText, Compile(Library(("adult", "a AND b")), rule.CanonicalText).CanonicalText);
    }

    /// <summary>
    /// A call names the rule as the library holds it and pins in upper case, so calls that differ only in letter case are
    /// one node.
    /// </summary>
    [Fact]
    public void Compile_CallInOtherLetterCase_NamesTheRuleAsTheLibraryHoldsIt_Test()
    {
        RuleLibrary library = Library(("Adult", "a AND b"));
        Assert.True(library.TryGet("adult", out LibraryRule? adult));

        CompiledRule<RuleTestContext> rule = Compile(
            library,
            $"CALL(\"ADULT\", HASH: \"{adult.Hash.ToLowerInvariant()}\") AND call(\"adult\", hash: \"{adult.Hash}\")"
        );

        Assert.Equal(1, rule.Metrics.DistinctTermCount);
        Assert.StartsWith($"call(\"Adult\", hash: \"{adult.Hash}\")", rule.CanonicalText, StringComparison.Ordinal);
    }

    /// <summary>The faults of the callee are in the fault list of the caller's decision.</summary>
    [Fact]
    public async Task EvaluateAsync_CalleeFaults_FaultJoinsTheCallersFaultList_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("risky", "boom AND a")), "call(\"risky\") OR b");

        Decision decision = await EvaluateRuleAsync(rule);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("boom", fault.Term.PredicateName);
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>The trace of the callee nests under the call node, in the trace tree and in the flat trace.</summary>
    [Fact]
    public async Task EvaluateAsync_Call_NestsTheTraceOfTheCalleeUnderTheCallNode_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("adult", "a AND NOT b")), "call(\"adult\")");

        Decision decision = await EvaluateRuleAsync(rule, new EvaluationOptions(IncludeTrace: true));

        TraceNode call = Assert.IsType<TraceNode>(decision.TraceTree);
        Assert.Equal("call(\"adult\")", call.Text);
        Assert.Equal(TruthValue.True, call.Result);
        TraceNode callee = Assert.Single(call.Children);
        Assert.Equal("AND", callee.Text);
        Assert.Equal(["a", "b", "NOT", "AND", "call(\"adult\")"], decision.Trace!.Entries.Select(entry => entry.Text));
    }

    /// <summary>
    /// A library rule that a reload removed makes the call <c>Unknown</c> with one fault, and the decision does not satisfy.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_CalleeRemovedByReload_IsUnknownWithAFaultAndNotSatisfied_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("adult", "a")), "call(\"adult\")");

        Decision decision = await EvaluateRuleAsync(rule, new EvaluationOptions(Library: Library(("other", "a"))));

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("call(\"adult\")", fault.Term.PredicateName);
        Assert.Contains("'adult'", fault.Exception.Message, StringComparison.Ordinal);
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>A pinned call whose library rule a reload changed is <c>Unknown</c> with one fault, naming both hashes.</summary>
    [Fact]
    public async Task EvaluateAsync_PinnedCalleeChangedByReload_IsUnknownWithAFaultAndNotSatisfied_Test()
    {
        RuleLibrary original = Library(("adult", "a"));
        Assert.True(original.TryGet("adult", out LibraryRule? pinned));
        CompiledRule<RuleTestContext> rule = Compile(original, $"call(\"adult\", hash: \"{pinned.Hash}\")");
        RuleLibrary reloaded = Library(("adult", "a AND c"));
        Assert.True(reloaded.TryGet("adult", out LibraryRule? changed));

        Decision decision = await EvaluateRuleAsync(rule, new EvaluationOptions(Library: reloaded));

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Contains(pinned.Hash, fault.Exception.Message, StringComparison.Ordinal);
        Assert.Contains(changed.Hash, fault.Exception.Message, StringComparison.Ordinal);
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>
    /// An unpinned call follows a reload: it runs the current version of the library rule without a recompile of the caller.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_UnpinnedCalleeChangedByReload_RunsTheCurrentVersion_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("adult", "a")), "call(\"adult\")");

        Decision before = await EvaluateRuleAsync(rule);
        Decision after = await EvaluateRuleAsync(rule, new EvaluationOptions(Library: Library(("adult", "b"))));

        Assert.Equal(TruthValue.True, before.Result);
        Assert.Equal(TruthValue.False, after.Result);
        Assert.Empty(after.Faults);
    }

    /// <summary>A reload that makes the callee call itself again is a compile failure of the callee, so the call is <c>Unknown</c> with a fault and does not loop.</summary>
    [Fact]
    public async Task EvaluateAsync_ReloadIntroducesACycle_IsUnknownWithAFault_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("first", "a"), ("second", "b")), "call(\"first\")");
        RuleLibrary cyclic = Library(("first", "call(\"second\")"), ("second", "call(\"first\")"));

        Decision decision = await EvaluateRuleAsync(rule, new EvaluationOptions(Library: cyclic));

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Contains(DiagnosticCodes.LibraryRuleCycleDetected, fault.Exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A callee fault counts against the fault budget of the caller, and the operands after the Nth fault are not evaluated.</summary>
    [Fact]
    public async Task EvaluateAsync_CalleeFault_CountsAgainstTheCallersFaultBudget_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            Library(("first", "boom"), ("second", "bang")),
            "call(\"first\") OR call(\"second\")"
        );

        Decision budgeted = await EvaluateRuleAsync(rule, new EvaluationOptions(FaultBudget: 1));
        Decision unlimited = await EvaluateRuleAsync(rule);

        Assert.Single(budgeted.Faults);
        Assert.True(Assert.IsType<TraceNode>(budgeted.TraceTree).Children[1].NotEvaluated);
        Assert.Equal(2, unlimited.Faults.Count);
    }

    /// <summary>The timeout of the caller's options stops a callee that is still running, and it is not recorded as a fault.</summary>
    [Fact]
    public Task EvaluateAsync_CalleeOutlastsTheCallersTimeout_ThrowsOperationCanceled_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("slowRule", "slow")), "call(\"slowRule\")");

        return Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            EvaluateRuleAsync(rule, new EvaluationOptions(Timeout: TimeSpan.FromMilliseconds(50)))
        );
    }

    /// <summary>The cancellation token of the caller reaches a callee that is still running.</summary>
    [Fact]
    public async Task EvaluateAsync_CallerCancelsWhileTheCalleeRuns_ThrowsOperationCanceled_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("slowRule", "slow")), "call(\"slowRule\")");
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        source.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            rule.EvaluateAsync(new RuleTestContext(), cancellationToken: source.Token)
        );
    }

    /// <summary>Two calls of one rule in one evaluation share the value and record the fault of the callee once.</summary>
    [Fact]
    public async Task EvaluateAsync_TwoCallsOfOneRule_EvaluateTheCalleeOnce_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("risky", "boom")), "call(\"risky\") OR call(\"risky\")");

        Decision decision = await EvaluateRuleAsync(rule);

        Assert.Single(decision.Faults);
    }

    /// <summary>A call to a rule that the library does not hold fails the compile, like a <c>rule("x")</c> reference.</summary>
    [Fact]
    public void Compile_CallToAnUnknownRule_ReportsUnknownLibraryRule_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library(("adult", "a")));

        CompilationResult<RuleTestContext> result = compiler.Compile("call(\"missing\")");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.UnknownLibraryRule, diagnostic.Code);
    }

    /// <summary>A call pinned to a hash that is not the current one fails the compile, like a pinned <c>rule("x")</c> reference.</summary>
    [Fact]
    public void Compile_CallWithAStalePin_ReportsLibraryRuleHashMismatch_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library(("adult", "a")));

        CompilationResult<RuleTestContext> result = compiler.Compile($"call(\"adult\", hash: \"{new string('0', 64)}\")");

        Assert.Null(result.CompiledRule);
        Assert.Equal(DiagnosticCodes.LibraryRuleHashMismatch, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A cycle that goes through a <c>rule</c> reference and a <c>call</c> is one compile error that lists the path.</summary>
    [Fact]
    public void Compile_CycleThroughRuleAndCall_ReportsOneDiagnosticListingThePath_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(
            Library(("first", "rule(\"second\") AND a"), ("second", "b OR call(\"first\")"))
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("c AND call(\"first\")");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.LibraryRuleCycleDetected, diagnostic.Code);
        Assert.Contains("first -> second -> first", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A callee that uses a predicate the caller does not hold fails the compile of the call, naming the library rule.</summary>
    [Fact]
    public void Compile_CalleeUsesAPredicateTheCallerLacks_ReportsInvalidLibraryRule_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library(("adult", "nobody")));

        CompilationResult<RuleTestContext> result = compiler.Compile("call(\"adult\")");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidLibraryRule, diagnostic.Code);
        Assert.Contains("'adult'", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Two calls with the same name and pin are one atom, and a different name is another atom even when the callee text is
    /// the same: the analysis never reads the callee.
    /// </summary>
    [Fact]
    public void Metrics_Calls_AreOpaqueAtomsKeyedByNameAndPin_Test()
    {
        RuleLibrary library = Library(("x", "a"), ("y", "a"));
        Assert.True(library.TryGet("x", out LibraryRule? x));

        CompiledRule<RuleTestContext> sameCall = Compile(library, "call(\"x\") AND NOT call(\"x\")");
        CompiledRule<RuleTestContext> otherName = Compile(library, "call(\"x\") AND NOT call(\"y\")");
        CompiledRule<RuleTestContext> pinned = Compile(library, $"call(\"x\") AND NOT call(\"x\", hash: \"{x.Hash}\")");

        Assert.Equal(1, sameCall.Metrics.DistinctTermCount);
        Assert.Equal(2, otherName.Metrics.DistinctTermCount);
        Assert.Equal(2, pinned.Metrics.DistinctTermCount);
        Assert.NotNull(sameCall.Metrics.BddNodeCount);
    }

    /// <summary>A rewrite keeps a call as it is and does not expand it into the callee.</summary>
    [Fact]
    public void ToCnf_RuleWithCalls_KeepsTheCallsAsLiterals_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("x", "a AND b")), "call(\"x\") OR (c AND NOT call(\"x\"))");

        CompiledRule<RuleTestContext> cnf = rule.ToCnf().CompiledRule!;

        Assert.Contains("call(\"x\")", cnf.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "b",
            cnf.CanonicalText.Replace("call", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
    }

    /// <summary>The coverage of a decision that ran a call lists the call as one term.</summary>
    [Fact]
    public async Task Coverage_RuleWithACall_ListsTheCallAsOneTerm_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("x", "a AND b")), "call(\"x\") OR c");
        Decision decision = await EvaluateRuleAsync(rule);

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, [decision]);

        Assert.Contains(coverage.Terms, term => term.Term == "call(\"x\")");
        Assert.DoesNotContain(coverage.Terms, term => term.Term == "b");
    }

    /// <summary>The equation legend and the truth table give a call one variable or column of its own, named by the call text.</summary>
    [Fact]
    public void PrintTruthTable_RuleWithACall_HasOneColumnForTheCall_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(Library(("x", "a AND b")), "call(\"x\") OR NOT call(\"x\")");

        string table = rule.PrintTruthTable();
        string legend = rule.PrintEquationWithLegend().ToString();

        Assert.Contains("call(\"x\")", table, StringComparison.Ordinal);
        Assert.Equal(3, table.Split('\n').Count(line => line.StartsWith('|')) - 2);
        Assert.Contains("call(\"x\")", legend, StringComparison.Ordinal);
    }

    private static RuleLibrary Library(params (string Name, string Text)[] rules)
    {
        return new RuleLibrary(rules.Select(rule => new KeyValuePair<string, string>(rule.Name, rule.Text)));
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler(RuleLibrary library)
    {
        return new RuleCompiler<RuleTestContext>(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", false)
                .AddConstant("c", true)
                .AddThrowing("boom")
                .AddThrowing("bang")
                .AddDelayed("slow", TimeSpan.FromSeconds(30), true)
                .Build(),
            new CompilerOptions(Library: library)
        );
    }

    private static CompiledRule<RuleTestContext> Compile(RuleLibrary library, string text)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler(library).Compile(text);
        return result.CompiledRule
            ?? throw new InvalidOperationException($"'{text}' did not compile: {result.FormatDiagnostics(text)}");
    }

    private static Task<Decision> EvaluateRuleAsync(CompiledRule<RuleTestContext> rule, EvaluationOptions? options = null)
    {
        return rule.EvaluateAsync(
            new RuleTestContext(),
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
