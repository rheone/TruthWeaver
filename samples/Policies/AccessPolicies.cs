namespace TruthWeaver.Samples.Policies;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Policies;
using TruthWeaver.Registry;

/// <summary>
/// A policy set with the <see cref="CombiningAlgorithm.FirstApplicable"/> algorithm, and the rule-set
/// analysis that finds a policy no request can reach.
/// </summary>
public static class AccessPolicies
{
    /// <summary>
    /// Builds the policy set. The policies are tried in priority order, and the first one that applies decides.
    /// The <c>premium-staff</c> policy is a mistake that the sample keeps on purpose: <c>staff</c> already
    /// covers every request that <c>premium-staff</c> covers.
    /// </summary>
    /// <returns>The policy set.</returns>
    public static PolicySet<Request> Create()
    {
        RuleCompiler<Request> compiler = CreateCompiler();

        return new PolicySet<Request>(
            "access",
            CombiningAlgorithm.FirstApplicable,
            [
                new Policy<Request>("blocked", PolicyEffect.Deny, Compile(compiler, "isBlocked"), priority: 10),
                new Policy<Request>("staff", PolicyEffect.Permit, Compile(compiler, "isStaff"), priority: 5),
                new Policy<Request>("premium-staff", PolicyEffect.Permit, Compile(compiler, "isStaff AND isPremium")),
            ]
        );
    }

    /// <summary>
    /// Analyzes the conditions of the policies in evaluation order. A <see cref="RuleShadowing"/> entry with
    /// <see cref="RuleShadowing.IsShadowed"/> set means the earlier policies decide every request that the
    /// policy at that index could decide.
    /// </summary>
    /// <param name="set">The policy set to analyze.</param>
    /// <returns>The analysis. Its indexes are positions in <see cref="PolicySet{TContext}.Policies"/>.</returns>
    public static RuleSetAnalysisResult Analyze(PolicySet<Request> set)
    {
        return RuleSetAnalysis.Analyze([.. set.Policies.Select(policy => policy.Condition)]);
    }

    private static CompiledRule<Request> Compile(RuleCompiler<Request> compiler, string rule)
    {
        return compiler.Compile(rule).GetRuleOrThrow();
    }

    private static RuleCompiler<Request> CreateCompiler()
    {
        PredicateRegistry<Request> registry = PredicateRegistry<Request>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("isStaff", "Is staff", "Is the requester a staff member?"),
                (request, _, _) => ValueTask.FromResult(request.IsStaff ? TruthValue.True : TruthValue.False)
            )
            .Add(
                PredicateSchema.NoArguments("isPremium", "Is premium", "Does the requester have a premium plan?"),
                (request, _, _) => ValueTask.FromResult(request.IsPremium ? TruthValue.True : TruthValue.False)
            )
            .Add(
                PredicateSchema.NoArguments("isBlocked", "Is blocked", "Is the requester blocked?"),
                (request, _, _) => ValueTask.FromResult(request.IsBlocked ? TruthValue.True : TruthValue.False)
            )
            .Build();

        return new RuleCompiler<Request>(registry);
    }
}
