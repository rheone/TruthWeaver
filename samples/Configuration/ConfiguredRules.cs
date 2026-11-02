namespace TruthWeaver.Samples.Configuration;

using Microsoft.Extensions.Configuration;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Extensions.Configuration;
using TruthWeaver.Registry;

/// <summary>
/// Rules that live in configuration, not in code. Each key of the <c>Rules</c> section is a rule name and
/// each value is the rule text, so an operator can change a rule without a new build.
/// </summary>
public static class ConfiguredRules
{
    /// <summary>The path of the configuration section that holds the rules.</summary>
    public const string SectionPath = "Rules";

    /// <summary>
    /// Creates the settings of the sample as an in-memory configuration. A real application reads the same
    /// shape from <c>appsettings.json</c>, which reloads on a change.
    /// </summary>
    /// <returns>A configuration with two rules under <see cref="SectionPath"/>.</returns>
    public static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Rules:canCheckout"] = "isVip OR isSmallOrder",
                    ["Rules:canRefund"] = "isVip AND NOT isSmallOrder",
                }
            )
            .Build();
    }

    /// <summary>
    /// Creates the store. It compiles every rule of the section once and keeps the compiled rules until the
    /// configuration reloads. Dispose the store to stop it from reacting to changes.
    /// </summary>
    /// <param name="configuration">The configuration that holds the <see cref="SectionPath"/> section.</param>
    /// <returns>The store of the live rules.</returns>
    public static ConfigurationRuleStore<Order> CreateStore(IConfiguration configuration)
    {
        PredicateRegistry<Order> registry = PredicateRegistry<Order>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("isVip", "Is VIP", "Is the customer a VIP?"),
                (order, _, _) => ValueTask.FromResult(order.IsVip ? TruthValue.True : TruthValue.False)
            )
            .Add(
                PredicateSchema.NoArguments("isSmallOrder", "Is small order", "Is the order total under 50?"),
                (order, _, _) => ValueTask.FromResult(order.Total < 50m ? TruthValue.True : TruthValue.False)
            )
            .Build();

        return new ConfigurationRuleStore<Order>(configuration.GetSection(SectionPath), new RuleCompiler<Order>(registry));
    }
}
