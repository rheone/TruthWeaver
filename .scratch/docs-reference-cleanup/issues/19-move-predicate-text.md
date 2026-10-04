# 19: Move the predicate text

**What to build:** A reader finds the predicate types on `docs/predicates.md`: stateless lambdas, class-based predicates with injected dependencies, externally selected values, and a link to `docs/data-sources.md` for variable references. The `ageVariable` doctest moves with its text.

**Blocked by:** 18

**Status:** done

- [x] `docs/predicates.md` follows the standard and is not on the baseline
- [x] The page is added to the doctest list, and `ageVariable` still compiles
- [x] The README section is replaced by a link, and its table of contents matches
- [x] No link is broken, and the README doctests still pass
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: Created `docs/predicates.md` from the README Predicate types section (ADR links, the breaking-change callout and the deferred-feature link restated as rules). `ageVariable` moved with its text and links to `docs/data-sources.md`. The README keeps the `## Predicate types` heading as a one-paragraph link, so the TOC and the `#predicate-types` links in Examples still resolve. The page links to `../README.md#3-named-arguments` and `../README.md#1-a-single-predicate`; ticket 20 must redirect them to `docs/examples.md`. Registered in `DocExampleTests` and `docs/doc-examples.md`. README is now 1,234 lines.
