# 07: Insert menu

**What to build:** One menu inserts a predicate, an operator, a variable, a constant (`True`, `False`, `Unknown` and the letters `T`, `F`, `U`) or a definition at the cursor of the editor.

**Blocked by:** 05

**Status:** done

- [x] Each kind has its own entry; the predicate entry opens the predicate catalog and inserts a call with its argument placeholders.
- [x] An insert replaces the selection when there is one.
- [x] The inserted text compiles in every rule format the editor supports, or the menu says the kind is not available for that format. (Rule text only; JSON and YAML say the menu is not available.)
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
- [x] Every entry shows a short description (one line) next to its name: operators, the cardinality family, predicates, constants and definitions. Predicate descriptions come from the predicate catalog, and the other descriptions come from `OperatorDefinitions`, so there is one source for each. (Operator and constant descriptions are read from the outline of the compiled text, which uses `OperatorDefinitions`/`OperatorInfo`. The definition entries show the description of the sample definition and "A definition of the open rule." from the palette.)
- [x] A test fails when an inserted operator or predicate has no description.
