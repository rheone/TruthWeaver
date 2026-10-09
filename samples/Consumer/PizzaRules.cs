namespace TruthWeaver.Samples.Consumer;

using Microsoft.Extensions.DependencyInjection;
using TruthWeaver.Abstractions;
using TruthWeaver.DependencyInjection;
using TruthWeaver.Predicates;

/// <summary>The sample rule and the container wiring that evaluates it.</summary>
public static class PizzaRules
{
    /// <summary>
    /// The sample rule in rule text. It is satisfied for a regular customer who loves pineapple.
    /// </summary>
    public const string PineappleOffer = "lovesPineapple AND isRegular";

    /// <summary>
    /// Registers the predicates and the rule compiler for <see cref="Customer"/>.
    /// </summary>
    /// <param name="services">The container to add to.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddPizzaRules(this IServiceCollection services)
    {
        // The registry only records the type of a class-based predicate. The container must also
        // be able to create it.
        services.AddTransient<LovesPineapple>();

        return services.AddTruthWeaver<Customer>(registry =>
        {
            registry.Add<LovesPineapple>();

            // Shape 2: a selector and a test. No class is needed. Use this shape when the answer
            // comes from one value that is safe to read without container services.
            (PredicateSchema schema, var evaluate) = SelectedValuePredicates.Create<Customer, int>(
                "isRegular",
                "Is regular",
                "Did this customer place at least three orders?",
                select: (customer, _, _) => ValueTask.FromResult(customer.OrderCount),
                test: orderCount => orderCount >= 3 ? TruthValue.True : TruthValue.False
            );
            registry.Add(schema, evaluate);
        });
    }
}
