---
name: csharp-builder-pattern
description: Reference for the Builder design pattern in C# — classic GoF builder, fluent/chained builders, generic self-typed (CRTP) builder bases, and step builders that enforce build order via the type system — plus the C# language features that changed how idiomatic builders are written over time (generics enabling reusable generic builder bases, C# 2.0; object initializers and extension methods as a simple-case alternative/complement, C# 3.0; read-only auto-properties and expression-bodied members simplifying builder output, C# 6.0; init-only setters, records, and target-typed `new`, C# 9.0; required members, C# 11.0; primary constructors and collection expressions, C# 12.0). Use when writing, reviewing, or refactoring a builder class; deciding whether a type needs a builder at all versus an object initializer, `required` members, or a record `with`-expression; designing a generic `Builder<TSelf, TProduct>` base so fluent chains on a derived builder return the derived type; enforcing construction order at compile time with a step-builder/type-state interface chain; or writing test-data builders (object mothers) for fixture setup.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.1.0
---

# C# Builder Pattern

**Builder is a design pattern, not a C# language feature** — unlike this repo's other version-tiered
skills (generics, LINQ, async, and so on), there is no "the builder pattern shipped in C#N." What
this skill tiers instead is the set of C# language features that genuinely changed *how you write*
an idiomatic builder, or that compete with a builder for the same job in simple cases: generics
(C# 2.0) made a reusable generic builder base possible at all; object initializers (C# 3.0),
init-only setters and records (C# 9.0), and required members (C# 11.0) each narrow the cases where
a builder is still the best tool, without ever making the pattern itself obsolete; primary
constructors (C# 12.0) shrink the boilerplate around both a builder's product type and the builder
class itself. Several C# versions between these add nothing builder-relevant at all — that's a
real, informative gap, not an oversight; see the routing table below for exactly which versions.

## Quick start (works everywhere, C# 2.0+)

```csharp
public abstract class Builder<TSelf, TProduct> where TSelf : Builder<TSelf, TProduct>
{
    public abstract TProduct Build();
}

public sealed class InvoiceBuilder : Builder<InvoiceBuilder, Invoice>
{
    private string _title = "Untitled";
    private decimal _amount;

    public InvoiceBuilder WithTitle(string title) { _title = title; return this; }
    public InvoiceBuilder WithAmount(decimal amount) { _amount = amount; return this; }

    public override Invoice Build() => new Invoice(_title, _amount);
}

Invoice invoice = new InvoiceBuilder()
    .WithTitle("March")
    .WithAmount(199.99m)
    .Build();
```

This compiles unchanged on every tier from C# 2.0 forward (`=>` expression bodies need C# 6.0 —
swap in block bodies on an older target, see
[references/csharp6-readonly-autoprops-and-expression-bodied-members.md](references/csharp6-readonly-autoprops-and-expression-bodied-members.md)).
On C# 1.0, drop the generic base entirely — see
[references/pre-csharp2-classic-builder.md](references/pre-csharp2-classic-builder.md).

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets. Gaps in the
version sequence are deliberate — nothing builder-relevant shipped in the skipped versions.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 1.0/1.1 | C# 1.0 | [references/pre-csharp2-classic-builder.md](references/pre-csharp2-classic-builder.md) — no generics, no chaining convention yet; classic GoF builder with a director, one non-generic class per product |
| .NET Framework 2.0+ | C# 2.0 | [references/csharp2-generic-builders.md](references/csharp2-generic-builders.md) — generics enable a reusable `Builder<TProduct>`/`Builder<TSelf, TProduct>` base; the CRTP self-typed pattern for fluent chains that survive inheritance |
| .NET Framework 3.5+ | C# 3.0 | [references/csharp3-object-initializers-and-fluent-extensions.md](references/csharp3-object-initializers-and-fluent-extensions.md) — object/collection initializers as a builder-free alternative for simple shapes; extension methods add fluent calls without owning the builder type; lambda-based configuration callbacks |
| — | C# 6.0 | [references/csharp6-readonly-autoprops-and-expression-bodied-members.md](references/csharp6-readonly-autoprops-and-expression-bodied-members.md) — read-only auto-properties simplify the immutable product a builder targets; expression-bodied fluent methods and `Build()` |
| .NET 5+ | C# 9.0 | [references/csharp9-init-only-setters-and-records.md](references/csharp9-init-only-setters-and-records.md) — init-only setters and records/`with`-expressions narrow when a builder is needed at all; target-typed `new` shortens every builder call site |
| .NET 7+ | C# 11.0 | [references/csharp11-required-members.md](references/csharp11-required-members.md) — `required` forces mandatory fields without a builder, for shapes with no cross-field validation |
| .NET 8+ | C# 12.0 | [references/csharp12-primary-constructors.md](references/csharp12-primary-constructors.md) — primary constructors shrink both the product type's and the builder's own constructor boilerplate; collection expressions for a builder's accumulated-items output |

C# 4.0, 5.0, 7.x, 8.0, 10.0, 13.0, and 14.0 (verified current as of September 2026, alongside C# 15
in preview) add nothing that changes how a builder is written or narrows when one is needed — no
reference file exists for those versions; the newest tier at or below your target still applies
unchanged.

## Specialized patterns

- [specialized/fluent-builder-form.md](specialized/fluent-builder-form.md) — the plain fluent builder on its own terms: mutators returning the concrete type to enable chaining, naming conventions, eager vs. deferred in-chain validation, and reuse/thread-safety caveats
- [specialized/generic-self-typed-builder-base.md](specialized/generic-self-typed-builder-base.md) — the CRTP `Builder<TSelf, TProduct>` pattern in depth: multi-level hierarchies, a shared validation base, and the runtime-cast failure mode when `TSelf` is mismatched
- [specialized/step-builders-and-build-order-type-state.md](specialized/step-builders-and-build-order-type-state.md) — enforcing construction order at compile time with a chain of narrow step interfaces (the type-state pattern), including a generic step-interface shape reusable across product hierarchies
- [specialized/builder-vs-modern-alternatives.md](specialized/builder-vs-modern-alternatives.md) — the decision list for object initializer vs. `required`+`init` vs. record `with`-expression vs. an actual builder, plus the cross-field-validation case where a builder still wins
- [specialized/testing-with-builders.md](specialized/testing-with-builders.md) — the builder pattern as a test-authoring tool: test-data builders and object mothers for fixture setup, a shared generic test-builder base, and composing builders for an aggregate root
