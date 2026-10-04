# Data-Driven Tests

All three data-source attributes feed the same `[Theory]` method; you choose between them by where
the data naturally lives and whether it needs to be computed.

## `[InlineData]`

Use it for small, literal, compile-time-constant values declared right next to the test:

```csharp
[Theory]
[InlineData(2, 3, 5)]
[InlineData(-1, 1, 0)]
[InlineData(0, 0, 0)]
public void Add_ReturnsSum(int a, int b, int expected)
{
    Assert.Equal(expected, a + b);
}
```

Each `[InlineData(...)]` attribute becomes one visible, independently reportable test case in the
runner's output. Arguments must be values a C# attribute can express (primitives, strings, enums,
`null`, `Type`, and one-dimensional arrays of those) — you cannot pass an arbitrary object instance
this way.

## `[MemberData]`

Use it when the data set is a static property, field, or method returning
`IEnumerable<object[]>` (or, from more recent language/xUnit versions,
`IEnumerable<TheoryDataRow>`/`TheoryData<T1, ...>` for stronger typing) — anything too large,
too computed, or too non-literal for `[InlineData]`:

```csharp
public class DiscountTests
{
    public static IEnumerable<object[]> DiscountCases()
    {
        yield return new object[] { 100m, 0m, 100m };
        yield return new object[] { 100m, 25m, 75m };
        yield return new object[] { 200m, 50m, 100m };
    }

    [Theory]
    [MemberData(nameof(DiscountCases))]
    public void ApplyDiscount_ReturnsExpected(decimal subtotal, decimal discountPercent, decimal expected)
    {
        Assert.Equal(expected, subtotal - subtotal * discountPercent / 100m);
    }
}
```

`TheoryData<T1, T2, ...>` is the strongly-typed alternative to `IEnumerable<object[]>` — it
compiles with type checking on each row instead of deferring the mismatch to a runtime cast
failure, and is worth reaching for over a raw `object[]` yield whenever the case count is more than
a handful:

```csharp
public static TheoryData<decimal, decimal, decimal> DiscountCases => new()
{
    { 100m, 0m, 100m },
    { 100m, 25m, 75m },
};

[Theory]
[MemberData(nameof(DiscountCases))]
public void ApplyDiscount_ReturnsExpected(decimal subtotal, decimal discountPercent, decimal expected)
{
    Assert.Equal(expected, subtotal - subtotal * discountPercent / 100m);
}
```

`[MemberData]` can also reference a member on a different type by passing
`MemberType = typeof(OtherClass)`, which is how you share one data set across multiple test
classes.

## `[ClassData]`

Use it when the data-generation logic is substantial enough to deserve its own class — an
`IEnumerable<object[]>` implementation you construct independently of any one test class:

```csharp
public class DiscountTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { 100m, 0m, 100m };
        yield return new object[] { 100m, 25m, 75m };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(DiscountTestData))]
public void ApplyDiscount_ReturnsExpected(decimal subtotal, decimal discountPercent, decimal expected)
{
    Assert.Equal(expected, subtotal - subtotal * discountPercent / 100m);
}
```

xUnit constructs `DiscountTestData` once per theory run and enumerates it, which makes this the
right choice when the data source itself has constructor dependencies or needs to compute values
that don't belong as a bare static member on the test class.

## Picking one

- Handful of literal values, no computation → `[InlineData]`.
- Larger set, computed, or shared across test classes, values are simple → `[MemberData]` backed by
  `TheoryData<T...>`.
- The data source has its own construction logic or dependencies → `[ClassData]`.
