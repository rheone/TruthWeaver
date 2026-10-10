# 04: Migrate NormalForms, Simplifier and Compressor threshold rules

**What to build:** The capped NNF expansion in `NormalForms` uses the expansion module (its `Exceeded` flag becomes the module's `null`). The `NOT`-flips-the-comparison rule in `Simplifier` and the threshold recognition in `Compressor` use the core instead of their own `K + 1` / `K - 1` arithmetic.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] `NormalForms` has no subset loop and no binomial/cost helpers that the expansion module provides
- [ ] `Simplifier` and `Compressor` hold no copy of the threshold negation or normalisation
- [ ] The normal-form, simplify and compress tests and the fuzzer checks pass unchanged
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
