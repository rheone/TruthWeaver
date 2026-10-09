namespace TruthWeaver.Samples.PredicateLibrary;

using TruthWeaver.Abstractions;

/// <summary>
/// Shape 2: a schema and a delegate, with no class. Use this shape for a predicate that needs no
/// services from the container. The application registers the pair.
/// </summary>
public static class OrderPredicates
{
    /// <summary>The name of the argument that <see cref="MinimumOrders"/> declares.</summary>
    public const string MinimumArgument = "minimum";

    /// <summary>
    /// Gets the schema and the evaluation delegate of <c>hasMinimumOrders(minimum: 3)</c>. The schema
    /// declares one required integer argument, so the compiler rejects a rule that omits it or
    /// passes another type.
    /// </summary>
    public static (
        PredicateSchema Schema,
        Func<Account, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) MinimumOrders { get; } =
        (
            new PredicateSchema(
                "hasMinimumOrders",
                "Has minimum orders",
                "Did this account place at least the given number of orders?",
                [new PredicateArgumentSchema(MinimumArgument, "The smallest order count that passes.", LiteralKind.Int64)]
            ),
            (account, args, _) =>
                ValueTask.FromResult(account.OrderCount >= args.GetInt64(MinimumArgument) ? TruthValue.True : TruthValue.False)
        );
}
