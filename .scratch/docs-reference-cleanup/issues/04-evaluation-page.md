# 04: Evaluation page

**What to build:** A reader learns how an expression becomes a `Decision` from one page. `specification/evaluation.md` covers the expression and the `Decision`, evaluation modes, short-circuit and `NotEvaluated`, how a fault becomes `Unknown` and is recorded, and the fail-closed `IsSatisfied`. It includes the evaluation flow Mermaid diagram (predicate, fault or answer, `Unknown`, `Decision`, `Project` or `Collapse`, `IsSatisfied`). Each statement is checked against the engine.

**Blocked by:** 02

**Status:** done

- [x] The page states the behavior in present tense with no history or ADR references
- [x] The flow diagram is valid and agrees with the prose
- [x] A throwaway probe or an added test shows the short-circuit, fault and `IsSatisfied` claims hold
- [x] The page links only inside `docs/strong-k3/`
- [x] The reference harness and the lint test pass

## Comments

- 2026-10-04: Done. The short-circuit, fault, `IsSatisfied`, `Project` and `Collapse` claims were checked with a throwaway evaluation probe over 31 rules in both modes; the probe was deleted afterward. The probe confirmed that `NAND`, `NOR`, `IMPLIES` and `XOR` run every operand in both modes.
