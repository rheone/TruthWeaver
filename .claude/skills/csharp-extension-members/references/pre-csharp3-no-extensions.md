# No Extension Mechanism (C# 1.0 – 2.0 / .NET Framework 1.0 – 2.0)

Extension methods do not exist before C# 3.0 (.NET Framework 3.5, 2007). Targeting .NET
Framework 1.0, 1.1, or 2.0 — or compiling with `<LangVersion>1</LangVersion>` /
`<LangVersion>2</LangVersion>` — none of the `this`-parameter or `extension(...)` syntax in this
skill compiles.

## What .NET 2.0 does have

- **Generics** (introduced in .NET 2.0 / C# 2.0) — generic *methods* and *types* exist, just not
  generic *extension* methods (those need C# 3.0's `this`-parameter syntax on top).
- **Static classes** (`static class`) — introduced in C# 2.0, so the container is available even
  though the extension mechanism isn't.
- **Nullable value types** (`Nullable<T>` / `T?`) — but no nullable *reference* type annotations;
  those are C# 8 ([csharp8-nullable-extensions.md](csharp8-nullable-extensions.md)).

## The fallback pattern: static helper classes

Instead of `text.IsNullOrBlank()`, write a static utility class and call it directly, passing the
"receiver" as an ordinary first argument:

```csharp
// C# 2.0 / .NET Framework 2.0 — compiles everywhere
public static class StringUtil
{
    public static bool IsNullOrBlank(string value)
    {
        return value == null || value.Trim().Length == 0;
    }
}

bool blank = StringUtil.IsNullOrBlank(someText);
```

Generic helper equivalent:

```csharp
public static class CollectionUtil
{
    public static bool IsEmpty<T>(IEnumerable<T> source)
    {
        foreach (T item in source)
        {
            return false;
        }
        return true;
    }
}

bool empty = CollectionUtil.IsEmpty(numbers);
```

## Porting forward

When a project later moves to .NET Framework 3.5+ (C# 3.0+), promote these to real extension
methods by adding `this` to the first parameter — see
[csharp3-extension-methods.md](csharp3-extension-methods.md). The call sites change from
`StringUtil.IsNullOrBlank(someText)` to `someText.IsNullOrBlank()`; the method bodies are
unchanged, so the port is mechanical.
