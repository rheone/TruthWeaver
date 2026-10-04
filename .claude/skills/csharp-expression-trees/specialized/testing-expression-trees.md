# Expression Trees as a Test-Authoring Tool

This file is about *using expression trees to write tests* — extracting a member name from a
lambda so a test helper never hard-codes a property name as a string, building dynamic assertion
matchers, and expression-based mock/stub argument matching — not about testing this skill's own
expression-tree syntax examples. See
[csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md)
for the underlying `Expression<TDelegate>` mechanics these patterns build on.

## Basic: extracting a property name from `Expression<Func<T, TProperty>>` to avoid magic strings

```csharp
public static class PropertyName
{
    public static string Of<T, TProperty>(Expression<Func<T, TProperty>> propertyAccessor)
    {
        if (propertyAccessor.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        // a value-type property access is wrapped in a Convert node (boxing to object)
        if (propertyAccessor.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary
            && unary.Operand is MemberExpression innerMember)
        {
            return innerMember.Member.Name;
        }

        throw new ArgumentException($"'{propertyAccessor}' is not a simple property access.");
    }
}
```

```csharp
[Fact]
public void ValidationError_Test_ReportsCorrectPropertyName()
{
    ValidationResult result = Validator.Validate(new Order { Total = -5m });

    Assert.Contains(PropertyName.Of<Order, decimal>(o => o.Total), result.ErrorsByField.Keys);
}
```

`PropertyName.Of<T, TProperty>` is a generic helper: `T` is the type under test, `TProperty` is
inferred from whichever property the lambda accesses, and the compiler checks at compile time that
`o.Total` really is a member of `Order` — a refactoring rename of `Total` breaks the build instead
of silently leaving a stale `"Total"` string literal behind, which is the entire motivation for
reaching for an expression tree here instead of a plain string parameter. The `UnaryExpression`
branch handles value-type properties (`decimal`, `int`, ...), whose access gets implicitly wrapped
in a boxing `Convert` node when the lambda's declared return type is a reference type or object —
skipping it is a common bug in a first attempt at this helper.

## Basic: asserting two objects differ only in named properties

```csharp
public static class PropertyAssert
{
    public static void OnlyDiffersIn<T>(T expected, T actual, params Expression<Func<T, object?>>[] allowedDiffs)
    {
        HashSet<string> allowedNames = allowedDiffs.Select(PropertyName.Of).ToHashSet();

        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            if (allowedNames.Contains(property.Name))
            {
                continue;
            }

            Assert.Equal(property.GetValue(expected), property.GetValue(actual));
        }
    }
}
```

```csharp
[Fact]
public void UpdateShippingAddress_Test_OnlyChangesAddressFields()
{
    Order before = Order.Sample();
    Order after = OrderService.UpdateShippingAddress(before, newAddress);

    PropertyAssert.OnlyDiffersIn(before, after,
        o => o.ShippingAddress, o => o.LastModifiedAt);
}
```

A `params Expression<Func<T, object?>>[]` parameter lets a test list exactly the properties a
mutation is *expected* to touch, by lambda rather than by string — every other property on `T` is
asserted unchanged automatically, so a bug that accidentally mutates an unrelated field fails the
test even though nobody wrote an assertion naming that field specifically.

## Advanced: a mocking-library-style expression matcher

```csharp
public sealed class ArgMatcher<T>
{
    private readonly Func<T, bool> _predicate;
    private readonly string _description;

    private ArgMatcher(Expression<Func<T, bool>> predicate)
    {
        _predicate = predicate.Compile();
        _description = predicate.Body.ToString();
    }

    public static ArgMatcher<T> Matching(Expression<Func<T, bool>> predicate) => new(predicate);

    public bool Matches(T value) => _predicate(value);
    public override string ToString() => _description;
}
```

```csharp
[Fact]
public void CheckoutService_Test_ChargesGatewayWithOrderTotal()
{
    var gateway = new Mock<IPaymentGateway>();
    var checkout = new CheckoutService(gateway.Object);

    checkout.Complete(new Order { Id = "ORD-1", Total = 42m });

    gateway.Verify(g => g.Charge(
        It.Is<ChargeRequest>(r => ArgMatcher<ChargeRequest>.Matching(x => x.Amount == 42m).Matches(r))));
}
```

Taking `Expression<Func<T, bool>>` rather than `Func<T, bool>` for the matcher's predicate — even
though only `.Compile()`'s resulting delegate is ever invoked here — buys a readable failure
message for free: `_description` captures the *source* of the predicate (`x.Amount == 42m`) via
`predicate.Body.ToString()`, so a failed match reports what was expected in something resembling
the original C#, not just "predicate returned false." This is the same trick real mocking
libraries' `It.Is<T>(Expression<Func<T,bool>>)` overloads use internally.

## Advanced: comparing two `Expression<Func<T, TResult>>` trees for structural equality

```csharp
public static class ExpressionAssert
{
    public static void AreEquivalent(Expression expected, Expression actual)
    {
        Assert.Equal(Normalize(expected), Normalize(actual));

        static string Normalize(Expression expression) =>
            new ParameterNameEraser().Visit(expression)!.ToString();
    }

    private sealed class ParameterNameEraser : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            Expression.Parameter(node.Type, "p");
    }
}
```

```csharp
[Fact]
public void BuildTotalGreaterThan_Test_ProducesExpectedTree()
{
    Expression<Func<Order, bool>> built = QueryBuilder.BuildTotalGreaterThan(100m);

    ExpressionAssert.AreEquivalent(
        (Expression<Func<Order, bool>>)(o => o.Total > 100m),
        built);
}
```

Directly asserting `built.ToString() == "o => (o.Total > 100)"` is brittle — the parameter name a
manually-built tree happens to use (`"x"` vs. `"o"`) shouldn't make an otherwise-identical tree fail
the test. `ParameterNameEraser` (an `ExpressionVisitor` subclass — see
[expression-visitor-and-tree-rewriting.md](expression-visitor-and-tree-rewriting.md)) normalizes
every parameter to the same name before comparing string representations, so this asserts on the
tree's actual *shape*, not on incidental naming. This pattern is specifically useful for testing
code whose whole job is to *build* expression trees, like a dynamic query helper — the "assert
against the expected code, not just the expected runtime behavior" case a delegate-returning
equivalent couldn't cover at all, since two different `Func<>` delegates are never comparable for
equivalence at all.

## Fallback

Every example above needs `ExpressionVisitor` (for `ParameterNameEraser`) or nothing more than
`Expression<TDelegate>`/`MemberExpression`/`UnaryExpression` from the C# 3.0 baseline
(`PropertyName.Of`, `PropertyAssert.OnlyDiffersIn`, `ArgMatcher<T>`) — see
[csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md).
The one example needing `ExpressionVisitor` (structural tree comparison) needs .NET Framework 4.0 /
C# 4.0 or later, per
[csharp4-dlr-expression-node-types.md](../references/csharp4-dlr-expression-node-types.md); on an
older target, normalize parameter names by hand-walking the tree with a `NodeType` switch instead
of subclassing `ExpressionVisitor`.
