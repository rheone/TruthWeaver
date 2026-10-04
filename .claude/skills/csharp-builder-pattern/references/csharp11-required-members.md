# Required Members (C# 11.0, GA November 8, 2022)

The `required` modifier lets a property or field demand that every construction path — object
initializer or constructor — actually assign it, enforced at compile time. Like init-only setters
in the previous tier, this doesn't change how a builder is *written*; it closes another gap in the
object-initializer/init-only alternative to a builder. Before `required`, init-only properties
(C# 9.0) gave you immutability after construction but nothing stopped a caller from leaving a
mandatory field at its default — `required` is the missing "you must set this" enforcement that
used to be one of a builder's few remaining unique selling points for simple product shapes.

## Syntax

```csharp
public sealed class Invoice
{
    public required string Title { get; init; }
    public required decimal Amount { get; init; }
    public string? Notes { get; init; } // still optional
}
```

## Basic use case: required members as a builder-free way to force mandatory fields

```csharp
var invoice = new Invoice
{
    Title = "March",
    Amount = 199.99m
    // omitting either Title or Amount here is a compile error: CS9035
};
```

```csharp
// var broken = new Invoice { Title = "March" }; // CS9035: required member 'Invoice.Amount' must be set
```

This is the direct answer to the classic builder-pattern justification "so callers can't forget to
set a mandatory field" — for a flat product shape with no cross-field validation, `required`
delivers that guarantee without a builder class to maintain, and the compiler diagnostic
(`CS9035`) fires at the call site instead of a builder's `Build()` throwing at run time.

## Advanced use case: a builder that still earns its place despite `required` — cross-field validation `required` cannot express

```csharp
public sealed class DateRange
{
    public required DateOnly Start { get; init; }
    public required DateOnly End { get; init; }
}
```

```csharp
// compiles, but is nonsense: required only checks presence, not the relationship between values
var backwards = new DateRange { Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 1, 1) };
```

```csharp
public sealed class DateRangeBuilder
{
    private DateOnly? _start;
    private DateOnly? _end;

    public DateRangeBuilder From(DateOnly start) { _start = start; return this; }
    public DateRangeBuilder To(DateOnly end) { _end = end; return this; }

    public DateRange Build()
    {
        if (_start is null || _end is null)
        {
            throw new InvalidOperationException("Both From and To are required.");
        }
        if (_end < _start)
        {
            throw new InvalidOperationException("End must not precede Start.");
        }
        return new DateRange { Start = _start.Value, End = _end.Value };
    }
}
```

`required` verifies only that a member was *assigned something* — it has no concept of one field's
value depending on another's. A builder's `Build()` step is still the only place that kind of
validation can run before the object exists, which is why `required` narrows a builder's
justification rather than eliminating it outright. See
[specialized/builder-vs-modern-alternatives.md](../specialized/builder-vs-modern-alternatives.md)
for the full decision list.

## Requirements and restrictions

- A `required` member must be a property or field that is settable via object-initializer syntax —
  `init` or a plain mutable `set` both work, but a get-only property computed from other state
  cannot be marked `required`.
- A constructor can satisfy a `required` member on the caller's behalf by applying
  `[SetsRequiredMembers]` to that constructor, but object-initializer syntax bypasses the
  requirement checked at the constructor only when that attribute is present — omitting it means
  every construction path (including through that constructor) still has to satisfy every
  `required` member via the object initializer.

## Fallback

On a target before C# 11.0 (down to C# 9.0, for the init-only baseline — see
[csharp9-init-only-setters-and-records.md](csharp9-init-only-setters-and-records.md)), there is no
compile-time way to force a mandatory init-only member to be set; a factory method or a builder's
`Build()` performing a run-time null/default check (as `DateRangeBuilder.Build()` already does
above) is the fallback, same as it was before `required` existed at all.
