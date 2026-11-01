# 80 - A repeated reference to a bad library rule reports the same error each time

Status: done
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
- [x] A test fails before the fix and passes after: a `let` definition holding `rule("missing")`, used several times, gives one `TRE0047` per distinct site; the same for a stale pin and a cycle.
- [x] Existing library tests pass unchanged.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Changing which diagnostics exist or their text.

## Resolution (2026-11-01)

`LibraryRuleExpander.Expand` consults its per-site cache before any check. The key is the reference name, its pin
(both upper-cased, because the library and the pin check compare without case) and the site span. A failed reference
(unknown rule, stale pin, cycle or an error in the library rule) is cached as failed, so a later reference at the same
site reports nothing more. The pin is in the key, so an unpinned and a stale pinned reference to one rule inside a
library rule (both at the outer site) are each checked.

Tests (`tests/TruthWeaver.Tests/RuleLibraryTests.cs`):

- `Compile_DefinitionHoldingABadReferenceUsedManyTimes_ReportsTheErrorOnce_Test` (theory: unknown rule, stale pin,
  cycle). Red first: the unknown-rule and stale-pin cases failed with three identical diagnostics (`Assert.Single()
  Failure: The collection contained 3 items`). The cycle case already passed, because the failed expansion of the
  outer library rule was cached; it stays as a guard.
- `Compile_UnpinnedAndStalePinnedReferenceInOneLibraryRule_ReportsTheStalePin_Test`: a guard for the pin in the key.

The existing `RuleLibraryTests` and `*Definition*` tests pass unchanged. CHANGELOG: one Fixed entry.
