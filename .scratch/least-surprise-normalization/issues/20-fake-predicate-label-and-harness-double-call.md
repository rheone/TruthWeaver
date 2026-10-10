# 20: FakePredicates derive their label; the harness documents its double call

**What to build:** A fake predicate's default label comes from its name, so rendered diagrams do not show "Fake" for every term. The `PredicateHarness` XML doc and `docs/predicate-harness.md` say that each case calls the predicate twice, so a predicate with side effects will see two calls.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A test shows distinct default labels
- [ ] Both docs state the double call
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
