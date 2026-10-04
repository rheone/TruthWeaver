# Generics for Testing

Generics show up constantly in test-authoring code: parameterized theory data, reusable
assertion helpers, and object-mother builders that work across an entire family of types.

## Basic: `TheoryData<T>` is itself generic

```csharp
public static TheoryData<int, int, int> Add_Test_Data => new()
{
    { 1, 2, 3 },
    { -1, 1, 0 },
    { 0, 0, 0 },
};

[Theory]
[MemberData(nameof(Add_Test_Data))]
public void Add_Test(int a, int b, int expected)
{
    Assert.Equal(expected, Calculator.Add(a, b));
}
```

`TheoryData<T1, T2, T3>` (up to seven type parameters) is a generic collection in the same family
as `Dictionary<TKey, TValue>` — see
[generic-collections-and-linq.md](generic-collections-and-linq.md).

## Basic: a generic assertion helper

```csharp
public static class EquatableAssertions
{
    public static void ShouldRoundTrip<T>(T value) where T : IEquatable<T>
    {
        var serialized = JsonSerializer.Serialize(value);
        var deserialized = JsonSerializer.Deserialize<T>(serialized);
        Assert.True(value.Equals(deserialized));
    }
}
```

```csharp
[Fact]
public void Order_Test_RoundTrips() => EquatableAssertions.ShouldRoundTrip(sampleOrder);
```

One helper, reused across every `IEquatable<T>` type in the codebase, instead of a
type-specific round-trip test per class.

## Advanced: a constrained generic assertion over an ID-bearing family

Rather than a generic *builder* (best done per-record with a `with`-expression extension method
per concrete type), generics pull their weight in *test-side assertions* shared across every type
that opts into a marker interface:

```csharp
public interface IHasId<TId>
{
    TId Id { get; }
}

public static class IdentityAssertions
{
    public static void ShouldHaveDistinctIds<T, TId>(this IEnumerable<T> entities) where T : IHasId<TId>
    {
        var ids = entities.Select(e => e.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }
}
```

```csharp
[Fact]
public void GetAll_Test_ReturnsOrdersWithDistinctIds() =>
    repository.GetAll().ShouldHaveDistinctIds<Order, OrderId>();
```

One assertion, reused for `Order`, `Customer`, or any other `IHasId<TId>` entity — no per-type
duplication of the "no duplicate IDs" check.

## Advanced: generic math in property-based-style boundary tests

```csharp
public static class NumericBoundaryTests
{
    public static void ShouldClampToRange<T>(T value, T min, T max, Func<T, T, T, T> clamp)
        where T : INumber<T>, IComparisonOperators<T, T, bool>
    {
        T result = clamp(value, min, max);
        Assert.True(result >= min && result <= max);
    }
}
```

```csharp
[Theory]
[InlineData(5, 0, 10)]
[InlineData(-5, 0, 10)]
[InlineData(15, 0, 10)]
public void Clamp_Test(int value, int min, int max) =>
    NumericBoundaryTests.ShouldClampToRange(value, min, max, Math.Clamp);
```

One boundary-check helper covers `int`, `double`, `decimal`, and every other `INumber<T>` type
the codebase clamps — see
[generic-math-numeric-abstractions.md](generic-math-numeric-abstractions.md) for the underlying
constraint mechanics (needs C# 11 / .NET 7).

## Fallback

`TheoryData<T>` and generic assertion helpers work from C# 2.0-era generics (xUnit's generic
`TheoryData<T>` type itself is just a `List<object?[]>`-backed generic collection). The generic
math boundary-test example needs C# 11 — write one `ShouldClampToRange` overload per numeric type
below that, following
[csharp11-generic-math-and-attributes.md](../references/csharp11-generic-math-and-attributes.md#fallback).
