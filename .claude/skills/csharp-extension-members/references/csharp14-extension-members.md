# Extension Members (C# 14.0 / .NET 10)

C# 14 (.NET 10, GA November 2025) adds a second, block-based extension syntax that closes every
gap in classic extension methods: extension **properties**, **static** extension methods and
properties, and **operators**. It lowers to ordinary static-class IL under the hood — a consumer
on an older runtime just sees regular static extension methods for the members that can be
expressed that way — and it **coexists** with classic `this`-parameter syntax in the same static
class.

## Syntax

Declared inside a top-level, non-generic `static class`, as an `extension(...)` block:

```csharp
public static class EnumerableAcmeExtensions
{
    // instance members: receiver named once, shared by every member in the block
    extension<T>(IEnumerable<T> source)
    {
        public bool IsEmpty => !source.Any();

        public int CountWhere(Func<T, bool> predicate) => source.Count(predicate);
    }

    // static members: receiver is a type, not a value — no parameter name
    extension(string)
    {
        public static string Join(char separator, IEnumerable<string> values) =>
            string.Join(separator, values);
    }
}
```

```csharp
bool empty = numbers.IsEmpty;            // extension property — impossible in classic syntax
string joined = string.Join(',', parts); // static extension member, called as if on `string` itself
```

## Instance vs. static blocks

- `extension(Type receiverName) { ... }` — members act like **instance** members of
  `receiverName`; every member in the block implicitly uses it.
- `extension(Type) { ... }` (no parameter name) — members act like **static** members of `Type`,
  called as `Type.Member`.
- A single static class can declare any number of extension blocks, mixing instance and static,
  over the same or different receiver types.

## Extension properties — basic use case

```csharp
public static class RectangleExtensions
{
    extension(Rectangle rect)
    {
        public double Area => rect.Width * rect.Height;
        public bool IsSquare => rect.Width == rect.Height;
    }
}
```

```csharp
if (rectangle.IsSquare) { /* ... */ }
double area = rectangle.Area;
```

No classic-syntax equivalent exists — the nearest pre-14 fallback is a method: `rect.GetArea()`.
Full treatment: [specialized/extension-properties.md](../specialized/extension-properties.md).

## Static extension members — advanced use case

Useful for adding factory-style members to a type you don't own, or approximating a
`static abstract` interface member on a type you can't modify:

```csharp
public static class GuidExtensions
{
    extension(Guid)
    {
        public static Guid NewSequential() => Guid.CreateVersion7();
    }
}

Guid id = Guid.NewSequential();
```

See [specialized/static-extension-members.md](../specialized/static-extension-members.md) for the
generic-constraint version.

## Operators

```csharp
public static class MoneyExtensions
{
    extension(Money left)
    {
        public static Money operator +(Money left, Money right) =>
            left with { Amount = left.Amount + right.Amount };
    }
}
```

Full walkthrough: [specialized/extension-operators.md](../specialized/extension-operators.md).

## Generic extension blocks

The type parameter is declared once on the `extension<T>(...)` block, not repeated per member
(unlike classic generic extension methods, which repeat `<T>` on every method). Constraints go on
the block:

```csharp
public static class ReadOnlyListExtensions
{
    extension<T>(IReadOnlyList<T> items) where T : IComparable<T>
    {
        public T? Max() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) >= 0 ? a : b);
    }
}
```

Full treatment: [specialized/generic-extension-members.md](../specialized/generic-extension-members.md).

## Requirements and restrictions

- The **enclosing class** must be a non-generic, top-level `static class` (an extension block
  itself can still introduce its own generic parameters, as above).
- Extension blocks cannot be nested inside another extension block.
- An extension member never shadows a real member — same silent-precedence rule as classic
  extension methods.
- No field-like state inside an extension block — only computed members (methods, properties,
  operators) backed by the receiver. A stateful extension property needs external storage (e.g.
  `ConditionalWeakTable<TKey, TValue>`) — see
  [specialized/extension-properties.md](../specialized/extension-properties.md).

## Enable it

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>14.0</LangVersion> <!-- or leave unset: 14 is net10.0's default -->
</PropertyGroup>
```

.NET 9 projects cannot access this feature at any `LangVersion` setting, including `preview` —
extension members were deferred out of the C# 13/.NET 9 cycle and only became available starting
with C# 14 previews on .NET 10.

## Fallback

For anything expressible as a plain method (not a property/static/operator), classic syntax in
[csharp3-extension-methods.md](csharp3-extension-methods.md) works identically on every earlier
target — prefer it when the library needs to be consumable from projects still on .NET 9 or older
*as source*. (Both forms are equally consumable as compiled binaries for plain methods, since C#
14 instance methods lower to the same IL shape as classic syntax.)
