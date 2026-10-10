# 08: MapChildren takes its child list from ExpressionShape

**What to build:** `MapChildren` reads the child list from `ExpressionShape` and keeps only the per-operator rebuild. Adding an operator edits one children switch and one rebuild switch, not two full switches. The rebuild-with-operands decision stays closed.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] `MapChildren` contains no case that only repeats the child list
- [ ] The `ExpressionShape` completeness test still fails when an operator has no shape
- [ ] `ExpressionTools` and `NodeShape` tests pass unchanged
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
