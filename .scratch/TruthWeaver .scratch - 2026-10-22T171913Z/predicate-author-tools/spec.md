# Predicate author tools: deprecation marker and unused-predicate audit

**Status:** done

Source: [library-roadmap](../library-roadmap/spec.md) re-score (2026-10-03), grilled 2026-10-09.

## Problem Statement

A host that maintains a live predicate catalog cannot retire a predicate safely. There is no way to mark a predicate as
deprecated, so authors of existing rules get no warning. There is also no way to find which registered predicates no
stored rule uses any more, so dead predicates stay in the registry.

## Solution

1. A deprecation marker on `PredicateSchema`, with a compile warning on every use.
2. Two small public enumeration members: the schemas in a `PredicateRegistry` and the predicate names in a `CompiledRule`.
3. `PredicateAudit.FindUnused`, a static utility over a registry and a set of compiled rules.

## Decisions

- **Marker shape.** `PredicateSchema.Deprecation` is an optional `PredicateDeprecation(string? ReplacedBy, string? Message)`,
  set with `init` like `ArgumentValidator`. `null` means not deprecated. The change is additive and non-breaking.
- **Diagnostic.** New code `TRE0027`, severity Warning, always on (not an opt-in lint). One diagnostic per use of a
  deprecated predicate, at the call. The message names the replacement. The replacement is also a `DiagnosticSuggestion`.
  The rule still compiles. The code is the next after `TRE0026` in `DiagnosticCodes`.
- **Enumeration.** `PredicateRegistry<TContext>` gains a public `Schemas` list (today only `TryGetSchema` and an internal
  `Names` exist). `CompiledRule<TContext>` gains a public `PredicateNames` set (today only `Outline()` exposes terms).
- **Audit.** `PredicateAudit.FindUnused(registry, rules)` returns the registered schemas that no rule in `rules`
  references. It lives in the `TruthWeaver` package, takes compiled rules, and needs no rule store. Name matching uses the
  same normalized casing as term identity (CONTEXT.md).

## Out of scope

- Reporting deprecated predicates that are still used, beyond the compile warning.
- Compiling rule text inside the audit.
- Deprecating individual arguments.
- Reflection or `[Obsolete]` based discovery.

## Further notes

- Every ticket that adds public API carries XML docs, tests named per CLAUDE.md, and the full validation set.
- A new diagnostic code needs its entry in the diagnostics reference and in `DiagnosticCodes`.
- No operation or catalog predicate is added, so the K3 reference sync does not apply.
