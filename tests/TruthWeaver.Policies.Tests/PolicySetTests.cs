namespace TruthWeaver.Policies.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Pins <see cref="PolicySet{TContext}"/>: each combining algorithm over definite, Unknown and faulting policies, the
/// target and the priority, and the fail-closed decision.
/// </summary>
public class PolicySetTests
{
    private static readonly CompiledRule<object> True = Compile("yes");
    private static readonly CompiledRule<object> False = Compile("no");
    private static readonly CompiledRule<object> Unknown = Compile("maybe");
    private static readonly CompiledRule<object> Faulting = Compile("broken");

    /// <summary>Deny-overrides lets a definite deny win over a definite permit.</summary>
    [Fact]
    public async Task EvaluateAsync_DenyOverridesWithPermitAndDeny_Denies_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Permit("p", True),
            Deny("d", True)
        );

        Assert.Equal(PolicyOutcome.Deny, decision.Outcome);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>Deny-overrides permits when a permit applies and no deny does.</summary>
    [Fact]
    public async Task EvaluateAsync_DenyOverridesWithOnlyPermitApplying_Permits_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Permit("p", True),
            Deny("d", False)
        );

        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
        Assert.Equal(TruthValue.True, decision.Decision.Result);
        Assert.True(decision.IsPermitted);
    }

    /// <summary>A deny that is Unknown never lets a permit through under deny-overrides: the set is Indeterminate and fails closed.</summary>
    [Fact]
    public async Task EvaluateAsync_DenyOverridesWithUnknownDenyAndPermit_IsIndeterminateAndNotPermitted_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Permit("p", True),
            Deny("d", Unknown)
        );

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Assert.Equal(TruthValue.Unknown, decision.Decision.Result);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>A definite deny still wins over an Unknown deny and a permit.</summary>
    [Fact]
    public async Task EvaluateAsync_DenyOverridesWithDefiniteDenyAndUnknownDeny_Denies_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Deny("unknown", Unknown),
            Deny("definite", True),
            Permit("p", True)
        );

        Assert.Equal(PolicyOutcome.Deny, decision.Outcome);
    }

    /// <summary>Permit-overrides lets a definite permit win over a definite deny.</summary>
    [Fact]
    public async Task EvaluateAsync_PermitOverridesWithPermitAndDeny_Permits_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.PermitOverrides,
            null,
            Deny("d", True),
            Permit("p", True)
        );

        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
        Assert.True(decision.IsPermitted);
    }

    /// <summary>Mirror of the deny rule: a permit that is Unknown never lets a deny through under permit-overrides.</summary>
    [Fact]
    public async Task EvaluateAsync_PermitOverridesWithUnknownPermitAndDeny_IsIndeterminate_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.PermitOverrides,
            null,
            Deny("d", True),
            Permit("p", Unknown)
        );

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>Permit-overrides denies when a deny applies and no permit does.</summary>
    [Fact]
    public async Task EvaluateAsync_PermitOverridesWithOnlyDenyApplying_Denies_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.PermitOverrides,
            null,
            Deny("d", True),
            Permit("p", False)
        );

        Assert.Equal(PolicyOutcome.Deny, decision.Outcome);
    }

    /// <summary>When no policy applies the set is not applicable, and the decision is False (default deny).</summary>
    [Theory]
    [InlineData(CombiningAlgorithm.DenyOverrides)]
    [InlineData(CombiningAlgorithm.PermitOverrides)]
    [InlineData(CombiningAlgorithm.FirstApplicable)]
    public async Task EvaluateAsync_NoPolicyApplies_IsNotApplicableAndNotPermitted_Test(CombiningAlgorithm algorithm)
    {
        PolicyDecision decision = await EvaluateAsync(algorithm, null, Permit("p", False), Deny("d", False));

        Assert.Equal(PolicyOutcome.NotApplicable, decision.Outcome);
        Assert.Equal(TruthValue.False, decision.Decision.Result);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>A set without policies is not applicable under every algorithm.</summary>
    [Fact]
    public async Task EvaluateAsync_EmptySet_IsNotApplicable_Test()
    {
        PolicyDecision decision = await EvaluateAsync(CombiningAlgorithm.FirstApplicable);

        Assert.Equal(PolicyOutcome.NotApplicable, decision.Outcome);
        Assert.Empty(decision.Results);
    }

    /// <summary>First-applicable lets the first policy in the order of the set that applies decide, and does not evaluate the rest.</summary>
    [Fact]
    public async Task EvaluateAsync_FirstApplicable_FirstApplyingPolicyDecidesAndStops_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.FirstApplicable,
            null,
            Permit("skipped", False),
            Deny("decides", True),
            Permit("never reached", True)
        );

        Assert.Equal(PolicyOutcome.Deny, decision.Outcome);
        Assert.Equal(["skipped", "decides"], decision.Results.Select(result => result.PolicyId));
    }

    /// <summary>First-applicable stops at a policy that is Unknown: a later permit must not win by skipping it.</summary>
    [Fact]
    public async Task EvaluateAsync_FirstApplicableWithUnknownBeforePermit_IsIndeterminate_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.FirstApplicable,
            null,
            Deny("unknown", Unknown),
            Permit("p", True)
        );

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>A higher priority is evaluated first, so under first-applicable it decides before a policy listed earlier.</summary>
    [Fact]
    public async Task EvaluateAsync_FirstApplicable_HigherPriorityDecidesFirst_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.FirstApplicable,
            null,
            Deny("low", True, priority: 1),
            Permit("high", True, priority: 10)
        );

        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
        Assert.Equal("high", Assert.Single(decision.Results).PolicyId);
    }

    /// <summary>Equal priorities keep the order in which the policies were given.</summary>
    [Fact]
    public void Policies_EqualPriority_KeepGivenOrder_Test()
    {
        PolicySet<object> set = new(
            "set",
            CombiningAlgorithm.FirstApplicable,
            [Permit("a", True), Permit("b", True, priority: 5), Permit("c", True)]
        );

        Assert.Equal(["b", "a", "c"], set.Policies.Select(policy => policy.Id));
    }

    /// <summary>A policy whose target is False is not applicable and its condition is never evaluated.</summary>
    [Fact]
    public async Task EvaluateAsync_PolicyTargetFalse_SkipsConditionAndItsFault_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Deny("d", Faulting, target: False),
            Permit("p", True)
        );

        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
        Assert.Empty(decision.Decision.Faults);
    }

    /// <summary>A policy whose target is Unknown and whose condition is True is Unknown, not applicable.</summary>
    [Fact]
    public async Task EvaluateAsync_PolicyTargetUnknownConditionTrue_IsUnknown_Test()
    {
        PolicyDecision decision = await EvaluateAsync(CombiningAlgorithm.DenyOverrides, null, Deny("d", True, target: Unknown));

        Assert.Equal(TruthValue.Unknown, Assert.Single(decision.Results).Applies);
        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
    }

    /// <summary>Strong Kleene AND: a False condition makes the policy not applicable even when its target is Unknown.</summary>
    [Fact]
    public async Task EvaluateAsync_PolicyTargetUnknownConditionFalse_IsNotApplicable_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Deny("d", False, target: Unknown)
        );

        Assert.Equal(TruthValue.False, Assert.Single(decision.Results).Applies);
        Assert.Equal(PolicyOutcome.NotApplicable, decision.Outcome);
    }

    /// <summary>A set whose target is False is not applicable and evaluates no policy.</summary>
    [Fact]
    public async Task EvaluateAsync_SetTargetFalse_IsNotApplicableWithoutEvaluatingPolicies_Test()
    {
        PolicyDecision decision = await EvaluateAsync(CombiningAlgorithm.DenyOverrides, False, Permit("p", True));

        Assert.Equal(PolicyOutcome.NotApplicable, decision.Outcome);
        Assert.Empty(decision.Results);
    }

    /// <summary>A set whose target is Unknown cannot permit: a permit becomes Indeterminate.</summary>
    [Fact]
    public async Task EvaluateAsync_SetTargetUnknownWithPermit_IsIndeterminate_Test()
    {
        PolicyDecision decision = await EvaluateAsync(CombiningAlgorithm.DenyOverrides, Unknown, Permit("p", True));

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>A rule that faults is Unknown, so a faulting deny blocks a permit, and the fault is kept in the decision.</summary>
    [Fact]
    public async Task EvaluateAsync_FaultingDeny_IsIndeterminateAndKeepsTheFault_Test()
    {
        PolicyDecision decision = await EvaluateAsync(
            CombiningAlgorithm.DenyOverrides,
            null,
            Permit("p", True),
            Deny("d", Faulting)
        );

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Assert.Single(decision.Decision.Faults);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>
    /// A policy condition's quantifier whose collection selector throws is Unknown with a fault recorded against
    /// the collection itself (<c>TermIdentity(collectionName, [])</c>, ADR-0010 decision 4), not against a
    /// predicate term. The set's per-rule fault filter must still carry that fault into the decision, and must
    /// not leak it onto an unrelated policy that never references that collection (ticket 18).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_PolicyConditionCollectionSelectorThrows_IncludesTheFaultAndExcludesItFromAnUnrelatedPolicy_Test()
    {
        PolicyDecision decision = await new PolicySet<CollectionContext>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [
                new Policy<CollectionContext>("p", PolicyEffect.Permit, CompileTrue()),
                new Policy<CollectionContext>("d", PolicyEffect.Deny, CompileExistsOverThrowingSelector()),
            ]
        ).EvaluateAsync(new CollectionContext(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Fault fault = Assert.Single(decision.Decision.Faults);
        Assert.Equal(new TermIdentity("orders", []), fault.Term);
    }

    /// <summary>
    /// A policy condition's quantifier that exceeds <see cref="EvaluationOptions.MaxElementEvaluations"/> is
    /// Unknown with a fault recorded against the collection itself, the same identity shape a throwing selector's
    /// fault uses. The set's per-rule fault filter must carry that fault into the decision too (ticket 18).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_PolicyConditionElementEvaluationLimitExceeded_IncludesTheFaultInTheDecision_Test()
    {
        PolicyDecision decision = await new PolicySet<CollectionContext>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [new Policy<CollectionContext>("d", PolicyEffect.Deny, CompileForAllOverOrders())]
        ).EvaluateAsync(
            new CollectionContext { Orders = [TruthValue.True, TruthValue.True] },
            options: new EvaluationOptions(MaxElementEvaluations: 1),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Fault fault = Assert.Single(decision.Decision.Faults);
        Assert.Equal(new TermIdentity("orders", []), fault.Term);
        Assert.IsType<ElementEvaluationLimitExceededException>(fault.Exception);
    }

    /// <summary>
    /// A predicate shared by two policy conditions is evaluated once per context: the set shares one
    /// <c>EvaluationState</c> across its rules, so the second rule's identical term hits the first rule's memo
    /// entry instead of invoking the predicate again (feature backlog 08). Both rules are compiled against the
    /// same registry (the common case, ticket 34), which is exactly when this sharing applies.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_PredicateSharedByTwoPolicyConditions_IsEvaluatedOnce_Test()
    {
        int[] calls = [0];
        PredicateRegistry<object> registry = SharedRegistry(
            "shared",
            (_, _, _) =>
            {
                calls[0]++;
                return ValueTask.FromResult(TruthValue.True);
            }
        );
        CompiledRule<object> first = CompileSharing(registry, "shared");
        CompiledRule<object> second = CompileSharing(registry, "shared");

        // DenyOverrides never stops early for a Permit policy (it only stops at a definite Deny), so both
        // policies run and the sharing is actually exercised.
        PolicyDecision decision = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Permit("p1", first), Permit("p2", second)]
        ).EvaluateAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, calls[0]);
        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
        Assert.Equal(2, decision.Results.Count);
    }

    /// <summary>
    /// A faulting predicate shared by two policy conditions faults once (the second rule's term hits the memo
    /// without invoking the predicate again), but each policy that references it still lists the fault in the
    /// set's decision, exactly as it would without sharing (feature backlog 08). Both rules are compiled against
    /// the same registry (the common case, ticket 34).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultingPredicateSharedByTwoPolicyConditions_FaultsOnceButEachPolicyListsIt_Test()
    {
        int[] calls = [0];
        PredicateRegistry<object> registry = SharedRegistry(
            "sharedFault",
            (_, _, _) =>
            {
                calls[0]++;
                throw new InvalidOperationException("The service is offline.");
            }
        );
        CompiledRule<object> first = CompileSharing(registry, "sharedFault");
        CompiledRule<object> second = CompileSharing(registry, "sharedFault");

        PolicyDecision decision = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Deny("d1", first), Deny("d2", second)]
        ).EvaluateAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, calls[0]);
        Assert.Equal(2, decision.Decision.Faults.Count);
        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
    }

    /// <summary>
    /// A fault shared by two policy conditions counts once against the set's fault budget, not once per policy
    /// that references it: a third, unrelated policy still runs normally under a budget that only the doubled
    /// count would have reached (feature backlog 08). Both rules are compiled against the same registry (the
    /// common case, ticket 34).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultSharedByTwoPolicyConditions_CountsOnceAgainstTheFaultBudget_Test()
    {
        int[] calls = [0];
        PredicateRegistry<object> registry = SharedRegistry(
            "sharedFault2",
            (_, _, _) =>
            {
                calls[0]++;
                throw new InvalidOperationException("The service is offline.");
            }
        );
        CompiledRule<object> first = CompileSharing(registry, "sharedFault2");
        CompiledRule<object> second = CompileSharing(registry, "sharedFault2");

        PolicyDecision decision = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Deny("d1", first), Deny("d2", second), Permit("p3", True)]
        ).EvaluateAsync(
            new object(),
            options: new EvaluationOptions(FaultBudget: 2),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(1, calls[0]);
        Assert.Equal(TruthValue.True, decision.Results.Single(result => result.PolicyId == "p3").Applies);
    }

    /// <summary>
    /// Ticket 34: two policies' conditions that resolve to the same term identity (<c>isAdmin</c>, no arguments)
    /// but are compiled against different <c>PredicateRegistry</c> instances evaluate independently instead of
    /// one sharing the other's memoized answer. The deny policy's condition is compiled
    /// <see cref="CompilationMode.Lenient"/> against a registry with no <c>isAdmin</c> predicate, so its term
    /// memoizes <see cref="TruthValue.Unknown"/> plus a fault; the permit policy's condition is compiled against a
    /// different registry where <c>isAdmin</c> is registered and always answers <see cref="TruthValue.True"/>.
    /// Before the fix, the memo key ignored the registry, so the permit policy's identical term identity hit the
    /// deny policy's cached Unknown and the set could never permit.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SameTermIdentityCompiledAgainstDifferentRegistries_EachEvaluatesItsOwnPredicate_Test()
    {
        CompiledRule<object> missingAdminCondition = new RuleCompiler<object>(
            PredicateRegistry<object>.CreateBuilder().Build(),
            CompilerOptions.Default with
            {
                Mode = CompilationMode.Lenient,
            }
        )
            .Compile("isAdmin")
            .GetRuleOrThrow();

        PredicateRegistryBuilder<object> adminBuilder = PredicateRegistry<object>.CreateBuilder();
        Add(adminBuilder, FakePredicates.Returning<object>("isAdmin", TruthValue.True));
        CompiledRule<object> hasAdminCondition = new RuleCompiler<object>(adminBuilder.Build())
            .Compile("isAdmin")
            .GetRuleOrThrow();

        // PermitOverrides: a definite permit wins regardless of the deny policy's Unknown, so the assertion below
        // isolates whether the permit policy's own predicate ran (True) or inherited the deny policy's cached
        // Unknown answer (the pre-fix bug).
        PolicyDecision decision = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.PermitOverrides,
            [Deny("missingAdmin", missingAdminCondition), Permit("hasAdmin", hasAdminCondition)]
        ).EvaluateAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.True, decision.Results.Single(result => result.PolicyId == "hasAdmin").Applies);
        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
    }

    /// <summary>
    /// Two policies whose conditions read <c>from("line", "$.qty")</c> inside <c>FORALL(line IN lines, ...)</c>, compiled
    /// against different registries whose <c>lines</c> collections project each element to a different data source, each
    /// read their own element source. The element scope (<c>lines[0]</c>) and the reference are the same for both, so a
    /// data-source answer memo without a registry component would hand the second policy the first policy's answer.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SameElementSourceReferenceUnderDifferentRegistries_EachReadsItsOwnElementSource_Test()
    {
        CompiledRule<object> zeroQuantity = CompileLines(quantity: 0);
        CompiledRule<object> fiveQuantity = CompileLines(quantity: 5);

        PolicyDecision decision = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.PermitOverrides,
            [Deny("zero", zeroQuantity), Permit("five", fiveQuantity)]
        ).EvaluateAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.False, decision.Results.Single(result => result.PolicyId == "zero").Applies);
        Assert.Equal(TruthValue.True, decision.Results.Single(result => result.PolicyId == "five").Applies);
    }

    /// <summary>
    /// A deny condition whose chain a fault-budget abort cuts short does not let a permit through: the faulting
    /// <c>broken</c> inside <c>IsTrue</c> gives a definite <c>False</c>, but the skipped <c>yes</c> counts as
    /// <c>Unknown</c>, so the deny is <c>Unknown</c> and deny-overrides answers Indeterminate.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_DenyOverridesWithDenyConditionCutShortByTheFaultBudget_IsIndeterminate_Test()
    {
        PolicyDecision decision = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Permit("staff", True), Deny("blocked", Compile("IsTrue(broken) OR yes"))]
        ).EvaluateAsync(
            new object(),
            options: new EvaluationOptions(FaultBudget: 1),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(PolicyOutcome.Indeterminate, decision.Outcome);
        Assert.Equal(TruthValue.Unknown, decision.Results.Single(result => result.PolicyId == "blocked").Applies);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>
    /// A shared fault-budget abort makes a deny-overrides outcome depend on priority. The faulting permit runs first
    /// and spends the budget, so the deny answers <c>Unknown</c> and the set is Indeterminate. With the deny first, it
    /// is <c>True</c> before the abort, so the set denies.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SharedFaultBudgetAbort_OutcomeDependsOnPriority_Test()
    {
        EvaluationOptions options = new(FaultBudget: 1);
        PolicyDecision permitFirst = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Permit("staff", Faulting, priority: 10), Deny("blocked", True, priority: 0)]
        ).EvaluateAsync(new object(), options: options, cancellationToken: TestContext.Current.CancellationToken);
        PolicyDecision denyFirst = await new PolicySet<object>(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Permit("staff", Faulting, priority: 0), Deny("blocked", True, priority: 10)]
        ).EvaluateAsync(new object(), options: options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PolicyOutcome.Indeterminate, permitFirst.Outcome);
        Assert.Equal(PolicyOutcome.Deny, denyFirst.Outcome);
    }

    /// <summary>
    /// With <see cref="EvaluationOptions.IncludeTrace"/> the set's decision carries one flat trace of every rule that ran,
    /// in evaluation order, each entry labeled with the rule it came from. A policy whose target is False never runs
    /// its condition, so the condition has no entries.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_IncludeTrace_DecisionCarriesLabeledTraceOfEveryRuleThatRan_Test()
    {
        PolicySet<object> set = new(
            "set",
            CombiningAlgorithm.DenyOverrides,
            [Permit("p", False), Deny("outside", True, target: False), Deny("d", True)],
            True
        );

        PolicyDecision decision = await set.EvaluateAsync(
            new object(),
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotNull(decision.Decision.Trace);
        Assert.Equal(
            ["set target: yes", "policy 'p' condition: no", "policy 'outside' target: no", "policy 'd' condition: yes"],
            decision.Decision.Trace.Entries.Select(entry => entry.Text)
        );
        Assert.Equal(
            [TruthValue.True, TruthValue.False, TruthValue.False, TruthValue.True],
            decision.Decision.Trace.Entries.Select(entry => entry.Result)
        );
    }

    /// <summary>
    /// A term that an earlier rule already evaluated runs once, but each rule that references it still has its own
    /// labeled trace entry for it.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_IncludeTraceWithTermSharedByTwoRules_EachRuleHasAnEntry_Test()
    {
        int calls = 0;
        PredicateRegistry<object> registry = SharedRegistry(
            "shared",
            (_, _, _) =>
            {
                calls++;
                return ValueTask.FromResult(TruthValue.False);
            }
        );
        CompiledRule<object> first = CompileSharing(registry, "shared");
        CompiledRule<object> second = CompileSharing(registry, "shared");
        PolicySet<object> set = new("set", CombiningAlgorithm.DenyOverrides, [Permit("p", first), Deny("d", second)]);

        PolicyDecision decision = await set.EvaluateAsync(
            new object(),
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(1, calls);
        Assert.NotNull(decision.Decision.Trace);
        Assert.Equal(
            ["policy 'p' condition: shared", "policy 'd' condition: shared"],
            decision.Decision.Trace.Entries.Select(entry => entry.Text)
        );
    }

    /// <summary>Without <see cref="EvaluationOptions.IncludeTrace"/> the set's decision has no trace.</summary>
    [Fact]
    public async Task EvaluateAsync_TraceNotRequested_DecisionHasNoTrace_Test()
    {
        PolicySet<object> set = new("set", CombiningAlgorithm.DenyOverrides, [Permit("p", True)], True);

        PolicyDecision decision = await set.EvaluateAsync(
            new object(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Null(decision.Decision.Trace);
    }

    /// <summary>The constructor rejects two policies with the same identifier.</summary>
    [Fact]
    public void Constructor_DuplicatePolicyIds_Throws_Test()
    {
        Assert.Throws<ArgumentException>(() =>
            new PolicySet<object>("set", CombiningAlgorithm.DenyOverrides, [Permit("same", True), Deny("same", True)])
        );
    }

    /// <summary>The constructor rejects an algorithm that is not defined.</summary>
    [Fact]
    public void Constructor_UndefinedAlgorithm_Throws_Test()
    {
        Assert.Throws<ArgumentException>(() => new PolicySet<object>("set", (CombiningAlgorithm)99, []));
    }

    /// <summary>The set copies the policies it is given, so a later change to the source does not change the set.</summary>
    [Fact]
    public async Task Constructor_SourceListChangedAfterwards_DoesNotChangeTheSet_Test()
    {
        List<Policy<object>> source = [Permit("p", True)];
        PolicySet<object> set = new("set", CombiningAlgorithm.DenyOverrides, source);
        source.Add(Deny("d", True));

        PolicyDecision decision = await set.EvaluateAsync(
            new object(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
    }

    /// <summary>A canceled token ends the evaluation with <see cref="OperationCanceledException"/>, not with a decision.</summary>
    [Fact]
    public async Task EvaluateAsync_CanceledToken_Throws_Test()
    {
        PolicySet<object> set = new("set", CombiningAlgorithm.DenyOverrides, [Permit("p", True)]);
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            set.EvaluateAsync(new object(), cancellationToken: cancellation.Token)
        );
    }

    private static CompiledRule<object> Compile(string text)
    {
        PredicateRegistryBuilder<object> builder = PredicateRegistry<object>.CreateBuilder();
        Add(builder, FakePredicates.Returning<object>("yes", TruthValue.True));
        Add(builder, FakePredicates.Returning<object>("no", TruthValue.False));
        Add(builder, FakePredicates.Returning<object>("maybe", TruthValue.Unknown));
        Add(builder, FakePredicates.Faulting<object>("broken", new InvalidOperationException("The service is offline.")));
        return new RuleCompiler<object>(builder.Build()).Compile(text).GetRuleOrThrow();
    }

    private static void Add(
        PredicateRegistryBuilder<object> builder,
        (PredicateSchema Schema, Func<object, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate) fake
    )
    {
        builder.Add(fake.Schema, fake.Evaluate);
    }

    // Builds a registry with one named, no-argument predicate. Two rules compiled separately against the SAME
    // registry instance (CompileSharing below) produce the same term identity (predicate name, no arguments) AND
    // the same registry component of the memo key, so a shared PolicySet evaluation state memoizes across them
    // (the common case, ticket 34).
    private static PredicateRegistry<object> SharedRegistry(
        string predicateName,
        Func<object, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
    )
    {
        PredicateRegistryBuilder<object> builder = PredicateRegistry<object>.CreateBuilder();
        builder.Add(PredicateSchema.NoArguments(predicateName, predicateName, "Registered for a test."), evaluate);
        return builder.Build();
    }

    // Compiles FORALL(line IN lines, qtyAbove(n: from("line", "$.qty"))) against a NEW registry whose one-element
    // "lines" collection projects its element to a data source answering $.qty with the given quantity.
    private static CompiledRule<object> CompileLines(long quantity)
    {
        PredicateRegistryBuilder<object> builder = PredicateRegistry<object>
            .CreateBuilder()
            .AddCollection<string>(
                "lines",
                _ => ["only"],
                elements =>
                    elements.Add(
                        new PredicateSchema(
                            "qtyAbove",
                            "Qty Above",
                            "True when the quantity is above zero.",
                            [new PredicateArgumentSchema("n", "The quantity.", LiteralKind.Int64)]
                        ),
                        (_, args, _) => ValueTask.FromResult(args.GetInt64("n") > 0 ? TruthValue.True : TruthValue.False)
                    ),
                _ => new FakeDataSource().With("$.qty", quantity)
            );
        CompilationResult<object> result = RuleBuilder
            .ForAll("line", "lines", RuleBuilder.Predicate("qtyAbove", ("n", Arg.From("line", "$.qty"))))
            .Compile(new RuleCompiler<object>(builder.Build()));
        return result.GetRuleOrThrow();
    }

    // Compiles a single-term rule naming predicateName against the given registry.
    private static CompiledRule<object> CompileSharing(PredicateRegistry<object> registry, string predicateName)
    {
        return new RuleCompiler<object>(registry).Compile(predicateName).GetRuleOrThrow();
    }

    private static Policy<object> Permit(
        string id,
        CompiledRule<object> condition,
        CompiledRule<object>? target = null,
        int priority = 0
    )
    {
        return new Policy<object>(id, PolicyEffect.Permit, condition, target, priority);
    }

    private static Policy<object> Deny(
        string id,
        CompiledRule<object> condition,
        CompiledRule<object>? target = null,
        int priority = 0
    )
    {
        return new Policy<object>(id, PolicyEffect.Deny, condition, target, priority);
    }

    private static Task<PolicyDecision> EvaluateAsync(
        CombiningAlgorithm algorithm,
        CompiledRule<object>? setTarget = null,
        params Policy<object>[] policies
    )
    {
        return new PolicySet<object>("set", algorithm, policies, setTarget).EvaluateAsync(new object());
    }

    // Compiles a trivial, always-True rule over CollectionContext that references no collection, so a test can
    // pair it with a collection-faulting rule to check the fault is not leaked onto this unrelated policy.
    private static CompiledRule<CollectionContext> CompileTrue()
    {
        (
            PredicateSchema schema,
            Func<CollectionContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
        ) = FakePredicates.Returning<CollectionContext>("yes", TruthValue.True);
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>.CreateBuilder();
        builder.Add(schema, evaluate);
        return new RuleCompiler<CollectionContext>(builder.Build()).Compile("yes").GetRuleOrThrow();
    }

    // An EXISTS quantifier over a collection named "orders" whose selector always throws (ADR-0010 decision 4):
    // evaluating it faults once, against TermIdentity("orders", []), not against the "isOverdue" element term.
    private static CompiledRule<CollectionContext> CompileExistsOverThrowingSelector()
    {
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>
            .CreateBuilder()
            .AddCollection<TruthValue>(
                "orders",
                _ => throw new InvalidOperationException("The orders service is offline."),
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("isOverdue", "isOverdue", "True iff the order is overdue."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<CollectionContext>(builder.Build()).Compile("EXISTS(o IN orders, isOverdue)").GetRuleOrThrow();
    }

    // A FORALL quantifier over a real (non-throwing) "orders" collection, for the MaxElementEvaluations test: every
    // element answers True, so nothing short-circuits the loop before the element limit aborts it.
    private static CompiledRule<CollectionContext> CompileForAllOverOrders()
    {
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>
            .CreateBuilder()
            .AddCollection(
                "orders",
                ctx => ctx.Orders,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("isOverdue", "isOverdue", "True iff the order is overdue."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<CollectionContext>(builder.Build()).Compile("FORALL(o IN orders, isOverdue)").GetRuleOrThrow();
    }

    /// <summary>A context with a collection property, for the quantifier fault tests.</summary>
    private sealed class CollectionContext
    {
        public IReadOnlyList<TruthValue>? Orders { get; init; }
    }
}
