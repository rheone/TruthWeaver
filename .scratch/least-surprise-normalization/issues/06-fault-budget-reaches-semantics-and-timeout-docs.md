# 06: FaultBudget aborts when faults reach the budget; Timeout is documented

**What to build:** `FaultBudget = N` aborts evaluation when the Nth fault is recorded, as the public XML doc and ADR-0002 say. `null` means unlimited, and a budget below 1 is rejected when the options are validated. The `Timeout` documentation, `CONTEXT.md` and the project summary say that a timeout throws `OperationCanceledException` and does not become a fault.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The test that pinned the old `>` reading is replaced with one that pins `>=`; budgets of 1, 2 and a budget below 1 are covered
- [ ] The `Evaluator` comment that favours a ticket over the ADR is removed
- [ ] `EvaluationOptions` XML docs, `CONTEXT.md` and `CLAUDE.md` agree on both behaviors
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
