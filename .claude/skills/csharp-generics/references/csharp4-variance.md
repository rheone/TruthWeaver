# Variance (C# 4.0+ / .NET Framework 4.0+)

C# 4.0 (.NET Framework 4.0, 2010) added declaration-site variance for generic **interfaces** and
**delegates** — not classes or structs. Variance lets a generic type built over a more-derived
type argument be used where one built over a less-derived type argument is expected.

## Covariance (`out`)

A type parameter marked `out` may only appear in **output** positions (return types, `get`-only
properties). The generic type is then covariant: `IProducer<Derived>` is assignable to
`IProducer<Base>`.

```csharp
public interface IProducer<out T>
{
    T Produce();
}

IProducer<string> stringProducer = /* ... */;
IProducer<object> objectProducer = stringProducer; // legal: string is more derived than object
```

`IEnumerable<out T>` in the BCL is the everyday example: `IEnumerable<string>` converts to
`IEnumerable<object>` implicitly.

## Contravariance (`in`)

A type parameter marked `in` may only appear in **input** positions (method parameters). The
generic type is then contravariant: `IConsumer<Base>` is assignable to `IConsumer<Derived>`.

```csharp
public interface IConsumer<in T>
{
    void Consume(T item);
}

IConsumer<object> objectConsumer = /* ... */;
IConsumer<string> stringConsumer = objectConsumer; // legal: a consumer of anything can consume a string
```

`IComparer<in T>` and `Action<in T>` are the BCL examples.

## Delegates

The same annotations apply to delegate type parameters:

```csharp
public delegate TResult Transformer<in TInput, out TResult>(TInput input);
```

`Func<in T, out TResult>` and `Action<in T>` are declared this way in the BCL, which is why a
`Func<object, string>` can be assigned to a `Func<string, object>`-shaped... — more precisely, why
a `Func<Base, Derived>` converts to `Func<Derived, Base>`'s covariant/contravariant positions
without a cast.

## Rules

- Only **interfaces and delegates** can declare `in`/`out` type parameters — classes, structs, and
  generic methods cannot.
- A type parameter with no variance annotation is **invariant**: `List<string>` is never
  assignable to `List<object>`, because `List<T>` uses `T` in both input and output positions
  (`Add(T)` and the indexer getter).
- Mixing an `out` parameter into an input-only-looking position (e.g. as a constraint on another
  parameter) is checked at compile time — the compiler rejects invalid variance uses, it does not
  silently narrow them.

Full worked examples of designing your own variant interface, including why a naive attempt
usually fails on a member that mixes both positions, are in
[specialized/variance-in-depth.md](../specialized/variance-in-depth.md).

## Fallback

Drop the `in`/`out` annotations and lose the implicit conversions — the interface/delegate still
compiles and works from C# 2.0, just without variance:

```csharp
public interface IProducer<T> // C# 2.0 — no variance
{
    T Produce();
}
```

Callers then need an explicit cast or a `Select`-style projection where the variance conversion
used to apply implicitly.
