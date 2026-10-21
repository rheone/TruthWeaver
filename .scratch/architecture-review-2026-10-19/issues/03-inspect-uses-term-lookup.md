# 03: Inspect hover and node facts use the term lookup

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 01, 02
**Source:** architecture review of 2026-10-19, candidate 1

**What to build:** Inspect hover and tree-node facts take term outcomes from the Core lookup. `TermCalls` string matching (`SameCall`, `NameOf`, `Contains`) and the duplicated fault parsing are deleted.

- [ ] `TermCalls` no longer matches terms by display text
- [ ] Hover over a term in a rule with two same-name predicates shows the right value
- [ ] Existing Inspect tests still pass
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
