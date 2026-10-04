# The Builder Pattern as a Test-Authoring Tool

This file is about *using the builder pattern to write tests* — test-data builders (also called
object mothers) that construct fixtures with sensible defaults and overridable properties, cutting
test setup down to the one or two fields a given test actually cares about — not about testing this
skill's own builder syntax examples. A test-data builder is one of the single most common uses of
the builder pattern in real C# codebases: production code increasingly reaches for `required`,
`init`, and records (see
[builder-vs-modern-alternatives.md](builder-vs-modern-alternatives.md)) instead of a builder, but
test code keeps the pattern precisely because tests need the opposite trade-off — a fully-valid
default instance that only *some* tests need to deviate from, which is exactly what mandatory
`required` fields make painful to construct ad hoc in every test.

## Basic: a test-data builder with sensible defaults, overridable per test

```csharp
public sealed class OrderTestBuilder
{
    private string _id = "ORD-DEFAULT";
    private string _customerId = "CUST-DEFAULT";
    private decimal _total = 100m;
    private OrderStatus _status = OrderStatus.Pending;

    public OrderTestBuilder WithId(string id) { _id = id; return this; }
    public OrderTestBuilder WithCustomerId(string customerId) { _customerId = customerId; return this; }
    public OrderTestBuilder WithTotal(decimal total) { _total = total; return this; }
    public OrderTestBuilder WithStatus(OrderStatus status) { _status = status; return this; }

    public Order Build() => new(_id, _customerId, _total, _status);

    public static implicit operator Order(OrderTestBuilder builder) => builder.Build();
}
```

```csharp
[Fact]
public void ApplyDiscount_Test_ReducesTotalByPercentage()
{
    Order order = new OrderTestBuilder().WithTotal(200m).Build();

    Order discounted = DiscountService.Apply(order, percent: 10);

    Assert.Equal(180m, discounted.Total);
}
```

Only `WithTotal` is called — `Id`, `CustomerId`, and `Status` fall back to defaults that are valid
but otherwise irrelevant to this test. This is the core payoff: the test reads as "given an order
with a total of 200, applying a 10% discount yields 180," with no unrelated setup noise for fields
this test doesn't care about. The `implicit operator Order` overload is optional polish — it lets a
call site pass `new OrderTestBuilder().WithTotal(200m)` directly wherever an `Order` is expected,
skipping the explicit `.Build()` when the builder's declared type at the call site makes the intent
unambiguous; omit it if implicit conversions feel too magic for a given codebase's conventions.

## Basic: a generic self-typed test-builder base shared across every fixture type in a test project

```csharp
public abstract class TestBuilder<TSelf, TProduct> where TSelf : TestBuilder<TSelf, TProduct>
{
    public abstract TProduct Build();

    public static implicit operator TProduct(TestBuilder<TSelf, TProduct> builder) => builder.Build();
}

public sealed class CustomerTestBuilder : TestBuilder<CustomerTestBuilder, Customer>
{
    private string _id = "CUST-DEFAULT";
    private string _name = "Test Customer";
    private bool _isActive = true;

    public CustomerTestBuilder WithId(string id) { _id = id; return this; }
    public CustomerTestBuilder Named(string name) { _name = name; return this; }
    public CustomerTestBuilder Inactive() { _isActive = false; return this; }

    public override Customer Build() => new(_id, _name, _isActive);
}
```

```csharp
[Fact]
public void PlaceOrder_Test_RejectsInactiveCustomer()
{
    Customer customer = new CustomerTestBuilder().Inactive().Build();

    Action act = () => OrderService.PlaceOrder(customer, Any.Order());

    Assert.Throws<InactiveCustomerException>(act);
}
```

`TestBuilder<TSelf, TProduct>` — the same CRTP shape from
[generic-self-typed-builder-base.md](generic-self-typed-builder-base.md) — gives every test-data
builder in a project the implicit conversion operator for free, so no individual fixture builder
has to redeclare it. `Inactive()` is a named, intention-revealing toggle rather than a generic
`WithIsActive(false)`, which is the usual style difference between a production builder (exposing
every settable field generically) and a test-data builder (exposing the *scenarios* a test suite
actually needs to express).

## Advanced: composing builders for an aggregate, and an "object mother" facade over common scenarios

```csharp
public sealed class OrderTestBuilder
{
    private CustomerTestBuilder _customer = new();
    private readonly List<LineItemTestBuilder> _lines = new() { new LineItemTestBuilder() };

    public OrderTestBuilder For(CustomerTestBuilder customer) { _customer = customer; return this; }
    public OrderTestBuilder WithLine(LineItemTestBuilder line) { _lines.Add(line); return this; }
    public OrderTestBuilder WithNoLines() { _lines.Clear(); return this; }

    public Order Build() => new(_customer.Build(), _lines.Select(l => l.Build()).ToList());
}

// An "object mother": named factory methods over the generic builder, for the handful of shapes
// most tests reach for repeatedly.
public static class Orders
{
    public static Order Empty() => new OrderTestBuilder().WithNoLines().Build();
    public static Order ForInactiveCustomer() => new OrderTestBuilder().For(new CustomerTestBuilder().Inactive()).Build();
}
```

```csharp
[Theory]
[InlineData(0)]
public void SubmitOrder_Test_RejectsEmptyOrder(int expectedLineCount)
{
    Order order = Orders.Empty();

    Assert.Equal(expectedLineCount, order.Lines.Count);
    Assert.Throws<EmptyOrderException>(() => OrderService.Submit(order));
}
```

`OrderTestBuilder` composes a nested `CustomerTestBuilder` (accepting one via `For(...)`, defaulting
to a fresh one otherwise) — this is the aggregate-root case, where a fixture needs a whole object
graph, not just one flat record. `Orders` (the **object mother**) sits one layer above the raw
builder: it's a small set of named, no-argument factory methods for the scenarios that recur across
a test suite (`Empty()`, `ForInactiveCustomer()`), backed by the builder underneath. Reach for an
object mother once the same builder configuration starts appearing, copy-pasted, across several
test files; keep the underlying builder for the one-off cases a fixed named method can't cover.

## Fallback

Every example on this page needs only generics, method chaining, and (for the implicit-conversion
convenience) user-defined conversion operators — all available from C# 2.0, the tier established in
[../references/csharp2-generic-builders.md](../references/csharp2-generic-builders.md). On a target
predating generics, write each test-data builder as its own non-generic class (no shared
`TestBuilder<TSelf, TProduct>` base, no free implicit-conversion operator per fixture type) — see
[../references/pre-csharp2-classic-builder.md](../references/pre-csharp2-classic-builder.md).
