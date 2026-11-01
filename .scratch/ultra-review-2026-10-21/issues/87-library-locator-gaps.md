# 87 - The library locator is missing from some diagnostics, trace entries and rewritten rules

Status: ready-for-agent
Category: enhancement
Severity: low
Lens: Follow-up to ticket 83
Blocked by: none

## Finding

Ticket 83 added a library locator (`... In library rule 'adult' at line 1, column 7.` for a diagnostic; `a (in library rule 'adult' at line 1, column 1)` for a trace entry). The agent left these places without it, as a narrow choice:

- The quantifier's own flat trace entry (written by the collection binding) has no locator, though its body nodes do.
- Analyzer and linter findings on the whole tree have no span and no locator.
- A rewritten `CompiledRule` (`OptimizeOrder` and the other rewrites) drops the locators, the same way it drops notes.
- Only the flat `Decision.Trace` entries carry the locator; the trace tree, coverage and the printers do not.

## Open question for the owner

Which of these should carry the locator? Is the message wording acceptable? A rewrite that drops the locator means a trace from an optimized rule loses it.

## Next step

Decide, then write an Agent Brief; the rewrite case is likely the one that matters.

## Decision (owner, 2026-11-01)

Add the library locator to two more places: the quantifier's own flat trace entry, and a rewritten `CompiledRule` (`OptimizeOrder` and the other rewrites keep the locators). The wording stays as it is: `In library rule 'adult' at line 1, column 7.` for a diagnostic and `a (in library rule 'adult' at line 1, column 1)` for a trace entry. The trace tree, coverage, the printers and analyzer and linter findings stay without a locator.

## Agent Brief

**Category:** enhancement
**Summary:** Keep the library locator on a trace entry for a library quantifier and across the rewrites of a compiled rule.

**Current behavior:** The flat trace entry written for a `FORALL` or `EXISTS` node from a library rule has no locator, though its body nodes do. A rewritten `CompiledRule` drops the locators, as it drops notes, so a trace from an optimized rule loses them.

**Desired behavior:** The quantifier's flat trace entry ends with the same locator form as its body nodes. Each rewrite that returns a `CompiledRule` (`OptimizeOrder`, `Simplify`, `Canonicalize` and the others that rebuild the tree) carries the locators of the nodes it keeps. A node a rewrite creates, or merges from several nodes, takes the locator of its source node when there is exactly one, and none otherwise.

**Acceptance criteria:**
- [ ] A test fails before and passes after: a library rule holding a quantifier gives a flat trace entry for the quantifier that names the library rule.
- [ ] A test: after `OptimizeOrder` (and one other rewrite), a trace entry for a node from a library rule still names the library rule.
- [ ] A rule with no library reference is unchanged.
- [ ] Docs (`docs/rule-text.md` Library rules) state where the locator appears. CHANGELOG entry. The validation list in CLAUDE.md passes.

**Out of scope (deliberate):** The trace tree, coverage, the printers, and analyzer and linter findings.
