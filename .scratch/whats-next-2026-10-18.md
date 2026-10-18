# What's next (pickup for the next run)

Written 2026-10-18 at the end of the dispatch run. Branch `docs/project-readmes-tickets`. Everything below the "Done" list is not done.

## Done in this run

- feature-backlog 21c (rule-set analysis: redundancy, removable set, pairwise sweep; BDD node budget). Shadowing is not built.
- project-readmes 01 to 08 (27 READMEs and the drift test `ProjectReadmeTests`).
- tui-workbench 09 to 20 (Terminal.Gui.Editor, predicate catalog in the palette, Make True pane, rewrite accept with note carry-over, Compare pane, mouse, definitions in the tree pane, color-independent marks, light and dark theme, persisted theme and layout, debounced diagnostics, saved-case coverage).
- Generators.Tests loads the generator analyzer from a copy under `obj/`, to avoid the editor lock on the dll.

## 1. Close-out of this run (do first)

- The full validation from `CLAUDE.md` has **not** been run on the combined result. Run it: `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info`, `dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics`. Per-project builds and tests passed (TruthWeaver.Tests 4411, Yaml.Tests 77, Tool.Core.Tests 505, Tool.Tui.Tests 272, Architecture.Tests 63).
- Tickets still showing open boxes: tui 12 and 13 ("in progress, only the full validation run is open"); project-readmes 08 (format-all box open); feature-backlog 21c (the owner-scope box). Tick them after validation.
- feature-backlog 08 and 09 still say "ready" but were delivered (commits `36eec79`, `d06628c`). Set them to done.
- Not tried on a real terminal: NO_COLOR scheme swap, OSC 11 background detection, mouse support, the Terminal.Gui.Editor swap, `workbench.json` persistence.
- Check whether the Generators.Tests fix removes the editor lock: do a plain Debug `dotnet build` with the editor open.

## 2. Owner decisions waiting

- **Formatter gate.** `dotnet format --verify-no-changes --severity info` fails on pre-existing CA1822 (test methods that could be static) and IDE0060 findings in about 68 test files. Options: fix them in one dedicated change, or relax the rules with a documented reason. Do not let a worker run `format-all.cs` in apply mode.
- **21c shadowing.** Ticket 07 is done; ADR-0015 says the first-applicable algorithm never skips an Unknown policy. Choose: shadowing reads "blocked by True or Unknown"; the reading is a parameter; or shadowing stays out.
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
