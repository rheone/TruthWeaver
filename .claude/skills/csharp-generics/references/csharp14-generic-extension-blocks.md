# Generic Extension Blocks (C# 14 / .NET 10, C# 15 / .NET 11)

C# 14 (.NET 10, GA November 2025) lets an `extension(...)` block declare its own type
parameter, shared by every member inside the block — a distinct generics mechanism from a
classic generic extension method, where each method repeats its own `<T>`.

```csharp
public static class ReadOnlyListExtensions
{
    extension<T>(IReadOnlyList<T> items) where T : IComparable<T>
    {
        public T? Max() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) >= 0 ? a : b);
        public T? Min() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) <= 0 ? a : b);
    }
}
```

This skill covers generics, so here's the minimum needed to read the example above: an
`extension(ReceiverType receiverName) { ... }` block adds instance members to `ReceiverType`;
`extension(ReceiverType)` with no parameter name (no generic parameter needed on a static block
either, unless the static members themselves need one) adds static members, called as
`ReceiverType.Member`. Both forms also support properties and operators, not just methods.

## What's new for generics specifically

- The type parameter and its constraints are declared **once per block**, not once per member —
  the ergonomic difference from classic generic extension methods repeating `<T> where T : ...`
  on every method.
- Constraints on an extension block's type parameter compose with every constraint kind covered
  elsewhere in this skill: `allows ref struct` (C# 13), `notnull` (C# 8), `unmanaged` (C# 7.3),
  and interface constraints referencing `static abstract` members (C# 11 generic math) all work
  inside an `extension<T>(...)` block exactly as they do on an ordinary generic method.

## C# 15 / .NET 11 status

C# 15 (.NET 11, RC1 as of September 2026; GA expected November 2026) adds extension **indexers**
to the block syntax (`this T this[TIndex index] { get; set; }` inside an `extension(...)` block)
but introduces no new generics feature of its own. C# 15 also ships union types and closed
hierarchies; both support generic case types — a union's case types and a closed hierarchy's
derived types can each be closed generic types like any other, no special syntax beyond ordinary
generic type arguments.

## Fallback

Below C# 14, write a classic generic extension method per member instead of one shared block —
repeat `<T>` and its constraints on each method, with `this ReceiverType receiverName` as the
first parameter of each:

```csharp
public static class ReadOnlyListExtensions
{
    public static T? Max<T>(this IReadOnlyList<T> items) where T : IComparable<T> =>
        items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) >= 0 ? a : b);

    public static T? Min<T>(this IReadOnlyList<T> items) where T : IComparable<T> =>
        items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) <= 0 ? a : b);
}
```

See [references/csharp2-generics-fundamentals.md](csharp2-generics-fundamentals.md) for the
classic generic-method shape.
