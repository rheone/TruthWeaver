# 87 - The library locator is missing from some diagnostics, trace entries and rewritten rules

Status: needs-triage
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
