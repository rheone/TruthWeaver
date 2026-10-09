namespace TruthWeaver.Samples.Consumer.Tests;

using Microsoft.Extensions.DependencyInjection;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Testing;

public sealed class PizzaRulesTests
{
    /// <summary>
    /// A regular customer who loves pineapple satisfies the offer rule. The rule uses one class-based
    /// predicate and one selector-based predicate, both resolved through the container.
    /// </summary>
    [Fact]
    public async Task PineappleOffer_RegularCustomerWhoLovesPineapple_IsSatisfied_Test()
    {
        ServiceCollection services = new();
        services.AddPizzaRules();
        await using ServiceProvider provider = services.BuildServiceProvider();
        RuleCompiler<Customer> compiler = provider.GetRequiredService<RuleCompiler<Customer>>();
        CompilationResult<Customer> compiled = compiler.Compile(PizzaRules.PineappleOffer);
        Assert.True(compiled.Succeeded);

        Decision decision = await compiled.CompiledRule!.EvaluateAsync(
            new Customer(LovesPineapple: true, OrderCount: 5),
            provider,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied().HaveNoFaults();
    }
}
