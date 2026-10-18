# 07: Insert menu

**What to build:** One menu inserts a predicate, an operator, a variable, a constant (`True`, `False`, `Unknown` and the letters `T`, `F`, `U`) or a definition at the cursor of the editor.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Each kind has its own entry; the predicate entry opens the predicate catalog and inserts a call with its argument placeholders.
- [ ] An insert replaces the selection when there is one.
- [ ] The inserted text compiles in every rule format the editor supports, or the menu says the kind is not available for that format.
- [ ] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
- [ ] Every entry shows a short description (one line) next to its name: operators, the cardinality family, predicates, constants and definitions. Predicate descriptions come from the predicate catalog, and the other descriptions come from `OperatorDefinitions`, so there is one source for each.
- [ ] A test fails when an inserted operator or predicate has no description.
