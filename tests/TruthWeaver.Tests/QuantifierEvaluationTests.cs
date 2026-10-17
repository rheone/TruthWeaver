namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
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
    /// Over <see cref="EvaluationOptions.MaxElementEvaluations"/>, the quantifier is <c>Unknown</c> with exactly one
    /// extra fault for the collection and the evaluation aborts — not an exception (ADR-0010 decision 6).
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
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(decision.Trace!.Entries, entry => entry.Text.StartsWith("x[0]: elementTrue", StringComparison.Ordinal));
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

    private static RuleCompiler<LinesContext> CreateLinesCompiler()
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
                line => line.ScriptQty ? new FakeDataSource().With("$.qty", (long)line.Qty) : new FakeDataSource()
            );
        return new RuleCompiler<LinesContext>(builder.Build());
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
