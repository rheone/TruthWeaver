# Generic Attributes and Generic Math (C# 11 / .NET 7)

C# 11 (.NET 7, November 2022) shipped two large, independent generics features: attribute
classes can be generic, and interfaces can declare `static abstract` members — the mechanism
behind "generic math."

## Generic attributes

Before C# 11, an attribute needing a `Type` argument used `typeof`: `[MyAttribute(typeof(int))]`.
C# 11 lets the attribute class itself be generic:

```csharp
public class ValidatorAttribute<T> : Attribute where T : IValidator, new()
{
}

[Validator<EmailValidator>]
public string Email { get; set; } = "";
```

```csharp
// pre-C#11 equivalent
public class ValidatorAttribute : Attribute
{
    public ValidatorAttribute(Type validatorType) => ValidatorType = validatorType;
    public Type ValidatorType { get; }
}

[Validator(typeof(EmailValidator))]
public string Email { get; set; } = "";
```

Restrictions: the type argument must be fully specified at the attribute-usage site (no unbound
generic parameters), and the constructed attribute type must itself satisfy ordinary attribute
rules (it still derives from `Attribute`).

## Generic math: `static abstract` interface members

C# 11 allows an interface to declare a `static` member as `abstract`, which a type implementing
the interface must provide a static implementation for — including operators:

```csharp
public interface IAddable<TSelf> where TSelf : IAddable<TSelf>
{
    static abstract TSelf operator +(TSelf left, TSelf right);
    static abstract TSelf Zero { get; }
}
```

A generic method can then require `TSelf : IAddable<TSelf>` and call `+` or `Zero` on the type
parameter directly — something impossible before C# 11, since instance interface members can't
express "an operator" or "a static factory":

```csharp
public static T Sum<T>(IEnumerable<T> values) where T : IAddable<T>
{
    T total = T.Zero;
    foreach (var value in values)
    {
        total += value;
    }
    return total;
}
```

## The BCL's numeric interfaces

.NET 7 ships `System.Numerics.INumber<TSelf>` and its constituent interfaces (`IAdditionOperators`,
`IComparisonOperators`, `IParsable<TSelf>`, etc.), implemented by every built-in numeric type
(`int`, `double`, `decimal`, ...). Writing algorithms against `INumber<T>` instead of a specific
numeric type is generic math's main real-world use — full treatment, including why `TSelf` is
always self-referencing, in
[specialized/generic-math-numeric-abstractions.md](../specialized/generic-math-numeric-abstractions.md).

## Advanced use case: parsing generically

```csharp
public static T ParseOrThrow<T>(string text) where T : IParsable<T> =>
    T.TryParse(text, null, out var value) ? value : throw new FormatException($"Cannot parse '{text}' as {typeof(T)}.");

int port = ParseOrThrow<int>("8080");
DateTime date = ParseOrThrow<DateTime>("2026-01-01");
```

One method now serves every `IParsable<T>` implementation in the BCL and any user type that opts
in — the same static-member-through-a-constraint shape that C# 14 static extension members use
for types that *don't* implement the interface.

## Fallback

Generic attributes: use the pre-C#11 `Type`-parameter pattern shown above — works from C# 2.0.
Generic math: no equivalent before C# 11 — the pre-11 substitute is a `Func<T, T, T>` operation
delegate passed explicitly (e.g. `Sum<T>(IEnumerable<T> values, Func<T, T, T> add, T zero)`),
since there's no way to require "this type supports `+`" through an ordinary instance interface.
