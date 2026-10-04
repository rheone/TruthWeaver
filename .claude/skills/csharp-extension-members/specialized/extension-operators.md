# Extension Operators (C# 14+)

C# 14 lets an extension block declare `operator` overloads for a receiver type you don't own — the
operator becomes usable anywhere the type is, exactly as if declared on the type itself.

## Basic

```csharp
public readonly record struct Money(decimal Amount, string Currency);

public static class MoneyExtensions
{
    extension(Money left)
    {
        public static Money operator +(Money left, Money right)
        {
            if (left.Currency != right.Currency)
            {
                throw new InvalidOperationException("Currency mismatch.");
            }
            return left with { Amount = left.Amount + right.Amount };
        }

        public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
        public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    }
}
```

```csharp
Money total = price1 + price2;
if (price1 > price2) { /* ... */ }
```

## Advanced: extending a type from a library you don't own

The receiver doesn't need to be your own type — this is exactly the scenario operators were
previously impossible to retrofit onto:

```csharp
public static class VersionExtensions
{
    extension(System.Version left)
    {
        // contrived, but illustrates operating on a BCL type you can't modify
        public static bool operator ^(System.Version left, System.Version right) =>
            left.Major == right.Major;
    }
}
```

## Rules and limits

- Both operands of a binary operator still need to resolve through a receiver type declared in an
  in-scope extension block — ordinary operator-overload resolution rules (matching signatures, no
  boxing tricks) otherwise apply.
- Comparison operators are conventionally added in pairs (`<`/`>`, `<=`/`>=`), and `==`/`!=` go
  with `Equals`/`GetHashCode` overrides on the real type — same convention as declaring operators
  normally.
- Cannot add operators for built-in-to-built-in combinations already defined by the language
  (e.g. `int + int`).

## Fallback

No pre-14 syntax adds real operator syntax to a type you don't own. Use a plainly-named static
method (`MoneyMath.Add(a, b)`) via classic extension-method syntax as the pre-14 stand-in, and
swap call sites to operator syntax once the project can target C# 14.
