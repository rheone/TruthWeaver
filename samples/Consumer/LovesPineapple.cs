namespace TruthWeaver.Samples.Consumer;

using TruthWeaver.Abstractions;

/// <summary>
/// Shape 1: a class that implements <see cref="IPredicate{TContext}"/>. Use this shape when the
/// predicate needs services from the dependency-injection container, because the container creates
/// the instance for each evaluation.
/// </summary>
public sealed class LovesPineapple : IPredicate<Customer>
{
    /// <summary>Gets the name, label and arguments of the predicate. A rule refers to it as <c>lovesPineapple</c>.</summary>
    public static PredicateSchema Schema =>
        PredicateSchema.NoArguments("lovesPineapple", "Loves pineapple", "Does this customer like pineapple on pizza?");

    /// <inheritdoc />
    public ValueTask<TruthValue> EvaluateAsync(Customer context, PredicateArguments args, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(context.LovesPineapple ? TruthValue.True : TruthValue.False);
    }
}
