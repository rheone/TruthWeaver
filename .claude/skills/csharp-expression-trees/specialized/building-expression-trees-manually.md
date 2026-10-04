# Building Expression Trees Manually

The compiler builds a tree for you whenever a lambda targets `Expression<TDelegate>` (see
[csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md)),
but sometimes the shape of the logic is only known at runtime — a dynamic filter built from a list
of field names and operators chosen by a caller, for instance — and there's no lambda syntax to
write because there's no fixed lambda. This file covers assembling a tree node by node with the
`Expression` static factory class, and `.Compile()`ing the result into a real, invokable delegate.

## Basic: `Expression.Parameter` + `Expression.Call` + `Expression.Lambda`

```csharp
// Builds, at runtime: Expression<Func<string, bool>> check = s => s.StartsWith(prefix);

public static Expression<Func<string, bool>> BuildStartsWithCheck(string prefix)
{
    ParameterExpression s = Expression.Parameter(typeof(string), "s");
    MethodInfo startsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;

    MethodCallExpression call = Expression.Call(s, startsWith, Expression.Constant(prefix));

    return Expression.Lambda<Func<string, bool>>(call, s);
}

Expression<Func<string, bool>> tree = BuildStartsWithCheck("ORD-");
Func<string, bool> check = tree.Compile();

bool matches = check("ORD-001"); // true
```

`Expression.Parameter` declares the lambda's input; `Expression.Call` builds a method-invocation
node against that parameter (an instance method here — `Expression.Call(instance, method, args)` —
or a static one via `Expression.Call(method, args)` with no instance); `Expression.Lambda<TDelegate>`
wraps a body expression and its parameter list into the final `Expression<TDelegate>`.
`prefix` is captured as a `ConstantExpression`, baked into the tree at build time — not a
runtime-read closure variable the way it would be in an ordinary lambda, since there's no closure
here at all, just data assembled once.

## Basic: composing binary and member-access nodes

```csharp
// Builds: Expression<Func<Order, bool>> filter = o => o.Total > threshold;

public static Expression<Func<Order, bool>> BuildTotalGreaterThan(decimal threshold)
{
    ParameterExpression order = Expression.Parameter(typeof(Order), "o");
    MemberExpression total = Expression.Property(order, nameof(Order.Total));
    BinaryExpression comparison = Expression.GreaterThan(total, Expression.Constant(threshold));

    return Expression.Lambda<Func<Order, bool>>(comparison, order);
}
```

`Expression.Property` builds a `MemberExpression` reading a property off the parameter;
`Expression.GreaterThan` builds a `BinaryExpression`. Every C# operator has a corresponding
`Expression` factory method (`Expression.Equal`, `Expression.AndAlso`, `Expression.Add`, ...) —
building a tree by hand is mechanically translating the operator syntax you'd otherwise write into
factory-method calls.

## Advanced: a generic helper that builds a tree for an arbitrary `T`

```csharp
public static Expression<Func<T, bool>> BuildPropertyEquals<T, TValue>(
    string propertyName, TValue value)
{
    ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
    MemberExpression property = Expression.Property(parameter, propertyName);
    BinaryExpression equals = Expression.Equal(property, Expression.Constant(value, typeof(TValue)));

    return Expression.Lambda<Func<T, bool>>(equals, parameter);
}

Expression<Func<Order, bool>> pendingFilter = BuildPropertyEquals<Order, OrderStatus>(
    nameof(Order.Status), OrderStatus.Pending);

IQueryable<Order> pending = dbContext.Orders.Where(pendingFilter); // usable directly against IQueryable<T>
```

`BuildPropertyEquals<T, TValue>` is generic over both the entity type and the property's value
type, so it works for any `T`/`TValue` combination a caller supplies a matching property name and
value for — the same generic-method type-inference rules that apply everywhere else in C# apply
here too, with `T` typically supplied explicitly (as above) since there's no argument of type `T`
for the compiler to infer it from. Because the result is a real `Expression<Func<T, bool>>`, it
composes with `IQueryable<T>.Where` exactly like a compiler-built one would — a provider like EF
Core has no way to tell the two apart.

## Advanced: combining two trees with a shared parameter

```csharp
public static Expression<Func<T, bool>> And<T>(
    this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
{
    ParameterExpression parameter = left.Parameters[0];
    var rebind = new ParameterReplacer(right.Parameters[0], parameter);
    Expression rightBody = rebind.Visit(right.Body)!;

    return Expression.Lambda<Func<T, bool>>(
        Expression.AndAlso(left.Body, rightBody), parameter);
}
```

Two independently-built `Expression<Func<T, bool>>` trees each have their *own* `ParameterExpression`
instance — even if both are named `"x"`, they aren't reference-equal, so combining their bodies
directly with `Expression.AndAlso` would produce an invalid tree referencing two different
parameters for what should be one. `ParameterReplacer` (an `ExpressionVisitor` subclass that swaps
one `ParameterExpression` for another — see
[expression-visitor-and-tree-rewriting.md](expression-visitor-and-tree-rewriting.md)) rebinds
`right`'s body onto `left`'s parameter before combining. This `And<T>` extension method is the
core trick behind every "predicate builder" utility for composing `IQueryable<T>` filters at
runtime.

## Fallback

Manual tree construction via `Expression.Parameter`/`Expression.Call`/`Expression.Lambda` and
single-expression combinators (`Expression.GreaterThan`, `Expression.AndAlso`, `Expression.Property`,
...) all work from
[csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md)'s
C# 3.0 / .NET Framework 3.5 baseline onward — nothing in this file needs the DLR-era node types
from
[csharp4-dlr-expression-node-types.md](../references/csharp4-dlr-expression-node-types.md).
On .NET Framework 1.0–2.0, where `Expression` doesn't exist at all, build and interpret a
hand-rolled node hierarchy instead, per
[pre-csharp3-no-expression-trees.md](../references/pre-csharp3-no-expression-trees.md).
