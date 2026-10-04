# The Fluent Builder Form

A builder is "fluent" when every mutator method returns the same concrete builder type instead of
`void`, so a caller chains one call directly into the next and ends the chain with `Build()`. That
capability needs nothing beyond a method returning a reference to its own type — no generics, no
inheritance, no C# version past 1.0 — which makes it a decision independent of both the classic
setter-and-director shape ([../references/pre-csharp2-classic-builder.md](../references/pre-csharp2-classic-builder.md))
and the generic self-typed CRTP base
([generic-self-typed-builder-base.md](generic-self-typed-builder-base.md)), which only earns its
keep once a fluent chain has to keep returning a *derived* type through inheritance. This file is
the plain, single-class fluent form on its own terms.

## Basic: a plain fluent builder, no inheritance, no generics

```csharp
public sealed class ReportBuilder
{
    private string _title = "Untitled";
    private string _author = "Unknown";
    private bool _includeSummary;
    private int _columnCount = 1;

    public ReportBuilder WithTitle(string title) { _title = title; return this; }
    public ReportBuilder WithAuthor(string author) { _author = author; return this; }
    public ReportBuilder IncludeSummary() { _includeSummary = true; return this; }
    public ReportBuilder WithColumnCount(int columnCount) { _columnCount = columnCount; return this; }

    public Report Build() => new(_title, _author, _includeSummary, _columnCount);
}
```

```csharp
Report report = new ReportBuilder()
    .WithTitle("Q3 Results")
    .WithAuthor("Finance")
    .WithColumnCount(4)
    .Build();
```

Every mutator returns `this` — the declaring type itself, not a generic parameter — so the chain
reads top to bottom as its own recipe, with no separate director object sequencing the calls. Name
each mutator for what it does at the call site (`WithTitle`, `IncludeSummary`, a preposition or a
bare verb) rather than the `Set*` convention the non-chaining form uses, since `Set*` reads oddly
once the call no longer stands alone as a statement.

## Advanced: eager vs. deferred validation inside the chain

```csharp
public sealed class ReportBuilder
{
    private string _title = "Untitled";
    private int _columnCount = 1;

    public ReportBuilder WithColumnCount(int columnCount)
    {
        if (columnCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount), "Column count must be at least 1.");
        }
        _columnCount = columnCount;
        return this;
    }

    public ReportBuilder WithTitle(string title) { _title = title; return this; }

    public Report Build() => new(_title, _columnCount);
}
```

Validating inside `WithColumnCount` itself fails fast, but the failure surfaces mid-chain, at
whichever call happens to violate the rule — useful for a check that's local to one field (a
range, a non-null requirement) and cheap to state right where the field is set. A rule that spans
*multiple* fields (mutually exclusive options, "at least one of A or B") can't be checked this way,
because at the time any single `With*` call runs, the other field it needs to compare against
might not be set yet — that kind of check belongs inside `Build()`, running once after every call
in the chain has completed.

## Reuse and thread-safety

A fluent builder instance is mutable, single-owner state accumulated across the chain — treat it
like a local variable with a short lifetime, not a shared or cached object. Calling `Build()` reads
the accumulated fields but doesn't reset them, so invoking `Build()` a second time on the same
instance produces another object with the same field values, not a fresh default; if a codebase
wants "call `Build()` more than once for variants," add an explicit `Reset()` method or start a new
builder instance rather than relying on `Build()` to clear state on its own. Nothing about the
fluent form is safe to share across threads — two threads calling `With*` methods on the same
builder instance concurrently race on the same private fields with no synchronization, since
nothing about the shape implies thread-safety the way an immutable product type would.

## When the plain form isn't enough

- The chain needs to keep returning a *derived* builder's type through an inheritance hierarchy,
  not just the one concrete class above → the generic self-typed CRTP base in
  [generic-self-typed-builder-base.md](generic-self-typed-builder-base.md).
- The chain itself needs to enforce a specific call order at compile time, so a caller can't finish
  building an object having skipped a mandatory step → step builders in
  [step-builders-and-build-order-type-state.md](step-builders-and-build-order-type-state.md).

## Fallback

Every example on this page compiles unchanged back to C# 1.0 — chaining needs only a method
returning a reference to its own type, which has always been part of C#. The expression-bodied
`Build()` method needs C# 6.0; write it as a block body (`public Report Build() { return new
Report(...); }`) on an older target, per
[../references/csharp6-readonly-autoprops-and-expression-bodied-members.md](../references/csharp6-readonly-autoprops-and-expression-bodied-members.md)'s
own fallback, which points back to
[../references/pre-csharp2-classic-builder.md](../references/pre-csharp2-classic-builder.md).
