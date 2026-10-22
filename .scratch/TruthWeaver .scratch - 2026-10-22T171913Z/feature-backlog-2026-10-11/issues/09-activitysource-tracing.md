# 09: ActivitySource tracing beside the metrics

**Group:** B run at scale

**What to build:** OpenTelemetry users expect `ActivitySource` next to the existing `TruthWeaverMetrics` meter: a span per evaluation with the decision value and fault count as tags, and child spans per predicate (opt-in, because of cost). Already deferred in `deferred-features`.

**Blocked by:** None (can start immediately)

**Status:** done (owner decision 2026-10-17: approved)

- [x] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [x] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- Implemented: `TruthWeaverTracing` (source `TruthWeaver`), `truthweaver.evaluate` span with decision and fault count tags, opt-in `EvaluationOptions.TracePredicates` child spans, `TracingTests`, `docs/observability.md`, CHANGELOG and ADR-0013 note. Full-validation box left unticked for the orchestrator.
