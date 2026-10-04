# Extension Methods and Members for Testing

Extension methods and extension members are a common vehicle for test-authoring DSLs: fluent
assertions, test-data builders, and mock/stub setup helpers. This file is about *writing that
kind of extension* — not about unit-testing the syntax examples elsewhere in this skill. See
[csharp3-extension-methods.md](../references/csharp3-extension-methods.md) and
[csharp14-extension-members.md](../references/csharp14-extension-members.md) for the underlying
syntax rules.

## Basic: custom fluent assertions (classic extension methods, C# 3.0+)

```csharp
public static class AssertionExtensions
{
    public static void ShouldBe<T>(this T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            throw new Xunit.Sdk.XunitException($"Expected {expected}, but got {actual}.");
        }
    }

    public static void ShouldContain(this string actual, string expected) =>
        Assert.Contains(expected, actual);
}
```

```csharp
result.ShouldBe(42);
errorMessage.ShouldContain("timeout");
```

## Basic: test-data builder extensions

Fluent, immutable builders over an object mother's default instance:

```csharp
public static class OrderBuilderExtensions
{
    public static Order WithStatus(this Order order, OrderStatus status) =>
        order with { Status = status };

    public static Order WithLineItem(this Order order, LineItem item) =>
        order with { LineItems = [.. order.LineItems, item] };
}
```

```csharp
Order order = OrderMother.Default()
    .WithStatus(OrderStatus.Pending)
    .WithLineItem(new LineItem("SKU-1", quantity: 2));
```

## Advanced: generic assertion extensions over collections

```csharp
public static class CollectionAssertionExtensions
{
    public static void ShouldContainExactly<T>(this IEnumerable<T> actual, params T[] expected) =>
        Assert.Equal(expected, actual);

    public static void ShouldAllSatisfy<T>(this IEnumerable<T> actual, Action<T> assertion) =>
        Assert.All(actual, item => assertion(item));
}
```

```csharp
orders.ShouldAllSatisfy(o => Assert.Equal(OrderStatus.Pending, o.Status));
```

## Advanced: mock/stub setup extensions

Wraps repetitive `Substitute`/`Mock` configuration into a readable call, returning the mock so
setup chains:

```csharp
public static class OrderRepositoryMockExtensions
{
    public static IOrderRepository WithOrder(this IOrderRepository repo, Order order)
    {
        repo.GetById(order.Id).Returns(order);
        return repo;
    }

    public static IOrderRepository WithNoOrders(this IOrderRepository repo)
    {
        repo.GetAll().Returns([]);
        return repo;
    }
}
```

```csharp
var repository = Substitute.For<IOrderRepository>()
    .WithOrder(existingOrder)
    .WithNoOrders();
```

## Advanced: extension properties as computed test assertions (C# 14+)

An extension property reads more naturally than a `GetXxx()` assertion helper when the check is a
simple derived fact about the result:

```csharp
public static class ResultExtensions
{
    extension(Result result)
    {
        public bool IsSuccessful => result is { IsFailure: false };
        public string FailureSummary => string.Join("; ", result.Errors);
    }
}
```

```csharp
Assert.True(result.IsSuccessful);
Assert.Equal("Invalid email", result.FailureSummary);
```

Fallback below C# 14: a plain `IsSuccessful()` / `GetFailureSummary()` method pair via classic
syntax — see [csharp14-extension-members.md](../references/csharp14-extension-members.md#fallback).

## Generic test-helper extension: constrained object mother

Combine generics with a constraint so a single builder extension works across every type that
opts into a marker interface, instead of one builder method per entity:

```csharp
public interface IHasId<TId>
{
    TId Id { get; }
}

public static class EntityBuilderExtensions
{
    public static T WithId<T, TId>(this T entity, TId id) where T : IHasId<TId> =>
        entity switch
        {
            Order order => (T)(object)(order with { Id = (OrderId)(object)id! }),
            _ => throw new NotSupportedException($"No WithId mapping for {typeof(T)}."),
        };
}
```

In practice, prefer a `with`-expression extension per concrete record type (as in the `Order`
builder above) over a single reflection/cast-heavy generic — the generic form here illustrates the
constraint mechanics, not a recommended production pattern.

## Fallback

Everything except the extension-property section is classic-syntax extension methods and works
from C# 3.0 onward — see [pre-csharp3-no-extensions.md](../references/pre-csharp3-no-extensions.md)
for the .NET Framework 1.0–2.0 static-helper equivalent (a `Testing.OrderShouldBe(actual,
expected)` style call instead of `actual.ShouldBe(expected)`).
