# 07: Cardinality primitives

**What to build:** Documents for AtLeast, AtMost, Exactly, GreaterThan, LessThan and ExactlyOne, all defined by the definitely-true / possibly-true interval [T, T+U] over the true-count. Each has an Evaluation Table (parameterized and variadic, so a truth table is impractical) with the complete semantic definition, representative cases, boundary cases, the valid range of k and what is rejected, Unknown cases, empty and single-operand behaviour, and the reduction of AtLeast(1) to OR and AtLeast(n) to AND where it holds. A single interval diagram if it materially helps.

**Blocked by:** 05

**Status:** done

- [x] Six documents conform to the template with evaluation tables that pass the harness
- [x] Valid k ranges match the engine's compile-time rules
- [x] AtLeast(1) = OR and AtLeast(n) = AND are verified before being documented
- [x] The difference between ExactlyOne and PARITY is cross-linked
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) section A and appendix E.9. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added `cardinality/atleast.md`, `atmost.md`, `exactly.md`, `greaterthan.md`, `lessthan.md` and `exactlyone.md` and linked them from `cardinality/README.md` (pending -> linked for these six). Each has harness-checked Evaluation Tables (every `k` for three operands; one collapsed four-operand table; `Exactly` also one operand) and `k3:canonical` blocks: `AtLeast(1)` = `OR` and `AtLeast(n)` = `AND` (n = 2, 3, 4), `AtLeast(1, a)` = `a`, the `AtLeast`/`AtMost` negation duals, `AtMost(k)` = `NOT AtLeast(k + 1)`, `AtMost(0)` = `NONE`, `Exactly(k)` = `AND(AtLeast(k), AtMost(k))`, `GreaterThan(k)` = `AtLeast(k + 1)` = `NOT AtMost(k)`, `LessThan(k)` = `AtMost(k - 1)` = `NOT AtLeast(k)`, `GreaterThan(0)` = `OR`, `LessThan(1)` = `NONE`, `ExactlyOne` = `Exactly(1)`, and `ExactlyOne` = `XOR` for two operands. `ExactlyOne` versus `PARITY` versus `XOR` is contrasted and cross-linked with `derived/parity.md`. A mutated table row and a mutated form were confirmed to fail the harness; the `K3Reference` tests pass (16). No Mermaid diagram: the tables carry the interval, and a diagram would add nothing the formula does not.
- Verified against the engine with a throwaway probe (not committed): valid `k` ranges are `AtLeast` 1..n, `AtMost` 0..n-1, `Exactly` 0..n, `GreaterThan` 0..n-1, `LessThan` 1..n, rejected as `InvalidThresholdValue` (`BRE0008`); zero operands is `MalformedTree` (`BRE0014`); a missing or non-integer `k` is `BRE0001` in the DSL and `BRE0014` in JSON and YAML; one operand is accepted by the threshold family (owner decision 10A, documented with a note; mismatch tracked in hardening ticket 11) but `ExactlyOne` needs two (`BRE0014`); `RuleBuilder.GreaterThan` and `LessThan` have no `IEnumerable` overload, and the `IEnumerable` overloads of the other thresholds do not fold an empty list while `RuleBuilder.ExactlyOne(IEnumerable)` folds empty to `False` and one operand to itself. There are no aliases; words are case-insensitive. No operand short-circuits and a faulting operand always records one `Fault`. No source or test files changed.
