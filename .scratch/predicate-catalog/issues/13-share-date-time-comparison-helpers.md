# 13: Share the date-time comparison helpers

**What to build:** `DateTimePredicates.Single` and `DateTimePredicates.Range` duplicate `ScalarPredicateCore.Compare` and `ScalarPredicateCore.Range`, although `ScalarKinds.DateTimeOffset` exists. The copies have drifted: the reversed-bounds message text and the `ArgumentException` `paramName` differ (`DateTimePredicates` passes the lower-bound name, `ScalarPredicateCore` passes `nameof(args)`). Route `DateTimePredicates` through the shared helper so one implementation of comparison, range and null handling remains. Behavior stays the same: the tests stay green, and where the message or `paramName` must change, pick the `ScalarPredicateCore` form and update only the tests that assert the old text.

Also check whether the collection family reimplements the K3 `NOT` for its twins (the complement of the configured null answer). If it does, use the shared complement logic.

Minor cleanup candidate: `TypePredicates.StringTest(string)` ignores its parameter and always returns `true`. Review whether a simpler form removes it.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] No behavior change: `DateTimePredicatesTests`, `NotXTwinInvariantTests` and the clock predicate tests pass unchanged, except for any assertion on the reversed-bounds message or `paramName`
- [x] `DateTimePredicates` has no private copy of the compare or range logic
- [x] One reversed-bounds message and one `paramName` exist for numeric and date-time bounds
- [x] The collection twin complement and `TypePredicates.StringTest(string)` are reviewed, and each is changed or recorded as left alone with a reason
- [x] The full validation from CLAUDE.md passes

Source: [predicate-catalog ticket 06](06-date-time-comparison.md) and branch review, 2026-10-04.

## Comments

Done. `DateTimePredicates.Single` and `Range` are now thin wrappers over `ScalarPredicateCore.Compare` and `Range` with `ScalarKinds.DateTimeOffset`; no private compare or range logic remains.

Standardized reversed-bounds form (the `ScalarPredicateCore` one): `ArgumentException` with `ParamName` `args` and message `Predicate '<name>' has reversed bounds: '<lower>' (<value>) is greater than '<upper>' (<value>).` Characterization test added first (red): `BetweenAndOutside_ReversedBoundsWithNullSelection_ThrowSharedMessageAndParamName_Test`. No existing test asserted the old text.

Observable side effects, both from sharing the helper: the date-time range argument descriptions now read `The inclusive lower bound (date-time).` and `The inclusive upper bound (date-time). It must not be less than the lower bound.`; and the single-instant comparison reads its argument before the selector (a missing argument now throws even for a null selection; arguments are validated at compile time). The bound values in the message use default `DateTimeOffset` formatting instead of `O`.

Review: `CollectionPredicates` has no private K3 NOT; its builders already call `PredicateResult.ForNullAsync(nullBehavior, negate)`, so it is left alone. `TypePredicates.StringTest(string)` is removed; the two `string?` overloads of `IsString`/`IsNotString` pass `static _ => true`; `StringTest(object)` stays. Validation: build (CI=true), csharpier, `dotnet format`, roslynator clean; all test projects pass.
