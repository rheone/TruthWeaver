# 02: Predicate conventions page

**What to build:** An author finds, on one page, how every built-in predicate treats case, whitespace, range ends, null and culture, and which settings change them. The page states the conventions in the spec, includes the table of existing settings, and says that a regex author writes `(?i)` for case-insensitive matching. The string, collection, regex, numeric and date predicate pages state their fixed behavior in one sentence each and link to it.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] `docs/predicate-conventions.md` exists, follows `docs/agents/documentation-standard.md`, and is linked from the root `README.md` and `docs/predicates.md`
- [ ] The page states the contrast between closed value ranges, strict comparisons and half-open time windows
- [ ] The `InTimeWindow` and `NotInTimeWindow` pages say the default is half-open and name `includeStart` and `includeEnd`
- [ ] Runnable examples carry doctest markers per `docs/doc-examples.md`, and `DocumentationLint` passes
- [ ] The follow-up "Configurable variants for `Contains`, `StartsWith`, `EndsWith`" is recorded under `.scratch/` as an unscheduled item
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
