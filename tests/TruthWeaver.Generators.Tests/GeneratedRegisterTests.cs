namespace TruthWeaver.Generators.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Generators.Tests.Fixtures;
using TruthWeaver.Registry;

/// <summary>
/// Checks the <c>Register</c> method that the generator wrote for <see cref="ShopPredicates"/> in this project's build
/// against the same predicates registered by hand.
/// </summary>
public sealed class GeneratedRegisterTests
{
    private static readonly Customer Alice = new(
        Active: true,
        OrderCount: 4,
        Country: "NZ",
        Region: "Otago",
        Spent: 250m,
        Joined: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Id: new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
        Tags: ["gold"]
    );

    /// <summary>
    /// The generated registration holds the same schemas as a hand-written registration of the same methods: names,
    /// labels, descriptions, and for each argument the name, description, kind, required flag and default.
    /// </summary>
    [Fact]
    public void Register_ShopPredicates_SchemasEqualHandWrittenRegistration_Test()
    {
        PredicateRegistry<Customer> generated = ShopPredicates.Register(PredicateRegistry<Customer>.CreateBuilder()).Build();

        PredicateRegistry<Customer> handWritten = HandWritten().Build();

        Assert.Equal(Describe(handWritten), Describe(generated));
    }

    /// <summary>
    /// A rule over the generated registration evaluates each predicate through the marked method: arguments, defaults,
    /// the cancellation token and each return type reach the method.
    /// </summary>
    [Fact]
    public async Task Register_RuleOverGeneratedPredicates_EvaluatesThroughTheMarkedMethods_Test()
    {
        RuleCompiler<Customer> compiler = new(ShopPredicates.Register(PredicateRegistry<Customer>.CreateBuilder()).Build());
        const string rule = """
            isActive AND hasMinimumOrders AND NOT hasMinimumOrders(minimum: 5) AND livesIn(country: "NZ")
            AND livesIn(country: "NZ", region: "Otago") AND hasSpent AND NOT hasSpent(amount: 250, inclusive: false)
            AND hasSpent(amount: 250, inclusive: true) AND joinedBefore(before: "2021-01-01T00:00:00Z")
            AND isCustomer(id: "6f9619ff-8b86-d011-b42d-00c04fc964ff") AND hasAnyTag(tags: ["silver", "gold"])
            """;

        CompilationResult<Customer> compiled = compiler.Compile(rule);
        Assert.True(compiled.Succeeded, string.Join("; ", compiled.Diagnostics));
        Decision decision = await compiled.CompiledRule!.EvaluateAsync(
            Alice,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    private static PredicateRegistryBuilder<Customer> HandWritten()
    {
        return PredicateRegistry<Customer>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("isActive", "Is active", "Is the customer account active?"),
                (c, _, _) => ValueTask.FromResult(ShopPredicates.IsActive(c))
            )
            .Add(
                new PredicateSchema(
                    "hasMinimumOrders",
                    "Has minimum orders",
                    "Did the customer place at least the given number of orders?",
                    [
                        new PredicateArgumentSchema(
                            "minimum",
                            "The smallest order count that passes.",
                            LiteralKind.Int64,
                            Required: false,
                            Default: LiteralValue.OfInt64(3)
                        ),
                    ]
                ),
                (c, a, _) => ValueTask.FromResult(ShopPredicates.HasMinimumOrders(c, a.GetInt64("minimum")))
            )
            .Add(
                new PredicateSchema(
                    "livesIn",
                    "Lives in",
                    "Does the customer live in the given country and region?",
                    [
                        new PredicateArgumentSchema("country", "The ISO country code.", LiteralKind.String),
                        new PredicateArgumentSchema(
                            "region",
                            "The region inside the country, or \"any\".",
                            LiteralKind.String,
                            Required: false,
                            Default: LiteralValue.OfString("any")
                        ),
                    ]
                ),
                (c, a, t) => ShopPredicates.LivesInAsync(c, a.GetString("country"), a.GetString("region"), t)
            )
            .Add(
                new PredicateSchema(
                    "hasSpent",
                    "Has spent",
                    "Did the customer spend more than the amount?",
                    [
                        new PredicateArgumentSchema(
                            "amount",
                            "The amount to exceed, in the store currency.",
                            LiteralKind.Decimal,
                            Required: false,
                            Default: LiteralValue.OfDecimal(100.5m)
                        ),
                        new PredicateArgumentSchema(
                            "inclusive",
                            "When true, an equal amount also passes.",
                            LiteralKind.Boolean,
                            Required: false,
                            Default: LiteralValue.OfBoolean(false)
                        ),
                    ]
                ),
                (c, a, _) =>
                    new ValueTask<TruthValue>(ShopPredicates.HasSpentAsync(c, a.GetDecimal("amount"), a.GetBool("inclusive")))
            )
            .Add(
                new PredicateSchema(
                    "joinedBefore",
                    "Joined before",
                    "Did the customer join before the given time?",
                    [new PredicateArgumentSchema("before", "The point in time.", LiteralKind.DateTimeOffset)]
                ),
                (c, a, _) => ValueTask.FromResult(ShopPredicates.JoinedBefore(c, a.GetDateTimeOffset("before")))
            )
            .Add(
                new PredicateSchema(
                    "isCustomer",
                    "Is customer",
                    "Is this the customer with the given identifier?",
                    [new PredicateArgumentSchema("id", "The customer identifier.", LiteralKind.Guid)]
                ),
                (c, a, _) => ValueTask.FromResult(ShopPredicates.IsCustomer(c, a.GetGuid("id")))
            )
            .Add(
                new PredicateSchema(
                    "hasAnyTag",
                    "Has any tag",
                    "Does the customer hold any of the given tags?",
                    [new PredicateArgumentSchema("tags", "The tags to look for.", LiteralKind.StringArray)]
                ),
                (c, a, _) => ValueTask.FromResult(ShopPredicates.HasAnyTag(c, a.GetStringArray("tags")))
            );
    }

    /// <summary>
    /// Writes every schema of a registry as one line per predicate, sorted by name. <see cref="PredicateSchema"/> is a
    /// record, but its argument list compares by reference, so the schemas are compared through this text.
    /// </summary>
    private static string[] Describe(PredicateRegistry<Customer> registry)
    {
        return
        [
            .. registry
                .Schemas.OrderBy(s => s.Name, StringComparer.Ordinal)
                .Select(s =>
                    $"{s.Name} | {s.Label} | {s.Description} | "
                    + string.Join(
                        " ; ",
                        s.Arguments.Select(a => $"{a.Name}, {a.Description}, {a.Type}, {a.Required}, {a.Default}")
                    )
                ),
        ];
    }
}
