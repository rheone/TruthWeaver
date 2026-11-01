# 11b-6: Cross-form and property tests for the rule library

**Parent:** [11b](11b-rule-library.md)

**What to build:** Tests that prove the library behaves the same on every surface: generated libraries with random references compile to equal trees from rule text, JSON, YAML and the builder; `rule("x")` evaluates the same as the inlined text; `call("x")` agrees with evaluating the callee alone; cycle detection finds every generated cycle.

**Blocked by:** 11b-4, 11b-5

**Status:** needs-triage (sub-tickets released 2026-11-01; dispatch in order after its blocker is done)

- [ ] Property tests cover expansion equality, call agreement and cycle detection
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
