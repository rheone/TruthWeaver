# 12 - YamlNode and JsonElement adapters, with DocumentRewrite and EmbeddedRuleReader on the navigator

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: refactor
Severity: low
Blocked by: 06
Effort: [architecture-deepening-4](../spec.md)

**What to build:** `IPathNavigator<TNode>` from ticket 06 gets two more adapters, `YamlNode` and `JsonElement` (read-only: its replace refuses). The two consumers that walk a parsed `DocumentPath` themselves move onto `Locate`:

- `DocumentRewrite`: its `Child(JsonNode, segment)` and `Child(YamlNode, segment)` helpers and the loops that call them. It also replaces and deletes; the adapters supply the replace, and delete stays with the consumer.
- `EmbeddedRuleReader`: its JSON walk (`TryGetChild` over `JsonElement`) and its YAML walk (`Child` over `YamlNode`), including the "not found at segment N" error position.

With three adapters the seam is real.

**Constraints:** Error messages and positions that tests read ("not found at segment N", "not a rule") stay the same. `DocumentOutline` builds segments while it walks a whole document; it is not a locate and stays as it is.

**Acceptance criteria:**
- [ ] A test of `Locate` per new adapter: key, index, missing step with the stopping segment, and (YAML) replace.
- [ ] `DocumentRewrite` and `EmbeddedRuleReader` have no loop of their own over `DocumentPath.Segments`.
- [ ] Existing document rewrite and embedded rule tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.
