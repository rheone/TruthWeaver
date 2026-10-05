# C# Builder Pattern

The Builder pattern separates constructing a complex object from the object itself, letting you
assemble it step by step and validate the result before it ever becomes visible. This skill covers
the classic GoF builder, fluent chained builders, a generic self-typed builder base, and step
builders that enforce construction order through the type system, plus how each C# language
version since 1.0 has changed what an idiomatic builder looks like, or offered a lighter-weight
alternative for simple cases.

## When to reach for it

- A type has enough optional parameters, validation rules, or construction steps that a constructor
  call or object initializer would be unreadable or unsafe.
- You're deciding whether a type actually needs a builder at all, versus an object initializer,
  `required` members, or a record `with`-expression.
- You want a fluent chain on a derived builder class to keep returning the derived type instead of
  the base type.
- You need construction order enforced at compile time, so a caller can't finish building an object
  in an invalid state.

## Using it

This skill is model-invoked: it activates automatically when you're writing, reviewing, or
refactoring a builder class, or weighing a builder against a simpler construction approach. You can
also invoke it directly by asking for it or typing `/csharp-builder-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The classic GoF builder and director, with no generics available | [references/pre-csharp2-classic-builder.md](references/pre-csharp2-classic-builder.md) |
| Generic builder bases and self-typed (CRTP) chaining | [references/csharp2-generic-builders.md](references/csharp2-generic-builders.md) |
| Object/collection initializers and fluent extension methods as a simpler alternative | [references/csharp3-object-initializers-and-fluent-extensions.md](references/csharp3-object-initializers-and-fluent-extensions.md) |
| Read-only auto-properties and expression-bodied members simplifying builder output | [references/csharp6-readonly-autoprops-and-expression-bodied-members.md](references/csharp6-readonly-autoprops-and-expression-bodied-members.md) |
| Init-only setters and records narrowing when a builder is still needed | [references/csharp9-init-only-setters-and-records.md](references/csharp9-init-only-setters-and-records.md) |
| `required` members enforcing mandatory fields without a builder | [references/csharp11-required-members.md](references/csharp11-required-members.md) |
| Primary constructors and collection expressions shrinking builder boilerplate | [references/csharp12-primary-constructors.md](references/csharp12-primary-constructors.md) |
| The plain fluent builder form: chaining, naming, and in-chain validation | [specialized/fluent-builder-form.md](specialized/fluent-builder-form.md) |
| The generic, self-typed `Builder<TSelf, TProduct>` base in depth | [specialized/generic-self-typed-builder-base.md](specialized/generic-self-typed-builder-base.md) |
| Step builders that enforce build order via a chain of interfaces | [specialized/step-builders-and-build-order-type-state.md](specialized/step-builders-and-build-order-type-state.md) |
| The decision list for choosing a builder versus a modern construction alternative | [specialized/builder-vs-modern-alternatives.md](specialized/builder-vs-modern-alternatives.md) |
| Test-data builders and object mothers for fixture setup | [specialized/testing-with-builders.md](specialized/testing-with-builders.md) |

## Example prompts

- "This constructor has nine optional parameters. Should I build a fluent builder for it?"
- "I want a generic builder base so every derived builder's fluent methods return the derived type,
  not the base."
- "Help me write a step builder that won't let a caller finish building this object without setting
  the required fields first."
