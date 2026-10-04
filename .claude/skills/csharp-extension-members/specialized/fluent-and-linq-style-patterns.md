# Fluent and LINQ-Style Extension Patterns

Advanced compositional use cases built on top of the basics in
[csharp3-extension-methods.md](../references/csharp3-extension-methods.md).

## Fluent chaining

Return the same (or a related) type so calls chain:

```csharp
public static class StringBuilderExtensions
{
    public static StringBuilder AppendLineIf(this StringBuilder sb, bool condition, string line) =>
        condition ? sb.AppendLine(line) : sb;
}
```

```csharp
new StringBuilder()
    .AppendLine("Header")
    .AppendLineIf(includeDetails, "Details go here")
    .AppendLine("Footer");
```

## Custom LINQ-style query operators

Extension methods on `IEnumerable<T>` that return `IEnumerable<T>` compose with the built-in LINQ
operators and use `yield return` for deferred execution:

```csharp
public static class EnumerableAcmeExtensions
{
    public static IEnumerable<T> DistinctByKey<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }

    public static IEnumerable<(int Index, T Value)> WithIndex<T>(this IEnumerable<T> source)
    {
        int i = 0;
        foreach (var item in source)
        {
            yield return (i++, item);
        }
    }
}
```

```csharp
var deduped = orders.DistinctByKey(o => o.CustomerId).WithIndex();
```

Both are lazily evaluated (`yield return`) — matching the convention every built-in LINQ operator
follows, so mixing them with `.Where()`/`.Select()` doesn't force earlier materialization than the
caller expects.

## Builder-style extensions using extension properties (C# 14+)

Combine C# 14 extension properties with fluent methods when a "computed view" of a builder is more
natural as a property than a method call:

```csharp
public static class QueryBuilderExtensions
{
    extension(QueryBuilder builder)
    {
        public bool IsEmpty => builder.ClauseCount == 0;

        public QueryBuilder WhereActive() => builder.Where("IsActive = 1");
    }
}
```

```csharp
if (query.IsEmpty) { /* ... */ }
var built = query.WhereActive().OrderBy("CreatedAt");
```

## Fallback

Every pattern in this file except the last section is plain classic-syntax extension methods and
works from C# 3.0 onward. The builder-style property example needs C# 14 — see
[../references/csharp14-extension-members.md](../references/csharp14-extension-members.md) for the
fallback to a `GetIsEmpty()`-style method.
