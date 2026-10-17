namespace TruthWeaver.Policies.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;

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
