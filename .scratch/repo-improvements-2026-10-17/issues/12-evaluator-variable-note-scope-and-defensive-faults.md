# 12: Variable outcome note inside quantifiers, and faults on defensive Unknown paths

**Parent:** [05](05-unknown-consistency-audit.md) D3 and D5 (owner decisions 2026-10-17)

**Blocked by:** None

**Status:** ready-for-agent

**What to build:** Two fixes in `Evaluator.cs`. (D3) `ResolveVariablesAsync` stores the variable outcome note under `(this.scope, identity)` but `Annotated` reads `(this.memoScope, identity)`, so the trace loses `[ref: resolved]` inside a quantifier body. Key the write by `memoScope` (the doc of `EvaluationState.VariableOutcomes` already says it is keyed like `Memo`). (D5) `InvokeAsync` (unregistered predicate) and `EvalQuantifierAsync` (unregistered collection) return `Unknown` with no Fault; record a Fault in both so `Decision.Faults` says why.

- [ ] A test shows the outcome note inside a `FORALL`/`EXISTS` body for a `from(...)` term
- [ ] A test reaches each defensive path (build a compiled rule whose registry lacks the name, if needed) and sees a Fault
- [ ] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
