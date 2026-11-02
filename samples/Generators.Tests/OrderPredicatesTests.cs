namespace TruthWeaver.Samples.Generators.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;

public sealed class OrderPredicatesTests
{
    /// <summary>
    /// A rule that omits the argument uses the default from the method signature, which is 3 orders.
    /// </summary>
    [Fact]
    public async Task Register_RuleWithoutArgument_UsesTheDefaultFromTheMethodSignature_Test()
    {
        CompilationResult<Customer> compiled = OrderPredicates.CreateCompiler().Compile(OrderPredicates.IsRegular);
        Assert.True(compiled.Succeeded);

        Decision regular = await compiled
            .GetRuleOrThrow()
            .EvaluateAsync(new Customer(OrderCount: 3), cancellationToken: TestContext.Current.CancellationToken);
        Decision newcomer = await compiled
            .GetRuleOrThrow()
            .EvaluateAsync(new Customer(OrderCount: 2), cancellationToken: TestContext.Current.CancellationToken);

        regular.Should().BeSatisfied().HaveNoFaults();
        Assert.False(newcomer.IsSatisfied);
    }

    /// <summary>
    /// A rule can pass the argument by name, and the generated schema accepts it as a number.
    /// </summary>
    [Fact]
    public async Task Register_RuleWithNamedArgument_UsesTheGivenValue_Test()
    {
        CompilationResult<Customer> compiled = OrderPredicates.CreateCompiler().Compile("hasMinimumOrders(minimum: 10)");
        Assert.True(compiled.Succeeded);

        Decision decision = await compiled
            .GetRuleOrThrow()
            .EvaluateAsync(new Customer(OrderCount: 5), cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(decision.IsSatisfied);
    }
}
