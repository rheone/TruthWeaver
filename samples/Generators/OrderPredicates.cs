namespace TruthWeaver.Samples.Generators;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Generators;
using TruthWeaver.Registry;

/// <summary>
/// A predicate written as a plain static method. The <see cref="PredicateAttribute"/> attribute makes the
/// generator add a <c>Register</c> method to this partial type at compile time. The schema comes from the
/// method signature, so the name, the argument and the default are written once.
/// </summary>
public static partial class OrderPredicates
{
    /// <summary>The sample rule in rule text. It uses the default for <c>minimum</c>, which is 3.</summary>
    public const string IsRegular = "hasMinimumOrders";

    /// <summary>Answers whether the customer placed at least the given number of orders.</summary>
    /// <param name="customer">The customer to check.</param>
    /// <param name="minimum">The smallest order count that passes.</param>
    /// <returns><see cref="TruthValue.True"/> when the order count reaches <paramref name="minimum"/>.</returns>
    [Predicate("hasMinimumOrders", "Has minimum orders", "Did the customer place at least the given number of orders?")]
    public static TruthValue HasMinimumOrders(Customer customer, long minimum = 3)
    {
        return customer.OrderCount >= minimum ? TruthValue.True : TruthValue.False;
    }

    /// <summary>
    /// Creates a compiler for rules over <see cref="Customer"/> with the generated <c>Register</c> method.
    /// </summary>
    /// <returns>A compiler that knows <c>hasMinimumOrders</c>.</returns>
    public static RuleCompiler<Customer> CreateCompiler()
    {
        return new RuleCompiler<Customer>(Register(PredicateRegistry<Customer>.CreateBuilder()).Build());
    }
}
