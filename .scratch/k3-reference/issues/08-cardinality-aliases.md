# 08: Cardinality aliases: ANY, ALL, NONE, BETWEEN

**What to build:** Documents for the derived cardinality operations with canonical forms: ANY = AtLeast(1), ALL = AtLeast(n), NONE = AtMost(0), BETWEEN(min, max, ...) = AND(AtLeast(min), AtMost(max)). Each states the engine's minimum operand count and the BETWEEN rules (0 <= min <= max <= n, the full range is rejected as a constant), the K3 coincidences with OR, AND and NOT-OR (and why they remain distinct operations), the correct All() behaviour (False only when T+U < n) and any caveat found by the audit.

**Blocked by:** 07

**Status:** done

- [x] Four documents conform to the template with verified canonical forms
- [x] The corrected ALL behaviour is documented and checked by the harness
- [x] BETWEEN bound rules and the empty-range behaviour are explicit
- [x] Related-operation links to ticket 07 documents resolve
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) B2, B6. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added `cardinality/any.md`, `all.md`, `none.md` and `between.md` and linked them from `cardinality/README.md` (pending -> linked). Each has harness-checked Evaluation Tables (n = 2, 3 and a collapsed n = 4; `BETWEEN` for chosen bounds at n = 3 and one at n = 4) and `k3:canonical` blocks: `ANY` = `AtLeast(1)` = `OR` = `NOT NONE` = `GreaterThan(0)`, `ALL` = `AtLeast(n)` (n = 2, 3, 4) = `AND`, `NONE` = `AtMost(0)` = `NOT OR` = `NOT ANY` = `Exactly(0)` = `LessThan(1)` = `NOR` for two operands, `BETWEEN` = `AND(AtLeast(min), AtMost(max))` = `AND(AtLeast(min), NOT AtLeast(max + 1))` over every valid bound pair, plus De Morgan forms. The corrected `ALL` behaviour (False only when T+U < n; `ALL(U, U)` is Unknown) is explicit in Formal Semantics and Examples and enforced by the evaluation tables. A mutated table row was confirmed to fail the harness; the `K3Reference` tests pass (16). No Mermaid diagram. No source or test files changed.
- Verified against the engine with a throwaway probe (not committed): `ANY`, `ALL`, `NONE` and `BETWEEN` need two or more operands (`BRE0014`), unlike the threshold family, and this matches `OperatorDefinitions`, so no hardening mismatch; they have no aliases and are case-insensitive reserved words; JSON/YAML `op` is `any`/`all`/`none`/`between` (`min` and `max` required for `between`, else `BRE0014`); `BETWEEN` bounds outside `0 <= min <= max <= n` and the full range `0..n` are `InvalidThresholdValue` (`BRE0008`), a missing or non-integer bound is `BRE0001` in the DSL; `min > max` is rejected (the composition would return Unknown where the empty range is False; audit B6) and `min = max` is allowed; `Simplify`/`Canonicalize` collapse `ANY` to `OR` and `ALL` to `AND` but keep `NONE` and `BETWEEN`; `RuleBuilder.Any/All/None(IEnumerable)` fold (empty to False/True/True, one operand to itself/itself/its negation) while `Between(IEnumerable)` does not; no short-circuit, and a faulting operand records one `Fault`.
