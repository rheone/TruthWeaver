# 13: Compare two rules

**What to build:** Open a second rule beside the current one and show whether it is looser, tighter, equivalent or unrelated (the looser-or-tighter comparison of the library), plus the overlap result (`RuleOverlap.Compare`) with an example assignment where they differ. The second rule loads from a file or from the clipboard and is read-only.

**Blocked by:** none

**Status:** done

- [x] The pane shows the same relation and overlap as the library calls for the same two rules
- [x] A second rule that does not compile shows its diagnostics and no comparison
- [x] An example assignment from the pane can be loaded into the Assign pane
- [x] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- New Compare pane (right region, fifth pane) and key Ctrl+F9; `CompareController` holds the second rule as a `RuleDocument` that no key edits. It loads from the clipboard or a file (format detected). A source that does not compile loads too and the pane shows its diagnostics and no comparison.
- Choices: the second rule is compared as the "after" rule against the open rule (`RuleImplication.Compare(open, second)`): equivalent, tighter, looser, or unrelated (Overlapping and Disjoint both read as unrelated, with the reason). The overlap uses `RuleOverlap.Compare(open, second)` with the `MaxAnalysisTerms` cap of the settings and lists an example for each reachable pair of different values. The comparison follows edits of the open rule.
- Enter on a line with an example replaces the term values of the Assign pane (variables stay); it does not switch panes.
- The existing Ctrl+G compare with the last saved version is unchanged.
- Page updated: `docs/tui.md` (new section, layout table, key table) and the Tui `README.md` key list. Tests: `CompareControllerTests`, `RewritePaneTests`.
- Not run: the full validation set of CLAUDE.md. `DocumentationLintTests` fails on `tests/TruthWeaver.Architecture.Tests/README.md:10` ('ADR-0'), which is not from this change.
- 2026-10-18: Full validation passed on the combined result 2026-10-18 (6660 tests; csharpier, dotnet format, roslynator and normalize-text clean).
