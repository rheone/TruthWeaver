# 09: ActivitySource tracing beside the metrics

**Group:** B run at scale

**What to build:** OpenTelemetry users expect `ActivitySource` next to the existing `TruthWeaverMetrics` meter: a span per evaluation with the decision value and fault count as tags, and child spans per predicate (opt-in, because of cost). Already deferred in `deferred-features`.

**Blocked by:** None (can start immediately)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
