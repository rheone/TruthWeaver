# 09: Compiler validates operand counts from the operator table

**What to build:** `RuleNodeCompiler` checks operand counts against `MinOperands` and `MaxOperands` from the operator table instead of literal counts and per-operator checks. One arity fact then serves the DSL, JSON, YAML and the builder. Where an operator's rule cannot be expressed by the table (for example thresholds), the table or the compiler states why.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] No literal operand count remains in `RuleNodeCompiler` for operators the table describes
- [ ] Every arity diagnostic keeps its code and text, or the change is listed and `docs/diagnostics.md` is updated to match
- [ ] `OperatorDefinitionsTests` now has a production reader to guard
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
