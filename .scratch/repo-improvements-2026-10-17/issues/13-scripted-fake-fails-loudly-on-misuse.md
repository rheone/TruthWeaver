# 13: `FakePredicates.Scripted` fails loudly on misuse

**Parent:** [05](05-unknown-consistency-audit.md) D7 (owner decision 2026-10-17)

**Blocked by:** None

**Status:** ready-for-agent

**What to build:** The `Scripted` docs say it "fails loudly", but an under-scripted fake throws `InvalidOperationException`, which the evaluator absorbs into `Unknown` plus a Fault, so `HaveResult(Unknown)` can pass by accident. Owner decision: it fails loudly only on test misuse (an exhausted or misconfigured script); an exception the script deliberately throws is still absorbed as `Unknown` plus a Fault, as for any predicate (ADR-0001). The evaluator must let the misuse signal escape without weakening ADR-0001 for production predicates; choose the smallest mechanism that keeps the escape confined to `TruthWeaver.Testing` (for example a dedicated exception type that the evaluator rethrows, or a checkable recorded misuse), report the choice with alternatives, and update the docs to say exactly what happens.

- [ ] A test shows an exhausted script fails the test run instead of passing as `Unknown`
- [ ] A test shows a deliberately scripted exception is still `Unknown` plus a Fault
- [ ] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
