# 06: Derived logical gates: XOR, EQUIVALENT, IMPLIES, NAND, NOR, PARITY

**What to build:** One document each for the derived logical operations, with the canonical primitive form verified under Strong K3: XOR (binary), EQUIVALENT (aliases IFF and XNOR), IMPLIES (Kleene strong implication NOT A OR B, contrasted with Lukasiewicz), NAND, NOR and PARITY (n-ary parity, Unknown if any operand is Unknown; the former NXOR name is removed and not documented as an alias, with a note about why the name changed). Each states the arity, canonical form, truth table, whether a classical identity still holds, and edge cases such as XOR with more than two operands pointing at PARITY. A decomposition diagram only where it clarifies (for example the XOR and EQUIVALENT composition).

**Blocked by:** 05, k3-followups 06

**Status:** done

- [x] Six documents conform to the template, each with a verified canonical form or an explicit statement that none is established
- [x] Truth tables pass the harness
- [x] PARITY versus ExactlyOne versus XOR for three or more operands is explicitly contrasted
- [x] Diagrams, where present, are semantically identical to the documented formula
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) sections A and D. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added `derived/xor.md`, `equivalent.md`, `implies.md`, `nand.md`, `nor.md` and `parity.md` and linked them from `derived/README.md` (pending -> linked; the index and the documents now use the harness category label "Derived Logical Operations"). Tables (2 operands each; `PARITY` 2, 3 and 4 with the 81-row one collapsed) and canonical or equivalent forms (including `NOT(XOR)`, `AND(IMPLIES, IMPLIES)`, contraposition, De Morgan, `NONE`, `EXACTLYONE`, the odd-count `EXACTLY` disjunction and the `XOR` fold for `PARITY`) are harness-checked; a mutated table row and a mutated form were confirmed to fail. `K3Reference` tests pass (16). Two small flowcharts (XOR and EQUIVALENT composition) were checked by hand against the canonical forms; none for the other four.
- Verified against the engine with a throwaway probe (not committed): unlike `AND`/`OR`, a wrong operand count for `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` (chain in the DSL, JSON or YAML) is `BRE0006` `InfixArityViolation`, not `BRE0014` (the `XOR` message names `PARITY` and `ExactlyOne`); `RuleBuilder` takes exactly two arguments. `PARITY` does have a DSL call form (`PARITY(a, b, c)`, case-insensitive) and `BRE0014` below two operands; only `RuleBuilder.Parity(IEnumerable)` folds empty to `False` and one to itself. Aliases are `⊕ ⊻`, `→ ⇒`, `↔ ⇔` plus words `IFF`, `XNOR`, `↑ ⊼`, `↓ ⊽` (`^`, `<=>`, `->`, `=>` are syntax errors); `NXOR` is rejected with a rename message in DSL and JSON and is not documented as an alias. Mixing with `AND`/`OR` is `BRE0007`. None of these six short-circuits: every operand runs and a faulting operand always records its fault. No source or test files changed.
