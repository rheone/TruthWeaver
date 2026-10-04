# 11: Catalog-wide NotX twin invariant test

**What to build:** One test guards the twin rule for the whole catalog. It enumerates every registered predicate, requires a `NotX` twin for each positive one (and no twin for a predicate that is itself a negation), and asserts for True, False and Unknown selected values that `NotX(v)` equals `NOT X(v)`. A deliberately missing twin or a twin that maps Unknown to a definite value makes it fail.

**Blocked by:** 03, 04, 05, 06, 08 (and 07 if its predicates take twins)

**Status:** ready-for-agent

- [ ] The test covers every registered predicate and fails when a twin is missing
- [ ] The test fails for a twin that does not return Unknown for an Unknown selected value
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
