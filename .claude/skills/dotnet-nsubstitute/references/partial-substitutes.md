# Partial Substitutes

## `Substitute.ForPartsOf<T>()`

A regular `Substitute.For<T>()` class substitute never runs the class's real member logic — every
member returns a default until you configure it. `Substitute.ForPartsOf<T>()` builds a substitute
that runs the class's **real** implementation for every member by default, letting you override
only the specific members a given test needs to control:

```csharp
public class PricingService
{
    public virtual decimal GetBasePrice(string sku) => 100m; // pretend this hits a database

    public decimal GetDiscountedPrice(string sku, decimal discountPercent)
    {
        var basePrice = GetBasePrice(sku);
        return basePrice - basePrice * discountPercent / 100m;
    }
}

[Fact]
public void GetDiscountedPrice_AppliesPercentageToBasePrice()
{
    var pricing = Substitute.ForPartsOf<PricingService>();
    pricing.Configure().GetBasePrice(Arg.Any<string>()).Returns(200m);

    var result = pricing.GetDiscountedPrice("sku-1", discountPercent: 10);

    Assert.Equal(180m, result);
}
```

`GetDiscountedPrice` runs its real logic (unconfigured), while `GetBasePrice` returns the configured
stub value instead of its real implementation — this is the shape you reach for when you want to
test one method's real logic in combination with a stubbed-out collaborator method on the *same*
class, rather than extracting that collaborator into a separate interface purely for testability.

## `.Configure()`

Note the `.Configure()` call before `.GetBasePrice(...)` above — because a `ForPartsOf<T>()`
substitute runs real logic by default, NSubstitute needs an explicit signal that the next call is a
configuration call rather than a real invocation that happens to look like one. Omitting
`.Configure()` on a partial substitute calls the *real* method instead of registering a stub, which
is the most common mistake when adopting partial substitutes. This requirement is specific to
`ForPartsOf<T>()` — a full `Substitute.For<T>()` substitute never runs real logic, so it never needs
`.Configure()` before a `Returns` call.

## When to reach for a partial substitute

Prefer extracting a real dependency behind an interface and substituting that interface with
`Substitute.For<T>()` whenever you can — it keeps the test double's boundary clean and avoids the
`.Configure()` gotcha entirely. Reach for `ForPartsOf<T>()` specifically when the method you want to
stub and the method you want to test for real live on the same class and splitting them apart isn't
practical (a legacy class, a virtual method that exists purely as a template-method seam).

## Constructor arguments

Like `Substitute.For<T>()` on a class, `ForPartsOf<T>()` passes any constructor arguments the class
needs after the type argument:

```csharp
var pricing = Substitute.ForPartsOf<PricingService>(dependencyA, dependencyB);
```
