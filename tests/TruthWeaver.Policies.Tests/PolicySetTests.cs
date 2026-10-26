namespace TruthWeaver.Policies.Tests;

using TruthWeaver.Abstractions;
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
    /// A predicate shared by two policy conditions is evaluated once per context: the set shares one
    /// <c>EvaluationState</c> across its rules, so the second rule's identical term hits the first rule's memo
    /// entry instead of invoking the predicate again (feature backlog 08).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_PredicateSharedByTwoPolicyConditions_IsEvaluatedOnce_Test()
    {
        int[] calls = [0];
        CompiledRule<object> first = CompileSharing(
            "shared",
            (_, _, _) =>
            {
                calls[0]++;
                return ValueTask.FromResult(TruthValue.True);
            }
        );
        CompiledRule<object> second = CompileSharing(
            "shared",
            (_, _, _) =>
            {
                calls[0]++;
                return ValueTask.FromResult(TruthValue.True);
            }
        );

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
    /// set's decision, exactly as it would without sharing (feature backlog 08).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultingPredicateSharedByTwoPolicyConditions_FaultsOnceButEachPolicyListsIt_Test()
    {
        int[] calls = [0];
        CompiledRule<object> first = CompileSharing(
            "sharedFault",
            (_, _, _) =>
            {
                calls[0]++;
                throw new InvalidOperationException("The service is offline.");
            }
        );
        CompiledRule<object> second = CompileSharing(
            "sharedFault",
            (_, _, _) =>
            {
                calls[0]++;
                throw new InvalidOperationException("The service is offline.");
            }
        );

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
    /// count would have reached (feature backlog 08).
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultSharedByTwoPolicyConditions_CountsOnceAgainstTheFaultBudget_Test()
    {
        int[] calls = [0];
        CompiledRule<object> first = CompileSharing(
            "sharedFault2",
            (_, _, _) =>
            {
                calls[0]++;
                throw new InvalidOperationException("The service is offline.");
            }
        );
        CompiledRule<object> second = CompileSharing(
            "sharedFault2",
            (_, _, _) =>
            {
                calls[0]++;
                throw new InvalidOperationException("The service is offline.");
            }
        );

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

    // Compiles a single-term rule over one named, no-argument predicate, so the same term identity (predicate
    // name, no arguments) is produced by two different CompiledRule instances compiled from two different
    // registries, and a shared PolicySet evaluation state can memoize across them.
    private static CompiledRule<object> CompileSharing(
        string predicateName,
        Func<object, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
    )
    {
        PredicateRegistryBuilder<object> builder = PredicateRegistry<object>.CreateBuilder();
        builder.Add(PredicateSchema.NoArguments(predicateName, predicateName, "Registered for a test."), evaluate);
        return new RuleCompiler<object>(builder.Build()).Compile(predicateName).GetRuleOrThrow();
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
}
