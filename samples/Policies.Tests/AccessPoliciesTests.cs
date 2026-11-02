namespace TruthWeaver.Samples.Policies.Tests;

using TruthWeaver.Analysis;
using TruthWeaver.Policies;

public sealed class AccessPoliciesTests
{
    /// <summary>
    /// A staff member who is not blocked is permitted by the <c>staff</c> policy.
    /// </summary>
    [Fact]
    public async Task Create_StaffMemberWhoIsNotBlocked_IsPermitted_Test()
    {
        PolicySet<Request> set = AccessPolicies.Create();

        PolicyDecision decision = await set.EvaluateAsync(
            new Request(IsStaff: true, IsPremium: false, IsBlocked: false),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(PolicyOutcome.Permit, decision.Outcome);
        Assert.True(decision.IsPermitted);
    }

    /// <summary>
    /// The <c>blocked</c> policy has the highest priority, so it denies a blocked staff member before the
    /// <c>staff</c> policy is tried.
    /// </summary>
    [Fact]
    public async Task Create_BlockedStaffMember_IsDeniedByThePolicyWithTheHigherPriority_Test()
    {
        PolicySet<Request> set = AccessPolicies.Create();

        PolicyDecision decision = await set.EvaluateAsync(
            new Request(IsStaff: true, IsPremium: true, IsBlocked: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(PolicyOutcome.Deny, decision.Outcome);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>
    /// No policy applies to a requester who is not staff and not blocked.
    /// </summary>
    [Fact]
    public async Task Create_RequesterWhoMatchesNoPolicy_IsNotApplicable_Test()
    {
        PolicySet<Request> set = AccessPolicies.Create();

        PolicyDecision decision = await set.EvaluateAsync(
            new Request(IsStaff: false, IsPremium: true, IsBlocked: false),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(PolicyOutcome.NotApplicable, decision.Outcome);
        Assert.False(decision.IsPermitted);
    }

    /// <summary>
    /// The analysis reports the <c>premium-staff</c> policy as shadowed, because the <c>staff</c> policy
    /// before it applies to every request that <c>premium-staff</c> applies to. The other policies are not shadowed.
    /// </summary>
    [Fact]
    public void Analyze_PolicyCoveredByAnEarlierPolicy_IsReportedAsShadowed_Test()
    {
        PolicySet<Request> set = AccessPolicies.Create();

        RuleSetAnalysisResult analysis = AccessPolicies.Analyze(set);

        Assert.Equal(RuleSetAnalysisOutcome.Decided, analysis.Outcome);
        string[] shadowed = [.. analysis.Shadowing.Where(rule => rule.IsShadowed).Select(rule => set.Policies[rule.Index].Id)];
        Assert.Equal(["premium-staff"], shadowed);
    }
}
