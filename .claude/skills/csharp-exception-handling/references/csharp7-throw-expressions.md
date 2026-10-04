# Throw Expressions (C# 7.0)

C# 7.0 shipped March 2017 with Visual Studio 2017 and made `throw` usable as an **expression**,
not just a statement. Before this tier, `throw` could only appear as its own statement — it could
not appear as an operand of the conditional (`?:`) or null-coalescing (`??`) operators, or as the
body of an expression-bodied member, because a statement can't appear where an expression is
required. C# 7.0 added an expression form of `throw` (typed as the special "nothing" type, so it's
compatible with any expression context) specifically to close those gaps.

## Syntax

```csharp
condition ? expr : throw new SomeException();
expr ?? throw new SomeException();
member => throw new SomeException(); // expression-bodied member whose entire body is a throw
```

## Basic use case: null-coalescing

```csharp
public class Customer
{
    private string _name;

    public string Name
    {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }
}
```

Before C# 7.0, the setter needed a full statement body:

```csharp
set
{
    if (value is null)
    {
        throw new ArgumentNullException(nameof(value));
    }
    _name = value;
}
```

## Advanced use case: conditional operator and expression-bodied methods

```csharp
public string FirstArgument(string[] args) =>
    args.Length > 0 ? args[0] : throw new ArgumentException("At least one argument is required.");

public DateTime ToDateTime(IFormatProvider provider) =>
    throw new NotSupportedException("Conversion to DateTime is not supported for this type.");
```

The second example's entire expression-bodied method body is a `throw` — legal specifically because
the expression form of `throw`, not the statement form, is what an expression-bodied member's `=>`
requires.

## Requirements and restrictions

- A `throw` expression's operand is still subject to the same C# 1.0 rule as a `throw` statement
  — it must be (or produce) something implicitly convertible to `System.Exception`. See
  [csharp1-try-catch-finally.md](csharp1-try-catch-finally.md).
- The conditional operator's two branches must still have a common type once the `throw` branch is
  set aside — `condition ? "a" : throw new Exception()` is fine (the whole expression's type is
  `string`, taken from the non-throwing branch), but `condition ? throw new Exception() : throw
  new OtherException()` (both branches throwing) is not valid, since there's no non-throwing branch
  left to establish a type.
- This is purely a new place to write an existing statement's semantics — a throw expression has
  the same runtime effect (raise the exception, propagate/search for a handler) as a `throw`
  statement; nothing about how exceptions propagate or get caught changed in this tier.

## Fallback

Below C# 7.0, `throw` cannot appear inside `?:`, `??`, or as an expression-bodied member's entire
body — rewrite as an ordinary statement-bodied member with an `if`/`throw` (or a full-body `throw`
statement) as shown in the basic use case above, falling back to
[csharp1-try-catch-finally.md](csharp1-try-catch-finally.md)'s baseline `throw`-statement form.
