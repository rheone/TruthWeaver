namespace TruthWeaver.Samples.Configuration.Tests;

using Microsoft.Extensions.Configuration;
using TruthWeaver.Abstractions;
using TruthWeaver.Extensions.Configuration;

public sealed class ConfiguredRulesTests
{
    /// <summary>
    /// A rule that the configuration names is compiled by the store and evaluates against the order.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_RuleFromConfiguration_DecidesTheOrder_Test()
    {
        using ConfigurationRuleStore<Order> store = ConfiguredRules.CreateStore(ConfiguredRules.CreateConfiguration());

        Decision vip = await store.EvaluateAsync(
            "canCheckout",
            new Order(IsVip: true, Total: 500m),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision regularLarge = await store.EvaluateAsync(
            "canCheckout",
            new Order(IsVip: false, Total: 500m),
            cancellationToken: TestContext.Current.CancellationToken
        );

        vip.Should().BeSatisfied().HaveNoFaults();
        Assert.False(regularLarge.IsSatisfied);
    }

    /// <summary>
    /// The store holds every rule of the section, and the names ignore letter case.
    /// </summary>
    [Fact]
    public void Names_SectionWithTwoRules_ListsBothRules_Test()
    {
        using ConfigurationRuleStore<Order> store = ConfiguredRules.CreateStore(ConfiguredRules.CreateConfiguration());

        Assert.Equal(["canCheckout", "canRefund"], store.Names.Order(StringComparer.OrdinalIgnoreCase));
        Assert.True(store.TryGet("CANREFUND", out _));
    }

    /// <summary>
    /// A rule that the configuration does not hold fails closed: the decision is not satisfied.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_RuleMissingFromConfiguration_IsNotSatisfied_Test()
    {
        IConfiguration empty = new ConfigurationBuilder().Build();
        using ConfigurationRuleStore<Order> store = ConfiguredRules.CreateStore(empty);

        Decision decision = await store.EvaluateAsync(
            "canCheckout",
            new Order(IsVip: true, Total: 1m),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.False(decision.IsSatisfied);
    }
}
