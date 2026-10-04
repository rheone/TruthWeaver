# `notnull` and Nullable Reference Type Parameters (C# 8.0+ / .NET Core 3.0+)

C# 8 (.NET Core 3.0, 2019) added nullable reference type annotations — `string?` vs. `string` as
compiler-checked, opt-in (`<Nullable>enable</Nullable>`) hints about whether a reference-typed
value may be `null` — and, specific to generics, a `notnull` constraint.

## The `notnull` constraint

```csharp
public class Cache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _items = new();
    // ...
}
```

`Dictionary<TKey, TValue>` itself constrains `TKey` this way in modern BCL signatures — `notnull`
documents (and, with `<Nullable>enable</Nullable>`, enforces via a warning) that passing a
nullable reference type as `TKey` is a mistake, without requiring `TKey` to be a *value* type the
way `where T : struct` would.

`notnull` accepts any non-nullable reference type, any value type (nullable or not — `int?` still
satisfies it, since `Nullable<T>` is itself a value type, though the *unwrapped* nullability
warning still applies to reference type arguments specifically), and rejects an explicitly
nullable reference type argument (`string?`) with a warning, not an error — same
warning-vs-error posture as nullable annotations generally.

## Unconstrained type parameters and `?`

Under `<Nullable>enable</Nullable>`, an unconstrained `T` can't be annotated `T?` to mean "maybe
null" the way `string?` does, because the compiler doesn't know if `T` will be a value or
reference type at the call site. Two common resolutions:

```csharp
// constrain to class, then T? behaves like a normal nullable reference type
public static T? FindOrNull<T>(IEnumerable<T> source, Func<T, bool> predicate) where T : class =>
    source.FirstOrDefault(predicate);

// leave T unconstrained; T? here means "T or the type's default", value or reference alike
public static T? FirstOrDefault<T>(this IList<T> list) =>
    list.Count > 0 ? list[0] : default;
```

The second form's `T?` compiles for any `T` (the oblivious/"maybe-default" meaning); the first
form's `T?` only compiles because `class` pins `T` to reference-type semantics.

## Basic use case

```csharp
public interface IRepository<TKey, TEntity> where TKey : notnull
{
    TEntity? Find(TKey id);
    void Save(TKey id, TEntity entity);
}
```

## Advanced use case: generic default interface members

C# 8 also added default interface members, usable on generic interfaces to supply a shared
implementation that concrete generic types inherit without restating it:

```csharp
public interface IEqualityComparable<T> where T : IEqualityComparable<T>
{
    bool Equals(T other);

    bool NotEquals(T other) => !Equals(other); // default implementation
}
```

(This combines with the curiously recurring generic pattern — see
[specialized/curiously-recurring-generic-pattern.md](../specialized/curiously-recurring-generic-pattern.md).)

## Fallback

Without `notnull`, use `where TKey : class` if reference-type-only is acceptable, or no
constraint at all and rely on a runtime `ArgumentNullException` check in the constructor/method
body — valid from C# 2.0, just unchecked at compile time.
