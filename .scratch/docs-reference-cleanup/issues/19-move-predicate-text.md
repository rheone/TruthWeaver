# 19: Move the predicate text

**What to build:** A reader finds the predicate types on `docs/predicates.md`: stateless lambdas, class-based predicates with injected dependencies, externally selected values, and a link to `docs/data-sources.md` for variable references. The `ageVariable` doctest moves with its text.

**Blocked by:** 18

**Status:** ready-for-agent

- [ ] `docs/predicates.md` follows the standard and is not on the baseline
- [ ] The page is added to the doctest list, and `ageVariable` still compiles
- [ ] The README section is replaced by a link, and its table of contents matches
- [ ] No link is broken, and the README doctests still pass
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
