namespace TruthWeaver.Samples.PredicateLibrary.Tests;

using TruthWeaver.Abstractions;

public sealed class OrderPredicatesTests
{
    /// <summary>
    /// The arguments for a call come from the schema: the argument name is the declared name. The
    /// delegate answers True when the account has at least the minimum number of orders.
    /// </summary>
    [Fact]
    public async Task MinimumOrders_AccountMeetingTheMinimum_IsTrue_Test()
    {
        var (schema, evaluate) = OrderPredicates.MinimumOrders;
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue> { [schema.Arguments[0].Name] = LiteralValue.OfInt64(3) }
        );

        TruthValue result = await evaluate(
            new Account(IsActive: true, OrderCount: 5),
            args,
            TestContext.Current.CancellationToken
        );

        Assert.Equal("hasMinimumOrders", schema.Name);
        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>A class-based predicate answers Unknown when the context does not know the answer.</summary>
    [Fact]
    public async Task IsActive_AccountWithUnknownFlag_IsUnknown_Test()
    {
        TruthValue result = await new IsActive().EvaluateAsync(
            new Account(IsActive: null, OrderCount: 0),
            PredicateArguments.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, result);
    }
}
