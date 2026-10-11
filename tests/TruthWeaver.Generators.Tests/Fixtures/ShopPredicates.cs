namespace TruthWeaver.Generators.Tests.Fixtures;

using TruthWeaver.Abstractions;

/// <summary>
/// Predicate methods that the generator reads in this project's own build. Together they use every supported argument
/// type, an optional argument of each kind that can have a constant default, the cancellation token and each return
/// type.
/// </summary>
public static partial class ShopPredicates
{
    /// <summary>Answers whether the customer account is active.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <returns><see cref="TruthValue.True"/> for an active account.</returns>
    [Predicate("isActive", "Is active", "Is the customer account active?")]
    public static TruthValue IsActive(Customer customer)
    {
        return Truth(customer.Active);
    }

    /// <summary>Answers whether the customer placed at least the given number of orders.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="minimum">The smallest order count that passes.</param>
    /// <returns><see cref="TruthValue.True"/> when the order count reaches <paramref name="minimum"/>.</returns>
    [Predicate("hasMinimumOrders", "Has minimum orders", "Did the customer place at least the given number of orders?")]
    public static TruthValue HasMinimumOrders(Customer customer, long minimum = 3)
    {
        return Truth(customer.OrderCount >= minimum);
    }

    /// <summary>Answers whether the customer lives in the given country, and optionally in the given region.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="country">The ISO country code.</param>
    /// <param name="region">The region inside the country, or "any".</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns><see cref="TruthValue.True"/> when the address matches.</returns>
    [Predicate("livesIn", "Lives in", "Does the customer live in the given country and region?")]
    public static ValueTask<TruthValue> LivesInAsync(
        Customer customer,
        string country,
        string region = "any",
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool matches = customer.Country == country && (region == "any" || customer.Region == region);
        return ValueTask.FromResult(Truth(matches));
    }

    /// <summary>Answers whether the customer has spent more than an amount.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="amount">The amount to exceed, in the store currency.</param>
    /// <param name="inclusive">When <see langword="true"/>, an equal amount also passes.</param>
    /// <returns><see cref="TruthValue.True"/> when the spend is high enough.</returns>
    [Predicate("hasSpent", "Has spent", "Did the customer spend more than the amount?")]
    public static Task<TruthValue> HasSpentAsync(Customer customer, decimal amount = 100.5m, bool inclusive = false)
    {
        bool passes = inclusive ? customer.Spent >= amount : customer.Spent > amount;
        return Task.FromResult(Truth(passes));
    }

    /// <summary>Answers whether the customer joined before a point in time.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="before">The point in time.</param>
    /// <returns><see cref="TruthValue.True"/> when the customer joined earlier.</returns>
    [Predicate("joinedBefore", "Joined before", "Did the customer join before the given time?")]
    public static TruthValue JoinedBefore(Customer customer, DateTimeOffset before)
    {
        return Truth(customer.Joined < before);
    }

    /// <summary>Answers whether the customer has the given identifier.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="id">The customer identifier.</param>
    /// <returns><see cref="TruthValue.True"/> for the same identifier.</returns>
    [Predicate("isCustomer", "Is customer", "Is this the customer with the given identifier?")]
    public static TruthValue IsCustomer(Customer customer, Guid id)
    {
        return Truth(customer.Id == id);
    }

    /// <summary>Answers whether the customer holds any of the given tags.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="tags">The tags to look for.</param>
    /// <returns><see cref="TruthValue.True"/> when one tag matches.</returns>
    [Predicate("hasAnyTag", "Has any tag", "Does the customer hold any of the given tags?")]
    public static TruthValue HasAnyTag(Customer customer, IReadOnlyList<string> tags)
    {
        return Truth(customer.Tags.Any(tags.Contains));
    }

    private static TruthValue Truth(bool value)
    {
        return value ? TruthValue.True : TruthValue.False;
    }
}
