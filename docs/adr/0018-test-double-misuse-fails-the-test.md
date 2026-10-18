# ADR-0018: A misused test double fails the test

## Status

Accepted (2026-10-17). Decided with the owner in the Unknown audit (item D7). It refines
[ADR-0001](0001-kleene-failure-model.md) for the test package only.

## Context

ADR-0001 turns a thrown exception in a predicate into `Unknown` plus a `Fault`. `FakePredicates.Scripted` threw
`InvalidOperationException` when its script ran out, so the evaluator absorbed the misuse and `HaveResult(Unknown)` could
pass by accident.

## Decision

1. **A dedicated internal exception.** An exhausted or misconfigured script throws `TestDoubleMisuseException`, an
   `internal` type in `TruthWeaver`. The evaluator and `PredicateHarness` rethrow it. A predicate in production cannot
   throw it, so ADR-0001 holds for them. An exception that a script throws on purpose (the new `Scripted` overload with
   steps) is still `Unknown` plus a `Fault`.
2. **`InternalsVisibleTo` from `TruthWeaver` to `TruthWeaver.Testing`.** This is how the test package can throw the type
   without making it public.

## Considered options

- A public marker exception: any predicate could throw it and skip ADR-0001.
- A recorded misuse that assertions check: a plain assertion on `Unknown` would still pass.
- Matching the exception by name, `Environment.FailFast` or `OperationCanceledException`: fragile, too blunt, or it breaks
  the cancellation rules.

## Consequences

The new overload makes `Scripted(name, [])` ambiguous (CS0121); a caller writes `Array.Empty<TruthValue>()`.

`TruthWeaver` exposes all of its internals to `TruthWeaver.Testing`, which ships as a separate package.
`TruthWeaver.Testing` can then depend on any internal member, so the two packages must ship together at the same version.
An architecture test in `TruthWeaver.Architecture.Tests` pins the set of assemblies that get `InternalsVisibleTo` from
`TruthWeaver`, so a new grant is a deliberate change.
