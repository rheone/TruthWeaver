# Extension Properties (C# 14+)

The single most requested extension-member gap, closed in C# 14
([csharp14-extension-members.md](../references/csharp14-extension-members.md)). Read-only,
write-only, and read/write forms are all supported.

## Basic: read-only computed property

```csharp
public static class PersonExtensions
{
    extension(Person person)
    {
        public string FullName => $"{person.FirstName} {person.LastName}";
    }
}
```

## Advanced: read/write property backed by an external store

Extension members can't add fields, so a read/write extension property needs somewhere else to
keep state — typically `ConditionalWeakTable<TKey, TValue>` for per-instance state that shouldn't
leak memory:

```csharp
public static class TagExtensions
{
    private static readonly ConditionalWeakTable<object, string> Tags = new();

    extension(object obj)
    {
        public string? Tag
        {
            get => Tags.TryGetValue(obj, out var tag) ? tag : null;
            set
            {
                Tags.Remove(obj);
                if (value is not null)
                {
                    Tags.Add(obj, value);
                }
            }
        }
    }
}
```

```csharp
someOrder.Tag = "priority";
Console.WriteLine(someOrder.Tag);
```

`ConditionalWeakTable` ties the tag's lifetime to `obj` — no leak, and no need to make every
extended type `IDisposable`.

## Static extension properties

```csharp
public static class TimeProviderExtensions
{
    extension(DateTimeOffset)
    {
        public static DateTimeOffset UnixEpoch => DateTimeOffset.UnixEpoch;
    }
}
```

## Fallback

No pre-14 syntax produces a real property. Fall back to a `GetXxx()` method (and
`SetXxx(value)` for a writable one) using classic extension-method syntax — see
[csharp3-extension-methods.md](../references/csharp3-extension-methods.md) — and revisit once the
project moves to C# 14.
