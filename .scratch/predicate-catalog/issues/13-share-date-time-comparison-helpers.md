# 13: Share the date-time comparison helpers

**What to build:** `DateTimePredicates.Single` and `DateTimePredicates.Range` duplicate `ScalarPredicateCore.Compare` and `ScalarPredicateCore.Range`, although `ScalarKinds.DateTimeOffset` exists. The copies have drifted: the reversed-bounds message text and the `ArgumentException` `paramName` differ (`DateTimePredicates` passes the lower-bound name, `ScalarPredicateCore` passes `nameof(args)`). Route `DateTimePredicates` through the shared helper so one implementation of comparison, range and null handling remains. Behavior stays the same: the tests stay green, and where the message or `paramName` must change, pick the `ScalarPredicateCore` form and update only the tests that assert the old text.

Also check whether the collection family reimplements the K3 `NOT` for its twins (the complement of the configured null answer). If it does, use the shared complement logic.

Minor cleanup candidate: `TypePredicates.StringTest(string)` ignores its parameter and always returns `true`. Review whether a simpler form removes it.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] No behavior change: `DateTimePredicatesTests`, `NotXTwinInvariantTests` and the clock predicate tests pass unchanged, except for any assertion on the reversed-bounds message or `paramName`
- [ ] `DateTimePredicates` has no private copy of the compare or range logic
- [ ] One reversed-bounds message and one `paramName` exist for numeric and date-time bounds
- [ ] The collection twin complement and `TypePredicates.StringTest(string)` are reviewed, and each is changed or recorded as left alone with a reason
- [ ] The full validation from CLAUDE.md passes

Source: [predicate-catalog ticket 06](06-date-time-comparison.md) and branch review, 2026-10-04.

## Comments
