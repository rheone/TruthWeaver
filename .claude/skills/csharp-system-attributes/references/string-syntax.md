# String syntax attribute

`System.Diagnostics.CodeAnalysis.StringSyntaxAttribute` — since .NET 7.

Tells the IDE what embedded language a `string` parameter, field, or property actually holds, so
it can turn on that language's editing experience — syntax highlighting, IntelliSense, and
in-editor diagnostics — inside the string literal at the call site. It has **no compiler or
runtime effect of its own**: omitting it never produces a warning, and applying it never changes
behavior. The entire value is IDE tooling (Visual Studio and Rider both honor it).

```csharp
public static Regex Compile([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
    new(pattern);

Compile(@"^\d{3}-\d{4}$"); // now syntax-highlighted and validated as a regex literal in the IDE
```

## Built-in syntax identifiers

`StringSyntaxAttribute` exposes its known syntaxes as `const string` fields, not an enum — the
type is intentionally open so third parties can define their own:

| Constant | What it marks |
| --- | --- |
| `StringSyntaxAttribute.Regex` | A regular expression pattern |
| `StringSyntaxAttribute.Json` | A JSON document fragment |
| `StringSyntaxAttribute.DateOnlyFormat` / `TimeOnlyFormat` / `TimeSpan` | `DateOnly`/`TimeOnly`/`TimeSpan` custom format strings |
| `StringSyntaxAttribute.NumericFormat` | A numeric format string (`"N2"`, `"0.00"`, ...) |
| `StringSyntaxAttribute.CompositeFormat` | A `string.Format`/`CompositeFormat`-style template with `{0}` placeholders |
| `StringSyntaxAttribute.Uri` | A URI |
| `StringSyntaxAttribute.Xml` | An XML fragment |
| `StringSyntaxAttribute.GuidFormat` | A `Guid` custom format string |

## `CompositeFormat` and the optional `arguments` parameter

The constructor accepts an optional `object?[] arguments` — used only with
`StringSyntaxAttribute.CompositeFormat` — to tell the IDE which of the *method's own* parameters
supply the `{0}`, `{1}`, ... placeholder values, so it can flag a placeholder index with no
matching argument:

```csharp
public static string Format(
    [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
    params object?[] args) => string.Format(format, args);
```

**When to add it proactively**: any public parameter, property, or field whose `string` value is
parsed as another language downstream — a regex pattern, a route template, a JSON payload, a
format string — especially on library APIs, where the IDE has no other way to infer what a plain
`string` actually contains. It costs nothing (no runtime dependency, no behavior change) and
directly improves the authoring experience for every caller's IDE.

## Fallback / no-op behavior

.NET 7+ only; the attribute type doesn't exist on older targets. On an older multi-targeted TFM,
omit it under `#if NET7_0_OR_GREATER` rather than substituting anything — there is no
lesser-version equivalent, and skipping it costs callers on that TFM nothing but the IDE
highlighting.
