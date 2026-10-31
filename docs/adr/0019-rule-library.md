# ADR-0019: Rule library

## Status

Accepted (2026-10-17). Decided with the owner in the grilling of feature backlog ticket 11b. This ADR records the
decisions only. The names `RuleLibrary`, `rule` and `call` are the owner's. The diagnostic codes and the exact API are
chosen when the sub-tickets 11b-2 to 11b-5 land. It builds on the definitions of
[ADR-0008](0008-definitions-and-parameters.md) and the rule store of [ADR-0016](0016-configuration-rule-store.md).

## Context

A definition is reusable inside one rule document. A host that keeps many rules has no way to reuse one rule inside
another, so it copies text. A host also needs a rule to call another compiled rule at run time, for a rule that changes
more often than the rules that use it.

## Decision

1. **Two reference spellings.** `rule("x")` expands the named library rule inline at compile time, with the same
   expansion as a definition. `call("x")` is a runtime call to another compiled rule. The spellings are separate and the
   compiled tree shows which one a node is.
2. **Versioning by an optional hash pin.** `rule("x")` uses the current version of `x`. A reference can pin the content
   hash that the rule store already computes, as in `rule("x", hash: "ab12...")`. A library has no version numbers.
3. **Cycles are a compile error.** The compiler walks the graph of the library, expand and call alike. Any cycle is a
   compile diagnostic.
4. **A library rule has no registry or data sources of its own.** Its predicates and data-source names resolve against
   the compile of the caller. A name that the caller does not hold is a diagnostic that names the library rule. The
   registry and the data sources stay owned by the caller.
5. **An immutable `RuleLibrary` in the core.** It lives in `TruthWeaver` and adds no dependency. The package
   `TruthWeaver.Extensions.Configuration` builds a new library on each reload and swaps it with one reference write.
6. **A library rule takes no parameters.** It is a complete rule. Reuse with arguments stays in definitions.
7. **A call folds into the caller.** The value of the callee is the value of the call node. The faults of the callee join
   the fault list of the caller under the fault budget of the caller. The trace of the callee nests under the call node.
   The timeout and the cancellation come from the caller.
8. **A missing rule or a stale pin is a compile diagnostic.** A reference to a rule that the library does not hold, or a
   pin that does not match the current content hash, fails the compile.
9. **A reload that removes or changes a called rule fails closed at run time.** If a reload later removes or changes a
   rule that a `call` uses, the call node returns `Unknown` and records a `Fault`, as in
   [ADR-0001](0001-kleene-failure-model.md).
10. **Every surface.** Rule text, JSON, YAML and `RuleBuilder` all read and write `rule` and `call`.

## Considered options

- **Expand only.** A rule that changes would need a recompile of every rule that uses it. Rejected because the call
  spelling gives a host a way to change a callee without a recompile of its callers.
- **Call only.** A host could not inline a library rule at compile time. Rejected because the owner wants both modes.
- **One spelling that the compiler chooses.** The tree would not show which behavior a node has. Rejected.
- **Version numbers in the reference.** The library would need to store old versions. Rejected because the content
  hash identifies a version without a new store.
- **A registry or data sources in each library rule.** Two rules in one compile could bind one name to two
  implementations. Rejected, so a library rule resolves its names against the caller.
- **Parameters on a library rule.** This duplicates definitions. Rejected.

## Consequences

- Sub-tickets 11b-2 to 11b-6 build the library and the expansion, the runtime call, the other formats, the rule store
  link and the cross-form tests. The order follows the "Blocked by" lines of the tickets.
- Each new diagnostic code gets a row in the diagnostics table when it is added.
