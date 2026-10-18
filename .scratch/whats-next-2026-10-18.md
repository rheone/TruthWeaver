# What's next (pickup for the next run)

Written 2026-10-18 at the end of the dispatch run. Branch `docs/project-readmes-tickets`. Everything below the "Done" list is not done.

## Done in this run

- feature-backlog 21c (rule-set analysis: redundancy, removable set, pairwise sweep; BDD node budget). Shadowing is built (616d9ef).
- project-readmes 01 to 08 (27 READMEs and the drift test `ProjectReadmeTests`).
- tui-workbench 09 to 20 (Terminal.Gui.Editor, predicate catalog in the palette, Make True pane, rewrite accept with note carry-over, Compare pane, mouse, definitions in the tree pane, color-independent marks, light and dark theme, persisted theme and layout, debounced diagnostics, saved-case coverage).
- Generators.Tests loads the generator analyzer from a copy under `obj/`, to avoid the editor lock on the dll.
- Task queue after the CI fix: 21c shadowing, and the folder = namespace reorganization of Tool.Core, Tool.Tui and Testing (IDE0130 enforced by `dotnet format`). Samples refresh is ticketed in `.scratch/samples-refresh/`.

## 1. Close-out of this run (do first)

- Done 2026-10-18: the full validation from `CLAUDE.md` passed on the combined result (6660 tests passed; csharpier, dotnet format, roslynator and normalize-text clean). The CI failure (S8970 in Generators.Tests) was fixed in commit `5539512`.
- Done 2026-10-18: tickets tui 12 and 13 and project-readmes 08 are ticked and done; feature-backlog 21c has the owner-scope box ticked and a new open box for the shadowing implementation; feature-backlog 08 and 09 are set to done.
- Not tried on a real terminal: NO_COLOR scheme swap, OSC 11 background detection, mouse support, the Terminal.Gui.Editor swap, `workbench.json` persistence.
- Check whether the Generators.Tests fix removes the editor lock: do a plain Debug `dotnet build` with the editor open.

## 2. Owner decisions waiting

- **21c shadowing.** Done (commit 616d9ef): a rule is shadowed when no assignment makes it True while every earlier rule is False, so an earlier True or Unknown rule always blocks it (ADR-0015).
- **Ticket 03.** The Tool README lists ten commands (`import` is the tenth); the ticket said nine.
- **Tool.Tui README.** The install step follows `docs/tui.md` (pack from the repo), not the ticket's plain `dotnet tool install`.
- **Follow-up F.** `.scratch/followups-2026-10-11/issues/F-owner-review-of-dispatch-notes.md` holds the open API and wording reviews from earlier dispatch notes.

## 3. Still on hold or deferred (do not start without the owner)

documentation-modernization, public-api-documentation, the 11b rule-library tickets, 16b registry assembly loading, 17b language server, 19 web playground, 23 LINQ and SQL translation, package-updates 04, rule-linting 04, k3-followups 26, repo-improvements 09 (needs owner action).

## 4. Dispatch hazards learned

- The Agent tool's `isolation: "worktree"` cut worktrees from `main`, not the current branch, so new tickets were missing. Create worktrees by hand from HEAD (`git worktree add .claude/worktrees/<name> -b dispatch/<name> HEAD`) and point the agent at the path, after committing so the branch carries the work.
- Merge worker output with a 3-way merge (`git merge-file`) when several workers touch `docs/tui.md`, `StructureController.cs` or the palette files.
- Workers must not run `dotnet format` or `scripts/format-all.cs` in apply mode over the repo.
- Python on this machine writes CRLF: run `sed -i 's/\r$//'` after editing with it.
- `DocumentationLint` scans `.claude/worktrees`; remove finished worktrees before running the tests.
