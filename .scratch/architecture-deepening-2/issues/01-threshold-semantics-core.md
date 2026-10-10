# 01: Threshold semantics core

**What to build:** One internal module in `Ast` that knows what the threshold comparisons mean, with no `Expression` in its interface. Given a comparison, `k` and an operand count it returns the normalised form (`GreaterThan(k)` is `AtLeast(k+1)`, `LessThan(k)` is `AtMost(k-1)`), the negation (`AtMost(k)` is `NOT AtLeast(k+1)`), the constant outcome when `k` is out of range, and the `Between` split. `Canonicalizer` and `Analyzer` call it and lose their private copies.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The core is tested directly at its own interface, including both boundaries (`k <= 0`, `k > n`) for every comparison
- [ ] `Canonicalizer` and `Analyzer` hold no copy of the `k+1` / `k-1` normalisation
- [ ] `EvaluatorAnalyzerPinningTests` and the canonical-form tests pass unchanged
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
