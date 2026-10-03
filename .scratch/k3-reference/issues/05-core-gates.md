# 05: Core gates: NOT, AND, OR

**What to build:** One document each for NOT, AND and OR (the primitive Strong K3 connectives) following the approved template: classification, arity (AND and OR variadic with two or more operands in the engine; state the empty and single-operand identity conventions and how the engine treats them), domains, definition, syntax including the symbol and word forms, aliases, formal semantics and formula, exhaustive truth tables, examples with Unknown, edge cases (n-ary evaluation, short-circuit does not change the value, faults as Unknown) and related operations. A diagram only if it adds information.

**Blocked by:** 01, 02, 03

**Status:** done

- [x] Three operation documents conform to the approved template
- [x] Exhaustive tables for NOT, AND and OR (up to 4 operands for the variadic ones) pass the harness
- [x] Unknown cases and the F-dominates-AND and T-dominates-OR behaviour are explicit
- [x] Aliases are only those the engine actually accepts
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) section A. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added `gates/not.md`, `gates/and.md` and `gates/or.md` to the approved template and linked them from `gates/README.md` (pending -> linked). `AND` and `OR` carry exhaustive truth tables for 2, 3 and 4 operands (the 81-row table sits in a collapsed `<details>`), `NOT` its 3-row table; the template's Truth Table section holds several `k3:truth` markers because the Evaluation Table is mutually exclusive with it. Equivalent Forms hold harness-checked `k3:canonical` blocks (De Morgan, associativity, `OR` = `ANY` = `AtLeast(1, ...)`, `AND` = `ALL`, `NOT` = `NAND(a, a)` = `NOR(a, a)`); a mutation of a table row and of a form was confirmed to fail the harness. The `K3Reference` tests pass (16).
- Verified against the engine with a throwaway probe (not committed): `AND`/`OR` have no call form in the DSL (`AND(a, b)` is a syntax error; the README line calling them `AND(a, b, c)` is shorthand), JSON/YAML and `RuleBuilder.And/Or(params)` reject fewer than two operands (`BRE0014`), only `RuleBuilder.And/Or(IEnumerable)` folds empty to `True`/`False` and one operand to itself, aliases are exactly `&&`/`∧`, `||`/`∨`, `!`/`¬` (a lone `&`, `|` or `~` is a syntax error), the default mode stops after the first `False` (`AND`) or `True` (`OR`), `Exhaustive` runs every operand, and a faulting term gives `Unknown` plus one `Fault` unless a dominating operand precedes it. The Category line is bare (`Category: Gates / Operators`) with the index link on its own line because the harness compares the whole line. No Mermaid diagram: none adds information beyond the formula and tables. No source or test files changed.
