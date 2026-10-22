# 14: Coverage over saved cases

**What to build:** Show which parts of the rule the saved test cases (ticket 07) exercise, using the rule coverage of the library (`docs/rule-coverage.md`). The tree pane marks nodes that no case reaches, and a summary line gives the share covered.

**Blocked by:** none

**Status:** done

- [x] The marks and the summary equal the library's coverage result for the same rule and cases
- [x] A rule with no saved cases shows a message, not zero percent
- [x] Coverage updates when a case is added, changed or removed
- [x] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- Coverage is `RuleCoverage.FromDecisions` over one decision per saved case. Each case is evaluated with the stub evaluator (its `given` values and its variables) in the default short-circuit mode, on the canonical text of the last valid tree. Logic is in `CaseCoverage` (`src/TruthWeaver.Tool.Tui`); no core library change.
- Owner decision not recorded: "a node no case reaches" is a node with `EvaluatedCount == 0` in the library result. Exhaustive mode would reach every node, so short-circuit is the mode that makes the mark useful. The "decided" counts of the library are not shown; the ticket asks for reach only.
- Marks: the line of an unreached node ends with `  [no case]` (text, so it does not depend on color). Summary: a second line in the Tree pane, `Coverage: 2 of 3 node(s) reached by 1 case(s) (67%).`, from the same node list. A case that cannot be evaluated is left out and the line says how many.
- No cases (or no valid rule) shows a message and no marks. The cases come from the open rule test file (`RuleTestSession.Rows`), so a file must be opened (Enter in the path field) or saved to first. `Workbench.Coverage` caches by the rule and the list of rows, and the Tree pane refreshes on `RuleTestsChanged`.
- Page: `docs/tui.md` (Rule tests, "Coverage over saved cases"). Checked: Tui tests (268), Architecture tests (63) and the documentation tests pass; csharpier and `dotnet format --severity info` are clean on the changed files. The full-repo format and Roslynator run were not done here (other work is in flight in the tree).
