# 14: Keep the reference in sync as predicates (and operations) are added

**What to build:** A guard and a process so the reference library at docs/strong-k3/ never falls behind the engine. A check (run with the other gates, built on the verification harness from ticket 02) fails when a built-in predicate registered by the predicates package, or an operator known to the engine's operator tables, has no reference document, or when a reference document names a predicate or operator the engine does not have. Until the predicates are documented (ticket 13), the check reports predicates as "on hold" through an explicit, visible allow-list rather than silently skipping them; releasing the hold in ticket 13 empties the allow-list. Add the working rule to the places contributors and agents read: any ticket that adds, renames or removes a predicate or operation must add, rename or remove its reference document, update the category index and the root navigation, and re-run the harness (a line in CLAUDE.md, the ticket template notes and the reference's own README). Each newly added predicate gets its document following the approved template (Name, Classification, Kind, Arity, Input and Output Domain, Definition, Syntax, Aliases, Formal Semantics, plus the conditional sections that apply), including its null-selected-value behaviour and its Unknown cases.

**Blocked by:** 02, 04 (the guard itself); full predicate coverage additionally waits for 13 and for each predicate's own implementation ticket

**Status:** done

- [x] The sync check fails when a registered built-in predicate or a known operator has no reference document, and when a document refers to something the engine lacks (demonstrated with a fixture or a temporary example)
- [x] The predicate allow-list is explicit, committed and referenced from the check's failure message; ticket 13 removes it
- [x] CLAUDE.md (Reference sync) and the ticket workflow notes state the rule that adding or changing a predicate or operation updates its reference document in the same change, and `docs/doc-examples.md` documents the sync check. The reference README no longer states it: that section was removed, so the rule lives in CLAUDE.md and `docs/doc-examples.md`
- [x] Future predicate tickets are given an acceptance criterion pointing at this rule (add it to the predicate catalog track's ticket notes)
- [x] The check is wired into the same gates as the rest of the validation set and documented
- [x] Built test-first; the full validation set in CLAUDE.md passes

Source: owner request 2026-10-03 (keep the document in sync with additionally added predicates). See also [spec](../spec.md) and [predicate gap list](../../predicate-catalog/k3-gap-list.md).

## Comments

- Check: `tests/TruthWeaver.Tests/ReferenceDocs/K3ReferenceSyncChecker.cs` (run by `K3ReferenceSyncTests` in `dotnet test`, so it is in the same gate as the harness). It compares the engine's `OperatorDefinitions` table, the `Decision` methods `Project` and `Collapse`, and the public predicate factories of `TruthWeaver.Predicates` (found by reflection; the test project now references that package) with the files under `docs/strong-k3/`. Fixtures prove each failure: operator without a document, document for an unknown operator, unknown transformation, predicate without document or hold, stale hold, unknown hold entry, document for an unknown predicate.
- Predicate documents are named `<kind>-<factory>.md` (for example `string-equals.md`): factory class without `Predicates`, hyphen, method name, lower-case. This naming is my choice for ticket 13 to confirm.
- Allow-list: `K3PredicateDocumentationHold.Stems` (the predicates that have no document yet; the list in the file is the current set). The failure messages name it. Ticket 13 removes entries as it documents each predicate; an entry that has a document fails the check.
- Rule added to CLAUDE.md (Documentation, "Reference sync"), `docs/strong-k3/README.md`, `docs/agents/issue-tracker.md` (ticket workflow notes), `docs/doc-examples.md` (check table) and `.scratch/predicate-catalog/README.md` (acceptance criterion for predicate tickets).
- Remainder, not done: full predicate coverage waits for ticket 13 and each predicate's own ticket. The "full validation set" item is left for the integration run.
- 2026-10-04 bookkeeping: the full validation passed on the integrated branch, so the last box is ticked.
- 2026-10-04: Full predicate coverage landed with ticket 13. Every public predicate factory has a document and `K3PredicateDocumentationHold.Stems` is empty. The check keeps the allow-list for a future predicate that cannot be documented in the same change. Nothing remains open.
