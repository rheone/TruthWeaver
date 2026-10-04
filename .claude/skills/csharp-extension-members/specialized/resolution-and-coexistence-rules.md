# Resolution, Precedence, and Coexisting Both Syntaxes

## Precedence: real members always win

An instance/static member actually declared on the receiver type, or inherited from a base
type/implemented interface, is chosen over any extension member (classic or C# 14+) with a
matching signature — silently, with no warning. This is unchanged since C# 3.0 and applies
identically to extension blocks.

## Multiple extension methods with the same signature, different namespaces

Ambiguous only if **both** namespaces are in scope via `using` at the same time — the compiler
raises CS0121 ("ambiguous call"). Fix by removing one `using`, or by calling one fully qualified:
`Acme.Text.StringExtensions.Truncate(s, 10)`.

## Classic and C# 14 extension members on the same receiver

Both can coexist for the same type, even in the same static class, without conflict, as long as
their member shapes don't collide — a classic extension **method** named `Foo` and a C# 14
extension **property** named `Foo` on the same receiver type *do* collide; pick one.

```csharp
public static class OrderExtensions
{
    // classic method — works on .NET 9 and earlier too
    public static decimal TotalWithTax(this Order order, decimal taxRate) =>
        order.Subtotal * (1 + taxRate);

    // C# 14 block — only compiles when LangVersion >= 14
    extension(Order order)
    {
        public bool HasLineItems => order.LineItems.Count > 0;
    }
}
```

## `using` directive scoping

Extension methods/members are visible for `.`-call resolution only when their containing
namespace is imported (`using Acme.Text;`) or `global using`'d, or the caller is in the same
namespace. `using static SomeStaticClass;` also brings a specific static class's extensions into
scope without importing the whole namespace.

## IL compatibility across syntaxes

A C# 14 extension **method** (not property/static/operator) lowers to the same static-method IL
shape as classic syntax — a consumer compiled against an older `LangVersion`, or written in a
different .NET language, calls it as an ordinary static method, unaware which syntax produced it.
Extension properties/operators/statics have no classic-syntax equivalent, so a consumer on an
older `LangVersion` simply can't see them as `.`-syntax members at all.

## Multi-targeting a library across LangVersions

```xml
<PropertyGroup>
  <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
</PropertyGroup>
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <LangVersion>14.0</LangVersion>
</PropertyGroup>
```

Gate C# 14-only members behind `#if NET10_0_OR_GREATER` so the `net9.0` build keeps compiling with
the classic-syntax fallback:

```csharp
public static class OrderExtensions
{
    public static decimal TotalWithTax(this Order order, decimal taxRate) => /* ... */;

#if NET10_0_OR_GREATER
    extension(Order order)
    {
        public bool HasLineItems => order.LineItems.Count > 0;
    }
#endif
}
```

`NET10_0_OR_GREATER` etc. are target-framework preprocessor symbols the SDK defines
automatically — they track the TFM, not `LangVersion`. If a project targets `net10.0` but pins
`LangVersion` below 14 for some reason, define a custom symbol
(`<DefineConstants>CSHARP14</DefineConstants>`) instead of relying on the TFM symbol.
