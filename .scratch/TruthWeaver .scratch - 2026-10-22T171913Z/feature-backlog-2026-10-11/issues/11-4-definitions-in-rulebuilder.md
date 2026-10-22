# 11-4: Definition API in RuleBuilder

**Parent:** [11](11-named-sub-rules-and-parameters.md)

**What to build:** A definition API on `RuleBuilder` that produces the same compiled result as the other three forms (the builder already reuses with C# variables; this adds named, parameterised definitions that go through the same expansion, guardrails and diagnostics). Keep the API small and documented.

**Blocked by:** 11-2

**Status:** done

- [ ] Builder definitions compile to trees equal to the rule-text form
- [ ] Guardrail diagnostics from 11-2 are reported for builder input
- [ ] Public members are documented
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
