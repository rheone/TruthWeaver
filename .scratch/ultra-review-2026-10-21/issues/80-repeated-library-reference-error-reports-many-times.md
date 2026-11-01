# 80 - A repeated reference to a bad library rule reports the same error each time

Status: ready-for-agent
Category: bug
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

In the library expander, an unknown rule, a hash mismatch and a cycle return before the expansion cache is consulted, while a failed expansion that was cached reports once. A `let` definition whose body holds `rule("missing")` and is used 20 times gives 20 identical `TRE0047` diagnostics at the same span, where a parse error in the same library rule is reported once. (Review, 2026-11-01: the cache comment says a later reference to a failed expansion reports nothing more; the early returns bypass it. Verified by reading the code, not by running it.)

## Agent Brief

**Category:** bug
**Summary:** Report each distinct library-reference error once per site, whichever kind it is.

**Current behavior:** `rule("missing")` inside a definition body used N times yields N diagnostics with the same code, span and message. An error inside the library rule's text yields one.

**Desired behavior:** The same code at the same span and with the same message is reported once, for an unknown rule, a hash mismatch and a cycle, as for any other expansion failure.

**Acceptance criteria:**
- [ ] A test fails before the fix and passes after: a `let` definition holding `rule("missing")`, used several times, gives one `TRE0047` per distinct site; the same for a stale pin and a cycle.
- [ ] Existing library tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.

**Out of scope:** Changing which diagnostics exist or their text.
