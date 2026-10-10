# 10: One internal type for the K3 scalar truth functions

**What to build:** All scalar Strong Kleene functions (`Not`, `And`, `Or`, `Xor`, `Equivalent`, `Nand`, `Nor`, `Implies`, `Parity`, `Inspect`) move to one internal type. `Evaluator`, `Simplifier` and `NormalForms` call it and lose their copies (`NnfSupport.Negate(TruthValue)`, `Simplifier.Inspected`). `K3Oracle` in `TruthWeaver.Testing` stays independent. The parked candidate in `.scratch/architecture-deepening/closed-and-parked-candidates.md` is recorded as closed.

**Blocked by:** 04 (both edit `NormalForms` and `Simplifier`)

**Status:** ready-for-agent

- [ ] The truth functions are tested directly over all 9 input pairs and 3 single values
- [ ] No truth table remains in `Evaluator`, `Simplifier` or `NormalForms`
- [ ] `K3Oracle` is untouched, and the closed-and-parked note records the decision
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
