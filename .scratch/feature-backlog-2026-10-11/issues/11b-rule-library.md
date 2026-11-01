# 11b: Rule library: named rules referenced from other rules

**Group:** C author and manage rules

**What to build:** A rule library: named, compiled rules stored in a registry and referenced from other rules (for example `rule("adult-customer") AND hasCoupon`), expanded with the same compile-time expansion as ticket 11. Needs a store, versioning (a reference names a version or a hash), cross-rule cycle detection, and a decision on how a referenced rule's own registry and data-source declarations merge. Builds on 11 and links to the rule store (12). Grill the owner first.

**Blocked by:** 11

**Status:** needs-triage (design grilled and confirmed 2026-10-17; sub-tickets released 2026-11-01; this ticket is the design record and is not dispatched itself)

## Decisions (owner, 2026-10-17)

| Topic | Decision |
| --- | --- |
| Resolution | Both modes. `rule("x")` expands inline at compile time. `call("x")` is a runtime call to another compiled rule. The spellings are separate and the tree shows which is which. |
| Versioning | `rule("x")` uses the current version. An optional pin names the content hash the rule store already computes, for example `rule("x", hash: "ab12...")`. |
| Cycles | The compiler walks the library's graph, expand and call alike. Any cycle is a compile diagnostic. |
| Registry and data sources | A library rule has none of its own. Its predicates and data-source names resolve against the caller's compile. A miss is a diagnostic that names the library rule. |
| Placement | An immutable `RuleLibrary` lives in the `TruthWeaver` core with no new dependency. `TruthWeaver.Extensions.Configuration` builds a new library on each reload and swaps it atomically. |
| Parameters | None. A library rule is a complete rule. Reuse with arguments stays in definitions. |
| Call result | The callee's value is the call node's value. Its faults join the caller's fault list under the caller's fault budget, and its trace nests under the call node. Timeout and cancellation come from the caller. |
| Surfaces | Rule text, JSON, YAML and `RuleBuilder` together. |
| Missing rule or stale pin | A compile diagnostic. If a reload later removes or changes a rule that a `call` uses, the call node returns Unknown plus a Fault, so the decision fails closed. |

## Sub-tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [11b-1](11b-1-rule-library-adr.md) | ADR recording the decisions above | none |
| [11b-2](11b-2-rule-library-and-expansion.md) | `RuleLibrary`, `rule("x")` expansion in rule text, pins, cycle and missing-rule diagnostics | 11b-1 |
| [11b-3](11b-3-runtime-call.md) | `call("x")` evaluation, faults, trace and analysis | 11b-2 |
| [11b-4](11b-4-library-references-in-json-yaml-builder.md) | `rule` and `call` in JSON, YAML and `RuleBuilder` | 11b-3 |
| [11b-5](11b-5-library-from-the-rule-store.md) | The rule store builds and swaps the library on reload | 11b-3 |
| [11b-6](11b-6-library-cross-form-and-property-tests.md) | Cross-form and property tests | 11b-4, 11b-5 |

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages and dependencies)
- [ ] The pages that describe the behavior are written or updated in the same change (rule text, JSON and YAML formats, builder, diagnostics table, glossary), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
