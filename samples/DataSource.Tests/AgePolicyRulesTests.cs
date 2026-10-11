namespace TruthWeaver.Samples.DataSource.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Testing;

public sealed class AgePolicyRulesTests
{
    /// <summary>
    /// The rule reads the minimum age from the policy document. An applicant who meets the minimum
    /// satisfies the rule, and one who does not fails it.
    /// </summary>
    [Fact]
    public async Task MeetsAgePolicy_ApplicantAgainstPolicyMinimum_FollowsThePolicy_Test()
    {
        const string policy = """{ "minAge": 18 }""";
        CancellationToken token = TestContext.Current.CancellationToken;

        Decision adult = await AgePolicyRules.EvaluateAsync(new Applicant(Age: 30), policy, token);
        Decision minor = await AgePolicyRules.EvaluateAsync(new Applicant(Age: 12), policy, token);

        adult.Should().BeSatisfied().HaveNoFaults();
        Assert.False(minor.IsSatisfied);
    }
}
