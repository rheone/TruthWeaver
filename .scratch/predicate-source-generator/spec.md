# Predicate source generator

**Status:** ready-for-agent

Source: [library-roadmap](../library-roadmap/spec.md) "Source-generator predicate registration", grilled 2026-10-09.

## Problem Statement

A host registers every predicate by hand. That is explicit and trim-safe, but it repeats the predicate name, label, description and argument schema that already exist in the method signature. Runtime assembly scanning is rejected (see [deferred-features](../deferred-features/spec.md)): it is magic and breaks trimming and AOT.

## Solution

A Roslyn incremental source generator, shipped as an analyzer in the package, that reads an attribute on predicate methods and emits registration code at compile time. There is no reflection at runtime.

## Decisions

- **Input.** An attribute on predicate methods (static factory methods). The generator reads the signature to build the `PredicateSchema`: argument names, kinds and defaults. Class-level attributes and `IPredicate` classes are not discovered in the first slice.
- **Output.** A generated `Register(PredicateRegistry<TContext>)` method per containing type. Registration stays an explicit call in host code.
- **Diagnostics.** An unsupported parameter type, a duplicate predicate name, or a method that does not return a predicate is a build error from the generator, not a runtime failure.
- **Constraints.** AOT and trim clean. Targets the existing registry API without changing it.

## Out of scope

- Assembly scanning at runtime.
- Class-based (`IPredicate`) discovery.

## Open for the first ticket

- Attribute name and home assembly (`TruthWeaver.Abstractions` or a new analyzer package).
- Whether the generated code calls the existing factory methods or builds the schema inline.
