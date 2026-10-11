namespace TruthWeaver.Generators.Tests.Fixtures;

/// <summary>The application context of the fixture predicates.</summary>
/// <param name="Active">Whether the account is active.</param>
/// <param name="OrderCount">The number of orders.</param>
/// <param name="Country">The ISO country code.</param>
/// <param name="Region">The region inside the country.</param>
/// <param name="Spent">The total spend.</param>
/// <param name="Joined">When the customer joined.</param>
/// <param name="Id">The customer identifier.</param>
/// <param name="Tags">The customer tags.</param>
public sealed record Customer(
    bool Active,
    long OrderCount,
    string Country,
    string Region,
    decimal Spent,
    DateTimeOffset Joined,
    Guid Id,
    IReadOnlyList<string> Tags
);
