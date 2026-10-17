# 05: Lenient unknown predicate records no Fault

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

**What to build:** Decide whether an unknown predicate in lenient mode should add a Fault. ADR-0002 says it compiles as a permanent Unknown fault on that term.

**Evidence:** Evaluator.cs:489-496 returns Unknown with a trace entry and no Fault; Expression.cs:79-83 documents it as never invoked. Decision.Faults is the record of why a result is Unknown, so the omission may hide the reason.

- [ ] Code, docs and ADR-0002 state the same behavior
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
