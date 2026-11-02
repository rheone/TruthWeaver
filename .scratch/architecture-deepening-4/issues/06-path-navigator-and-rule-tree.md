# 06 - Path navigator, with the JsonNode adapter and RuleTree on it

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: refactor
Severity: low
Blocked by: 05
Effort: [architecture-deepening-4](../spec.md)

**What to build:** The path module owns "walk a parsed path through a tree". It defines a small `IPathNavigator<TNode>` (try the child for a key or an index; replace a child, which a read-only adapter refuses) and a `Locate` that walks the segments in order and returns the node, its parent and a replace action, or the segment where the walk stopped. A `JsonNode` adapter is the first adapter.

`RuleTree` and `RuleTreeEditor` move onto it:
- `TryLocate` (operands only) and `TryLocatePath` (any key or index) call `Locate`; `TryLocate` is `Locate` plus the check that every step is `operands` then an index.
- `OperandPath` and `ParentPath` become segment operations on the path (`Parent()`, `Operand(i)`), so no path text is cut or joined with `LastIndexOf`. Callers: 2 in `RuleTree`, 4 in `RuleTreeEditor`.
- `TryStep` is deleted.

`RewritePlayground` (3 `TryLocate` sites) keeps working through `RuleTree.TryLocate`.

**Constraints:** The `put` action semantics stay: replacing the root replaces its members (`ReplaceMembers`); replacing a child replaces it in its owner. No change to the tree path format.

**Acceptance criteria:**
- [ ] A test of `Locate` through the `JsonNode` adapter covers a key, an index, a missing step (with the stopping segment) and a replace.
- [ ] A test shows a rule whose key contains the text `.operands[` is no longer cut by `ParentPath`.
- [ ] No `LastIndexOf` or `IndexOf` on a path remains in `RuleTree` or `RuleTreeEditor`; `TryStep` is gone.
- [ ] Existing rule tree, rule tree editor and rewrite playground tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.
