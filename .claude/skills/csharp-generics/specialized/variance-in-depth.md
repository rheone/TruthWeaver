# Variance In Depth

Builds on [references/csharp4-variance.md](../references/csharp4-variance.md)'s basics. Covers
designing your own variant interface, and the member shape that blocks variance entirely.

## Why `List<T>` isn't covariant but `IEnumerable<T>` is

`List<T>.Add(T item)` uses `T` as a method **parameter** (an input position); a covariant `T`
would let a caller add an `object` to what's really a `List<string>` through a
`List<object>`-typed reference, breaking type safety at runtime. `IEnumerable<T>` only ever
*produces* `T` (`Current { get; }`), never accepts one, so covariance is sound there.

The rule generalizes: **a member that only returns/gets `T` keeps covariance sound; a member that
accepts/sets `T` keeps contravariance sound; a member that does both blocks both.**

## Designing a covariant interface

Start from the member list and check every use of the type parameter is output-only:

```csharp
public interface IReadOnlyRepository<out T>
{
    T GetById(int id);          // output — OK
    IEnumerable<T> GetAll();    // output — OK
    // bool Contains(T item);   // would NOT compile: T as a parameter is an input position
}
```

Uncomment `Contains(T item)` and the compiler rejects the whole interface with CS1961 — `out T`
can't appear in an input position anywhere in the interface, not just in the member you're adding.

## Designing a contravariant interface

Same check, inverted — every use of `T` must be an input:

```csharp
public interface IValidator<in T>
{
    bool Validate(T item);           // input — OK
    void ValidateOrThrow(T item);    // input — OK
    // T Repair(T item);             // would NOT compile: return position is output
}
```

## Mixing variance across multiple type parameters

Different type parameters on the same interface can carry different variance independently:

```csharp
public interface ITransformer<in TInput, out TOutput>
{
    TOutput Transform(TInput input);
}
```

`TInput` only appears as a parameter (contravariant-safe); `TOutput` only appears as a return
type (covariant-safe) — this is exactly `Func<in T, out TResult>`'s shape in the BCL.

## When a member needs both directions for the same `T`

If a genuinely necessary member needs `T` in both an input and an output position, the type
parameter can't be marked either `in` or `out` — leave it invariant, or split the responsibility
across two interfaces (a producer and a consumer) that a single class implements both of, each
individually variant.

## Fallback

No variance before C# 4.0 — drop `in`/`out`, and every generic interface/delegate is invariant
(C# 2.0 behavior); callers use an explicit `.Cast<T>()`/`.Select(x => (T)x)` or `object` to bridge
what a variance conversion would otherwise cover implicitly.
