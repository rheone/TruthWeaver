# 04: Spans and paths on findings (deferred)

**What to build:** A source span (DSL) or path (JSON/YAML) on every lint finding, so a UI can highlight the construct.

**Blocked by:** a design for node locations. `Expression` nodes keep none today, and structural equality must not include one.

**Status:** deferred

- [ ] Owner confirms the node-location design (side table keyed by node, or a field excluded from equality)
- [ ] Every lint finding carries `Span` or `Path`, matching how parse diagnostics already do
- [ ] Rewrites keep or drop locations by a stated rule
- [ ] Documentation updated in the same change: `docs/diagnostics.md` states where a lint finding points, for DSL and for JSON/YAML
- [ ] The full validation from CLAUDE.md passes

Roadmap verdict: "Not now" (2026-10-09). Related: [k3-followups 26](../../k3-followups/issues/26-diagnostic-properties-and-json-pointer.md).

See also [spec](../spec.md).
