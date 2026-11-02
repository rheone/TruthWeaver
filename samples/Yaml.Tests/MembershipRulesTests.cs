namespace TruthWeaver.Samples.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Yaml;

public sealed class MembershipRulesTests
{
    /// <summary>
    /// The rule loaded from YAML is satisfied for an active member who is not suspended.
    /// </summary>
    [Fact]
    public async Task CanDownload_ActiveMemberWhoIsNotSuspended_IsSatisfied_Test()
    {
        CompilationResult<Member> compiled = MembershipRules.Compile();
        Assert.True(compiled.Succeeded);

        Decision decision = await compiled
            .GetRuleOrThrow()
            .EvaluateAsync(
                new Member(IsActive: true, IsSuspended: false),
                cancellationToken: TestContext.Current.CancellationToken
            );

        decision.Should().BeSatisfied().HaveNoFaults();
    }

    /// <summary>
    /// A suspended member does not satisfy the rule loaded from YAML.
    /// </summary>
    [Fact]
    public async Task CanDownload_SuspendedMember_IsNotSatisfied_Test()
    {
        CompilationResult<Member> compiled = MembershipRules.Compile();
        Assert.True(compiled.Succeeded);

        Decision decision = await compiled
            .GetRuleOrThrow()
            .EvaluateAsync(
                new Member(IsActive: true, IsSuspended: true),
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.False(decision.IsSatisfied);
    }

    /// <summary>
    /// A compiled rule prints back as YAML that compiles again.
    /// </summary>
    [Fact]
    public void PrintYaml_CompiledRule_RoundTripsThroughCompileYaml_Test()
    {
        string yaml = MembershipRules.Compile().GetRuleOrThrow().PrintYaml();

        CompilationResult<Member> again = MembershipRules.CreateCompiler().CompileYaml(yaml);

        Assert.True(again.Succeeded);
    }
}
