# 03: Inspect hover and node facts use the term lookup

**Status:** done
**Type:** task
**Blocked by:** 01, 02
**Source:** architecture review of 2026-10-19, candidate 1

**What to build:** Inspect hover and tree-node facts take term outcomes from the Core lookup. `TermCalls` string matching (`SameCall`, `NameOf`, `Contains`) and the duplicated fault parsing are deleted.

- [x] `TermCalls` no longer matches terms by display text
- [x] Hover over a term in a rule with two same-name predicates shows the right value
- [x] Existing Inspect tests still pass
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

Inspect hover and node facts take term outcomes from `TermOutcomeLookup`. The predicate-name hover shows the value of the call under the cursor only (it falls back to the name list when no call covers the cursor). `FromFacts` takes a `TermOutcome` found by path or span. `TermCalls` and its tests are deleted (no caller was left). `EvaluationStepController.FaultOf` also used the duplicated fault parsing, so it now reads `TermOutcome.Faults` through `FindByPath`. New test: `DescribeOffset_TwoPredicatesWithTheSameName_GiveTheirOwnValues_Test`.

Noticed, not changed: the fault text is now the exception message, as `StructureController` already shows it.
