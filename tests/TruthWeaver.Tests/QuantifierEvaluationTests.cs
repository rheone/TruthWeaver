namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using Arg = TruthWeaver.Building.Arg;

/// <summary>
/// <c>FORALL</c>/<c>EXISTS</c> over a <see cref="PredicateRegistryBuilder{TContext}.AddCollection{TItem}"/> binding
/// (ADR-0010): the Strong Kleene semantics table, the runtime and compile-time limits, the trace shape and
/// <see cref="Fault.Scope"/>, built and evaluated through <see cref="RuleBuilder"/>.
/// </summary>
public sealed class QuantifierEvaluationTests
{
    /// <summary>An empty collection makes <c>FORALL</c> <c>True</c> with no fault (ADR-0010 decision 4: the <c>AND</c> identity).</summary>
    [Fact]
    public async Task ForAll_EmptyCollection_IsTrueWithNoFault_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>An empty collection makes <c>EXISTS</c> <c>False</c> with no fault (ADR-0010 decision 4: the <c>OR</c> identity).</summary>
    [Fact]
    public async Task Exists_EmptyCollection_IsFalseWithNoFault_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileExists(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A <see langword="null"/> collection makes either quantifier <c>Unknown</c> with no fault (ADR-0010 decision 4).</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Quantifier_NullCollection_IsUnknownWithNoFault_Test(bool forAll)
    {
        CompiledRule<QuantifierContext> rule = forAll ? CompileForAll(CreateCompiler()) : CompileExists(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = null },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A throwing selector makes the quantifier <c>Unknown</c> with exactly one fault for the collection (ADR-0010 decision 4).</summary>
    [Fact]
    public async Task ForAll_ThrowingSelector_IsUnknownWithOneFault_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateThrowingSelectorCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.IsType<InvalidOperationException>(fault.Exception);
    }

    /// <summary><c>FORALL</c> is <c>True</c> when every element's body is <c>True</c> (the generalized <c>AND</c>).</summary>
    [Fact]
    public async Task ForAll_EveryElementTrue_IsTrue_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary><c>FORALL</c> is <c>False</c> as soon as one element's body is <c>False</c>, whatever follows it.</summary>
    [Fact]
    public async Task ForAll_OneElementFalse_IsFalse_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.False, TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary><c>FORALL</c> is <c>Unknown</c> when no element is <c>False</c> but at least one is <c>Unknown</c>.</summary>
    [Fact]
    public async Task ForAll_UnknownElementAndNoFalse_IsUnknown_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.Unknown] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary><c>EXISTS</c> is <c>True</c> as soon as one element's body is <c>True</c>.</summary>
    [Fact]
    public async Task Exists_OneElementTrue_IsTrue_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileExists(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.False, TruthValue.True, TruthValue.False] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary><c>EXISTS</c> is <c>False</c> when every element's body is <c>False</c>.</summary>
    [Fact]
    public async Task Exists_EveryElementFalse_IsFalse_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileExists(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.False, TruthValue.False] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary><c>EXISTS</c> is <c>Unknown</c> when no element is <c>True</c> but at least one is <c>Unknown</c>.</summary>
    [Fact]
    public async Task Exists_UnknownElementAndNoTrue_IsUnknown_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileExists(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.False, TruthValue.Unknown] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>
    /// A fault raised inside one element's body is recorded against the whole evaluation's fault budget (ADR-0010
    /// decision 5: "the fault budget is global"), so a budget of 2 aborts after the second faulting element and the
    /// third is never evaluated.
    /// </summary>
    [Fact]
    public async Task ForAll_FaultBudget_IsSharedAcrossElements_Test()
    {
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                ctx => ctx.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("throwsAlways", "throwsAlways", "Always faults."),
                        (_, _, _) => throw new InvalidOperationException("element fault")
                    )
            );
        RuleCompiler<QuantifierContext> compiler = new(builder.Build());
        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "items", RuleBuilder.Predicate("throwsAlways"))
            .Compile(compiler);
        Assert.True(result.Succeeded);

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.True, TruthValue.True] },
            options: new EvaluationOptions(FaultBudget: 2),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(2, decision.Faults.Count);
    }

    /// <summary>
    /// The elements a fault-budget abort skips count as <c>Unknown</c>: the inspection makes the first element's body
    /// definite (<c>False</c> for <c>EXISTS</c>, <c>True</c> for <c>FORALL</c>), but the skipped second element could
    /// still decide the quantifier, so it is <c>Unknown</c>.
    /// </summary>
    /// <param name="text">The quantifier rule.</param>
    [Theory]
    [InlineData("EXISTS(x IN items, IsTrue(throwsAlways))")]
    [InlineData("FORALL(x IN items, IsUnknown(throwsAlways))")]
    public async Task EvaluateAsync_FaultBudgetAbortSkipsTheRemainingElements_FoldsThemAsUnknown_Test(string text)
    {
        CompiledRule<QuantifierContext> rule = CreateCompiler().Compile(text).GetRuleOrThrow();

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.True] },
            options: new EvaluationOptions(FaultBudget: 1),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Single(decision.Faults);
        Assert.True(decision.TraceTree!.Children[1].NotEvaluated);
    }

    /// <summary>
    /// A quantifier written with different casing than its collection's registration (the collection is registered
    /// as "items") gets a trace label matching its canonical text (ultra-review ticket 12): the compiled expression
    /// carries the collection's registered name downstream, the same way a term's label is always its predicate's
    /// registered name rather than however the rule author wrote it.
    /// </summary>
    [Fact]
    public async Task ForAll_CollectionWrittenWithDifferentCasingThanRegistered_TraceLabelMatchesCanonicalText_Test()
    {
        CompiledRule<QuantifierContext> rule = CreateCompiler().Compile("FORALL(x IN Items, elementTrue)").GetRuleOrThrow();

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True] },
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("FORALL(x IN items)", decision.TraceTree!.Text);
        Assert.Equal("FORALL(x IN items, elementTrue)", rule.CanonicalText);
    }

    /// <summary>
    /// A <see cref="RuleCoverage"/> built from a decision of a rule whose quantifier was written with different
    /// casing than its collection's registration no longer throws a trace/rule mismatch (ultra-review ticket 12).
    /// </summary>
    [Fact]
    public async Task ForAll_CollectionWrittenWithDifferentCasingThanRegistered_RuleCoverageDoesNotThrow_Test()
    {
        CompiledRule<QuantifierContext> rule = CreateCompiler().Compile("FORALL(x IN Items, elementTrue)").GetRuleOrThrow();
        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True] },
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, [decision]);

        Assert.Equal(rule.CanonicalText, coverage.Rule);
    }

    /// <summary>
    /// Over <see cref="EvaluationOptions.MaxElementEvaluations"/>, the quantifier is <c>Unknown</c> with exactly one
    /// extra fault for the collection and the evaluation aborts - not an exception (ADR-0010 decision 6).
    /// </summary>
    [Fact]
    public async Task ForAll_OverMaxElementEvaluations_IsUnknownWithOneFault_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());
        TruthValue[] items = [.. Enumerable.Repeat(TruthValue.True, 10)];

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = items },
            options: new EvaluationOptions(MaxElementEvaluations: 3),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.IsType<ElementEvaluationLimitExceededException>(fault.Exception);
    }

    /// <summary>The trace tree records one child per element, labelled <c>variable[index]</c>, with no cap (ADR-0010 decision 7).</summary>
    [Fact]
    public async Task ForAll_Trace_RecordsOneChildPerElement_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.False, TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotNull(decision.TraceTree);
        TraceNode quantifierNode = decision.TraceTree;
        Assert.Equal(3, quantifierNode.Children.Count);
        Assert.Equal("x[0]", quantifierNode.Children[0].Text);
        Assert.Equal("x[1]", quantifierNode.Children[1].Text);

        // FORALL short-circuits at the first False (x[1]); the third element is recorded as not evaluated.
        Assert.False(quantifierNode.Children[0].NotEvaluated);
        Assert.False(quantifierNode.Children[1].NotEvaluated);
        Assert.True(quantifierNode.Children[2].NotEvaluated);
    }

    /// <summary>The flat trace log prefixes each body entry with its element scope path (ADR-0010 decision 7).</summary>
    [Fact]
    public async Task ForAll_Trace_FlatLogPrefixesElementScope_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True] },
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(decision.Trace!.Entries, entry => entry.Text.StartsWith("x[0]: elementTrue", StringComparison.Ordinal));
    }

    /// <summary>
    /// An operator inside a quantifier body adds its own flat trace entry, prefixed with the element scope path like
    /// the body's term entries, between the body's term entry and the quantifier's own entry.
    /// </summary>
    [Fact]
    public async Task ForAll_TraceWithOperatorInBody_FlatLogRecordsTheScopedOperator_Test()
    {
        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "items", RuleBuilder.Not(RuleBuilder.Predicate("elementTrue")))
            .Compile(CreateCompiler());
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True] },
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(3, decision.Trace!.Entries.Count);
        Assert.StartsWith("x[0]: elementTrue", decision.Trace.Entries[0].Text, StringComparison.Ordinal);
        Assert.Equal(new TraceEntry("x[0]: NOT", TruthValue.False, false), decision.Trace.Entries[1]);
        Assert.Equal(new TraceEntry("FORALL(x IN items)", TruthValue.False, false), decision.Trace.Entries[2]);
    }

    /// <summary>
    /// A fault raised inside an element's body carries that element's scope path on <see cref="Fault.Scope"/>
    /// (ADR-0010 decision 7), so it is distinguishable from a fault on the term in general.
    /// </summary>
    [Fact]
    public async Task ForAll_FaultInsideElementBody_CarriesElementScope_Test()
    {
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                ctx => ctx.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("throwsAlways", "throwsAlways", "Always faults."),
                        (_, _, _) => throw new InvalidOperationException("element fault")
                    )
            );
        RuleCompiler<QuantifierContext> compiler = new(builder.Build());
        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "items", RuleBuilder.Predicate("throwsAlways"))
            .Compile(compiler);
        Assert.True(result.Succeeded);

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("x[0]", fault.Scope);
    }

    /// <summary>A fault on the collection itself (a throwing selector) carries the quantifier's own scope, with no element index, and is <see langword="null"/> at the top level.</summary>
    [Fact]
    public async Task ForAll_ThrowingSelector_FaultHasNoScopeAtTopLevel_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll(CreateThrowingSelectorCompiler());

        Decision decision = await rule.EvaluateAsync(
            new QuantifierContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Fault fault = Assert.Single(decision.Faults);
        Assert.Null(fault.Scope);
    }

    /// <summary>
    /// Two quantifiers reusing the same bound variable name over two different collections (ADR-0010 decision 5:
    /// "the scope path is the chain of (binding, index) pairs") must not share memo entries. If the memo scope were
    /// built from the variable name instead of the collection's registered name, the second quantifier's element 0
    /// would wrongly reuse the first quantifier's cached answer for the identical term "elementTrue()", even though
    /// the two elements come from different collections with different values.
    /// </summary>
    [Fact]
    public async Task ForAll_SameVariableNameOverDifferentCollections_DoesNotShareMemoEntries_Test()
    {
        static Action<PredicateRegistryBuilder<Element<TwoCollectionsContext, TruthValue>>> ElementTrue() =>
            elements =>
                elements.Add(
                    PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                    (e, _, _) => ValueTask.FromResult(e.Item)
                );
        PredicateRegistryBuilder<TwoCollectionsContext> builder = PredicateRegistry<TwoCollectionsContext>
            .CreateBuilder()
            .AddCollection("a", ctx => ctx.A, ElementTrue())
            .AddCollection("b", ctx => ctx.B, ElementTrue());
        RuleCompiler<TwoCollectionsContext> compiler = new(builder.Build());
        CompilationResult<TwoCollectionsContext> result = RuleBuilder
            .And(
                RuleBuilder.ForAll("x", "a", RuleBuilder.Predicate("elementTrue")),
                RuleBuilder.ForAll("x", "b", RuleBuilder.Predicate("elementTrue"))
            )
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        // FORALL(x IN a, elementTrue) is True; FORALL(x IN b, elementTrue) is False. A memo scope keyed on the
        // variable "x" rather than the collection would let the second quantifier's element 0 reuse the first
        // quantifier's cached True, making the AND wrongly read True instead of False.
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new TwoCollectionsContext { A = [TruthValue.True], B = [TruthValue.False] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>
    /// Two quantifiers over the same collection binding, bound under two different variable names, share memo
    /// entries for an equal term (ADR-0010 decision 5: "two quantifiers over the same binding in one rule share the
    /// scope path and so share memo entries for equal terms"), so the predicate is invoked once per element rather
    /// than once per quantifier occurrence.
    /// </summary>
    [Fact]
    public async Task ForAll_SameCollectionUnderDifferentVariableNames_SharesMemoEntries_Test()
    {
        int predicateCallCount = 0;
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                ctx => ctx.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("countingTrue", "countingTrue", "Counts calls and returns the element."),
                        (e, _, _) =>
                        {
                            predicateCallCount++;
                            return ValueTask.FromResult(e.Item);
                        }
                    )
            );
        RuleCompiler<QuantifierContext> compiler = new(builder.Build());
        CompilationResult<QuantifierContext> result = RuleBuilder
            .And(
                RuleBuilder.ForAll("x", "items", RuleBuilder.Predicate("countingTrue")),
                RuleBuilder.ForAll("y", "items", RuleBuilder.Predicate("countingTrue"))
            )
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(2, predicateCallCount);
    }

    /// <summary>
    /// A collection selector is called at most once per (binding, outer element) within one evaluation (ADR-0010
    /// decision 5), so two quantifiers over the same collection in one evaluation call the selector once between
    /// them and reuse the materialized list.
    /// </summary>
    [Fact]
    public async Task Quantifiers_SameCollectionInOneEvaluation_MaterializesSelectorOnce_Test()
    {
        int selectorCallCount = 0;
        RuleCompiler<QuantifierContext> compiler = CreateCompiler(ctx =>
        {
            selectorCallCount++;
            return ctx.Items;
        });
        CompilationResult<QuantifierContext> result = RuleBuilder
            .And(
                RuleBuilder.ForAll("x", "items", RuleBuilder.Predicate("elementTrue")),
                RuleBuilder.Exists("y", "items", RuleBuilder.Predicate("elementTrue"))
            )
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        await result.CompiledRule!.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(1, selectorCallCount);
    }

    /// <summary>Nesting a quantifier inside another quantifier's body is allowed and evaluates with the expected semantics (ADR-0010).</summary>
    [Fact]
    public async Task ForAll_NestedExists_EvaluatesBothLevels_Test()
    {
        PredicateRegistryBuilder<GroupsContext> builder = PredicateRegistry<GroupsContext>
            .CreateBuilder()
            .AddCollection(
                "groups",
                ctx => ctx.Groups,
                groupElements =>
                    groupElements.AddCollection(
                        "items",
                        group => group.Item,
                        itemElements =>
                            itemElements.Add(
                                PredicateSchema.NoArguments(
                                    "elementTrue",
                                    "elementTrue",
                                    "True iff the element itself is True."
                                ),
                                (e, _, _) => ValueTask.FromResult(e.Item)
                            )
                    )
            );
        RuleCompiler<GroupsContext> compiler = new(builder.Build());
        CompilationResult<GroupsContext> result = RuleBuilder
            .ForAll("g", "groups", RuleBuilder.Exists("x", "items", RuleBuilder.Predicate("elementTrue")))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        // Every group has at least one True element, so EXISTS is True for each group and FORALL over the groups is True.
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new GroupsContext
            {
                Groups =
                [
                    [TruthValue.False, TruthValue.True],
                    [TruthValue.True],
                ],
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>Nesting a quantifier deeper than <see cref="CompilerOptions.MaxQuantifierDepth"/> is a compile error (ADR-0010 decision 6).</summary>
    [Fact]
    public void Compile_QuantifierNestedDeeperThanMaxQuantifierDepth_ReportsMaxQuantifierDepthExceeded_Test()
    {
        PredicateRegistryBuilder<GroupsContext> builder = PredicateRegistry<GroupsContext>
            .CreateBuilder()
            .AddCollection(
                "groups",
                ctx => ctx.Groups,
                groupElements =>
                    groupElements.AddCollection(
                        "items",
                        group => group.Item,
                        itemElements =>
                            itemElements.Add(
                                PredicateSchema.NoArguments(
                                    "elementTrue",
                                    "elementTrue",
                                    "True iff the element itself is True."
                                ),
                                (e, _, _) => ValueTask.FromResult(e.Item)
                            )
                    )
            );
        RuleCompiler<GroupsContext> compiler = new(builder.Build(), new CompilerOptions(MaxQuantifierDepth: 1));

        CompilationResult<GroupsContext> result = RuleBuilder
            .ForAll("g", "groups", RuleBuilder.Exists("x", "items", RuleBuilder.Predicate("elementTrue")))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxQuantifierDepthExceeded);
    }

    /// <summary>Reusing an enclosing quantifier's own variable name for a nested quantifier is a compile error (ADR-0010 decision 2).</summary>
    [Fact]
    public void Compile_NestedQuantifierReusesEnclosingVariableName_ReportsShadowedVariable_Test()
    {
        PredicateRegistryBuilder<GroupsContext> builder = PredicateRegistry<GroupsContext>
            .CreateBuilder()
            .AddCollection(
                "groups",
                ctx => ctx.Groups,
                groupElements =>
                    groupElements.AddCollection(
                        "items",
                        group => group.Item,
                        itemElements =>
                            itemElements.Add(
                                PredicateSchema.NoArguments(
                                    "elementTrue",
                                    "elementTrue",
                                    "True iff the element itself is True."
                                ),
                                (e, _, _) => ValueTask.FromResult(e.Item)
                            )
                    )
            );
        RuleCompiler<GroupsContext> compiler = new(builder.Build());

        CompilationResult<GroupsContext> result = RuleBuilder
            .ForAll("x", "groups", RuleBuilder.Exists("x", "items", RuleBuilder.Predicate("elementTrue")))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.ShadowedVariable);
    }

    /// <summary>A quantifier head naming a collection no <c>AddCollection</c> call registered is a compile error with a "did you mean" suggestion (ADR-0010 decision 3).</summary>
    [Fact]
    public void Compile_QuantifierOverUnknownCollection_ReportsUnknownCollection_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "itms", RuleBuilder.Predicate("elementTrue"))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownCollection);
        Assert.NotNull(diagnostic.Suggestion);
    }

    /// <summary>A body term not registered on the collection's own element registry is an unknown-predicate error, not resolved against the outer registry.</summary>
    [Fact]
    public void Compile_QuantifierBodyTermNotOnElementRegistry_ReportsUnknownPredicate_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "items", RuleBuilder.Predicate("notRegisteredAnywhere"))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownPredicate);
    }

    /// <summary>
    /// A quantifier body may read the current element with an untyped <c>from(variable, "...")</c> argument as
    /// well as a typed element predicate (ADR-0010 decision 3), when the collection was registered with an
    /// element-to-data-source projection: the per-element value the projection scripts for one element decides the
    /// body for that element, independently of every other element.
    /// </summary>
    [Fact]
    public async Task ForAll_BodyReadsElementWithFrom_UsesThePerElementProjectedSource_Test()
    {
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler();
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5), new LineItem(3)] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>
    /// The per-element projected source stays independent per element: one element whose projected source answers
    /// <c>False</c> makes <c>FORALL</c> <c>False</c>, whatever the other elements answer.
    /// </summary>
    [Fact]
    public async Task ForAll_BodyReadsElementWithFrom_OneElementFalse_IsFalse_Test()
    {
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler();
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5), new LineItem(0)] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>
    /// Inside a <c>FORALL</c> or <c>EXISTS</c> body, a term that reads the element with <c>from(...)</c> shows the
    /// outcome of the reference after the term in both the trace tree and the flat trace, as it does at the root of a
    /// rule (ADR-0006 decision 13).
    /// </summary>
    /// <param name="exists"><see langword="true"/> for <c>EXISTS</c>; <see langword="false"/> for <c>FORALL</c>.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EvaluateAsync_FromReferenceInsideQuantifierBody_TraceShowsTheOutcome_Test(bool exists)
    {
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler();
        RuleBuilder body = RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty")));
        CompilationResult<LinesContext> result = (
            exists ? RuleBuilder.Exists("line", "lines", body) : RuleBuilder.ForAll("line", "lines", body)
        ).Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(0)] },
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        const string Outcome = "[from(\"line\", \"$.qty\"): resolved]";
        TraceNode element = Assert.Single(decision.TraceTree!.Children);
        TraceNode term = Assert.Single(element.Children);
        Assert.EndsWith(Outcome, term.Text, StringComparison.Ordinal);
        Assert.Contains(
            decision.Trace!.Entries,
            entry =>
                entry.Text.StartsWith("line[0]: qtyAbove", StringComparison.Ordinal)
                && entry.Text.EndsWith(Outcome, StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// A query that matches no node in the element's projected source (ADR-0006 decision 6: a scalar argument with
    /// zero matches is missing) makes that element's body <c>Unknown</c> with a fault, not an exception, exactly as
    /// an undeclared-source or missing-match reference does outside a quantifier.
    /// </summary>
    [Fact]
    public async Task ForAll_BodyReadsElementWithFrom_QueryMatchesNoNode_ElementIsUnknownWithFault_Test()
    {
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler();
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5, ScriptQty: false)] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.NotEmpty(decision.Faults);
    }

    /// <summary>
    /// An element-to-data-source projection that throws faults that one element exactly like any other failing data
    /// source (ADR-0001, ADR-0006 decision 9): <c>Unknown</c> plus one fault, not an unhandled exception out of
    /// <c>EvaluateAsync</c>.
    /// </summary>
    [Fact]
    public async Task ForAll_ElementSourceProjectorThrows_ElementIsUnknownWithFault_Test()
    {
        PredicateRegistryBuilder<LinesContext> builder = PredicateRegistry<LinesContext>
            .CreateBuilder()
            .AddCollection(
                "lines",
                ctx => ctx.Lines,
                elements =>
                    elements.Add(
                        new PredicateSchema(
                            "qtyAbove",
                            "Qty Above",
                            "True when the element's quantity is above zero.",
                            [new PredicateArgumentSchema("n", "The threshold.", LiteralKind.Int64)]
                        ),
                        (_, args, _) => ValueTask.FromResult(args.GetInt64("n") > 0 ? TruthValue.True : TruthValue.False)
                    ),
                _ => throw new InvalidOperationException("projection fault")
            );
        RuleCompiler<LinesContext> compiler = new(builder.Build());

        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5)] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        VariableResolutionException resolutionFault = Assert.IsType<VariableResolutionException>(fault.Exception);
        Assert.IsType<InvalidOperationException>(resolutionFault.InnerException);
    }

    /// <summary>
    /// A collection registered with no element-to-data-source projection leaves its bound variable undeclared as a
    /// data source name, so a body that still writes <c>from(variable, ...)</c> against it gets the same
    /// undeclared-data-source diagnostic ADR-0006 already gives any other undeclared name (ADR-0010 decision 3).
    /// </summary>
    [Fact]
    public void Compile_QuantifierBodyFromOnCollectionWithNoElementSource_ReportsUndeclaredDataSource_Test()
    {
        // No elementSource delegate (the 4th AddCollection argument): this collection supports only the typed path.
        PredicateRegistryBuilder<LinesContext> builder = PredicateRegistry<LinesContext>
            .CreateBuilder()
            .AddCollection(
                "lines",
                ctx => ctx.Lines,
                elements =>
                    elements.Add(
                        new PredicateSchema(
                            "qtyAbove",
                            "Qty Above",
                            "True when the element's quantity is above zero.",
                            [new PredicateArgumentSchema("n", "The threshold.", LiteralKind.Int64)]
                        ),
                        (_, args, _) => ValueTask.FromResult(args.GetInt64("n") > 0 ? TruthValue.True : TruthValue.False)
                    )
            );
        RuleCompiler<LinesContext> compiler = new(builder.Build());

        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
    }

    /// <summary>
    /// A <c>let</c> definition used in a quantifier body compiles and evaluates in every format: the use expands in
    /// the body's element scope (ADR-0010 decision 10), so the definition's term resolves in the element registry.
    /// </summary>
    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("yaml")]
    [InlineData("builder")]
    public async Task Compile_DefinitionUsedInsideQuantifierBody_EvaluatesInEveryFormat_Test(string format)
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        CompilationResult<QuantifierContext> result = format switch
        {
            "text" => compiler.Compile("let y = elementTrue; FORALL(x IN items, y)"),
            "json" => compiler.CompileJson(
                """{"definitions":{"y":{"body":{"predicate":"elementTrue"}}},"op":"forall","var":"x","in":"items","operands":[{"op":"use","name":"y"}]}"""
            ),
            "yaml" => compiler.CompileYaml(
                """
                definitions:
                  y:
                    body:
                      predicate: elementTrue
                op: forall
                var: x
                in: items
                operands:
                  - op: use
                    name: y
                """
            ),
            _ => RuleBuilder
                .ForAll("x", "items", RuleBuilder.Use("y"))
                .WithDefinitions(RuleBuilder.Define("y", RuleBuilder.Predicate("elementTrue")))
                .Compile(compiler),
        };

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal("FORALL(x IN items, elementTrue)", result.CompiledRule!.CanonicalText);
        Decision decision = await result.CompiledRule.EvaluateAsync(
            new QuantifierContext { Items = [TruthValue.True, TruthValue.False] },
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>
    /// A definition whose body holds an element-only term, used outside the quantifier, is checked against the outer
    /// registry and gives the ordinary unknown-predicate diagnostic instead of throwing (ADR-0010 decision 10).
    /// </summary>
    [Fact]
    public void Compile_QuantifierDefinitionUsedOutsideTheBody_ReportsUnknownPredicate_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        CompilationResult<QuantifierContext> result = compiler.Compile("let y = elementTrue; FORALL(x IN items, y) AND y");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownPredicate);
    }

    /// <summary>
    /// A body with several distinct <c>from(variable, ...)</c> queries over one element runs the element-to-data-source
    /// projection once for that element, at its first query, not once for each query (ADR-0010 amendment: "the delegate
    /// runs at the first query of that element").
    /// </summary>
    [Fact]
    public async Task ForAll_BodyReadsElementWithSeveralQueries_ProjectsEachElementOnce_Test()
    {
        int projections = 0;
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler(line =>
        {
            projections++;
            return new FakeDataSource().With("$.qty", (long)line.Qty).With("$.min", 1L).With("$.max", 9L);
        });
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll(
                "line",
                "lines",
                RuleBuilder.And(
                    RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))),
                    RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.min"))),
                    RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.max")))
                )
            )
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5), new LineItem(3)] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(2, projections);
    }

    /// <summary>
    /// A projection that throws runs once for the element, however many distinct queries the body makes: the exception
    /// is kept, and each query still becomes its own source-error fault.
    /// </summary>
    [Fact]
    public async Task ForAll_ElementSourceProjectorThrowsWithSeveralQueries_ProjectsOnceAndFaultsEachQuery_Test()
    {
        int projections = 0;
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler(_ =>
        {
            projections++;
            throw new InvalidOperationException("projection fault");
        });
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll(
                "line",
                "lines",
                RuleBuilder.And(
                    RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))),
                    RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.min")))
                )
            )
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5)] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(1, projections);
        Assert.Equal(2, decision.Faults.Count);
    }

    /// <summary>
    /// A body that reads an outer-supplied data source the element binding does not shadow queries that source once
    /// for the whole evaluation, not once for each element (ADR-0006 decision 11: each <c>(source, query)</c> pair is
    /// queried at most once within one evaluation), so every element sees the same answer.
    /// </summary>
    [Fact]
    public async Task ForAll_BodyReadsOuterSource_QueriesItOncePerEvaluation_Test()
    {
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler(options: new CompilerOptions(DataSources: ["cfg"]));
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("cfg", "$.limit"))))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        FakeDataSource cfg = new FakeDataSource().With("$.limit", 1L);

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5), new LineItem(3), new LineItem(7)] },
            dataSources: new DataSources { ["cfg"] = cfg },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(["$.limit"], cfg.Queries);
    }

    /// <summary>
    /// A body that reads the element's own source, under a name that shadows an outer-supplied source, still queries
    /// each element's own projected source: the outer source is never queried and one element's answer never stands in
    /// for another's.
    /// </summary>
    [Fact]
    public async Task ForAll_ElementBindingShadowsOuterSource_QueriesEachElementsOwnSource_Test()
    {
        RuleCompiler<LinesContext> compiler = CreateLinesCompiler(options: new CompilerOptions(DataSources: ["line"]));
        CompilationResult<LinesContext> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        FakeDataSource outer = new FakeDataSource().With("$.qty", 1L);

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new LinesContext { Lines = [new LineItem(5), new LineItem(0)] },
            dataSources: new DataSources { ["line"] = outer },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.Empty(outer.Queries);
    }

    private static RuleCompiler<LinesContext> CreateLinesCompiler(
        Func<LineItem, IDataSource>? projector = null,
        CompilerOptions? options = null
    )
    {
        projector ??= line => line.ScriptQty ? new FakeDataSource().With("$.qty", (long)line.Qty) : new FakeDataSource();
        PredicateRegistryBuilder<LinesContext> builder = PredicateRegistry<LinesContext>
            .CreateBuilder()
            .AddCollection(
                "lines",
                ctx => ctx.Lines,
                elements =>
                    elements.Add(
                        new PredicateSchema(
                            "qtyAbove",
                            "Qty Above",
                            "True when the element's quantity is above zero.",
                            [new PredicateArgumentSchema("n", "The threshold.", LiteralKind.Int64)]
                        ),
                        (_, args, _) => ValueTask.FromResult(args.GetInt64("n") > 0 ? TruthValue.True : TruthValue.False)
                    ),
                projector
            );
        return new RuleCompiler<LinesContext>(builder.Build(), options ?? CompilerOptions.Default);
    }

    private static RuleCompiler<QuantifierContext> CreateCompiler(
        Func<QuantifierContext, IEnumerable<TruthValue>?>? selector = null,
        CompilerOptions? options = null
    )
    {
        selector ??= ctx => ctx.Items;
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                selector,
                elements =>
                    elements
                        .Add(
                            PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                            (e, _, _) => ValueTask.FromResult(e.Item)
                        )
                        .Add(
                            PredicateSchema.NoArguments("throwsAlways", "throwsAlways", "Always faults."),
                            (_, _, _) => throw new InvalidOperationException("element fault")
                        )
            );
        return new RuleCompiler<QuantifierContext>(builder.Build(), options ?? CompilerOptions.Default);
    }

    private static RuleCompiler<QuantifierContext> CreateThrowingSelectorCompiler()
    {
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
            .CreateBuilder()
            .AddCollection<TruthValue>(
                "items",
                _ => throw new InvalidOperationException("selector fault"),
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<QuantifierContext>(builder.Build());
    }

    private static CompiledRule<QuantifierContext> CompileForAll(RuleCompiler<QuantifierContext> compiler)
    {
        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "items", RuleBuilder.Predicate("elementTrue"))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }

    private static CompiledRule<QuantifierContext> CompileExists(RuleCompiler<QuantifierContext> compiler)
    {
        CompilationResult<QuantifierContext> result = RuleBuilder
            .Exists("x", "items", RuleBuilder.Predicate("elementTrue"))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }

    /// <summary>A collection context: a plain list of K3 values, so an element predicate can just read it back.</summary>
    private sealed class QuantifierContext
    {
        public IReadOnlyList<TruthValue>? Items { get; init; }
    }

    /// <summary>A context with two independent collections, for tests that reuse one variable name across them.</summary>
    private sealed class TwoCollectionsContext
    {
        public IReadOnlyList<TruthValue>? A { get; init; }

        public IReadOnlyList<TruthValue>? B { get; init; }
    }

    /// <summary>A nested-collection context: a list of groups, each itself a (nullable) list of K3 values.</summary>
    private sealed class GroupsContext
    {
        public IReadOnlyList<IReadOnlyList<TruthValue>?>? Groups { get; init; }
    }

    /// <summary>A collection context for the untyped element-read tests: a list of order lines.</summary>
    private sealed class LinesContext
    {
        public IReadOnlyList<LineItem>? Lines { get; init; }
    }

    /// <summary>
    /// One order line. <see cref="ScriptQty"/> lets a test build a line whose projected per-element source has no
    /// scripted answer for <c>$.qty</c>, to exercise the "query matches no node" case independently of the value.
    /// </summary>
    private sealed record LineItem(int Qty, bool ScriptQty = true);
}
