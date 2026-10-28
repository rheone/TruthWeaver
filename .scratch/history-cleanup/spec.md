# History cleanup: rewrite origin/main into a themed, attribution-clean history

**Status:** needs-triage (proposed, awaiting approval). Nothing in this plan has been executed.

Decided in the 2026-10-25 grilling session (scope, grouping, attribution, backup, force-push, verification, branch cleanup, commit style, theme source).

## Why

`origin/main` carries 587 commits across 25 merged PRs (2026-09-28 to 2026-10-25), plus roughly 86 direct-to-main commits from before the PR workflow started. Most PRs were merged with a real merge commit, so every intermediate "ticket" commit (including mid-review fixups and formatting-only commits) is still present on `main`. One PR (#7, "additional roadmap work and implementation") was squash-merged and bundles about nine genuinely distinct deliverables into a single commit with a 150-line bulleted body. The goal is a linear, thematically organized history that reads like the project's own `.scratch/` effort names, with no stray attribution noise.

## Facts gathered (verified against the repo, not assumed)

- Sole contributors: Robert H. Engelhardt (`rheone@gmail.com` / `1584973+rheone@users.noreply.github.com`) and `dependabot[bot]`.
- No git tags exist. No open PRs. All three local feature branches (`ultra-review/crash-and-blowup-fixes`, `ultra-review/verification-pass`, `plan/coverage-gaps-2026-10-25`) are already merged into `origin/main` (PRs #23-25) — nothing is at risk of being lost by deleting them.
- **No real Claude/Anthropic/LLM attribution exists anywhere in the 610-commit history.** Every "claude" hit in `git log --all` is a false positive: file paths (`.claude/skills/`) or mentions of `CLAUDE.md`.
- The one piece of attribution noise is a `Co-authored-by: x <x@x>` trailer on the PR #7 squash-merge commit (`3105131`). Its root cause is a genuine empty commit (`9c2baef`, subject `e`, zero file changes, authored as `x <x@x>`) that was present on the source branch and picked up as a co-author by GitHub's squash merge. It is not LLM-related — just a leftover from a broken git identity on one throwaway commit. It disappears automatically once that stretch of history is squashed; nothing special has to be done beyond not carrying the trailer forward.
- Two merge commits (`6251f69`, `785b1ce`) are empty "already in sync" merges with no net diff; they disappear automatically in a linear rewrite.
- PR #8's commits are a strict subset of PR #9's (branch ancestry), so they are one piece of work, not two.

## Settled decisions

| # | Decision | Answer |
|---|---|---|
| 1 | Scope | Rewrite all of `origin/main`'s history (587 commits / 25 PRs), not a slice and not just the feature branches. |
| 2 | Grouping | Regroup by logical theme/feature **across** PR boundaries, not by PR. |
| 3 | Attribution | Strip the one `Co-authored-by: x <x@x>` trailer. No other attribution edits needed — none found. |
| 4 | Backup | A `git bundle` of all refs saved outside the repo directory, **and** a `backup/pre-history-cleanup-2026-10-25` branch pushed to `origin` before any rewrite. |
| 5 | Force-push | Authorized, with one gate: show the final rewritten commit log for review before the actual `push --force-with-lease` to `origin/main`. |
| 6 | Verification cadence | Run the full required validation suite (`restore --locked-mode`, `build`, `test`, `csharpier check`, `format --verify-no-changes --severity info`, `roslynator analyze`) after **every** regrouped commit is built, not just at the end. |
| 7 | Branch cleanup | Delete the three already-merged branches, both locally and on `origin`, once the rewrite is confirmed safe. |
| 8 | Commit message style | Plain descriptive subject lines on the new commits, no `feat:`/`fix:`/`docs:` prefixes, written in the `voice-of-robert` technical register: short active-voice sentences naming the actor, present tense, no contractions, no AI-tell filler (no "this does not mean X but Y", no inflated language, no chat leftovers). This matches how the project's own existing commit messages already read (e.g. "Point BETWEEN's min>max case at min, not max"), so it is a continuation of the existing style, not a new one. |
| 9 | Theme source | Claude proposes the breakdown (below) for review, rather than the user dictating categories up front. |
| 10 | `CHANGELOG.md` SHA anchors | `CHANGELOG.md` pins seven commit SHAs (`df8f4b9`, `57cf2c9`, `4cfc1b9`, `4176ea2`, `dc02e7b`, `578cce8`, `f3272f1`) and one GitHub commit permalink as migration reference points; all go dead under a rewrite. De-anchor: rewrite those passages to reference themes/features by name and date instead of raw hashes, rather than re-pointing the hashes or leaving them stale. |
| 11 | `ultra-review-2026-10-21/summary.md` dead links | Deleting completed tickets 07 and 08 there leaves two dead links in `summary.md`'s findings table. Strip the links and keep the row text, rather than exempting the file and keeping the tickets. |

## Defaults disclosed, not separately grilled (speak up to change)

- **Author identity** is left as whatever each original commit already carries (`rheone@gmail.com` or the GitHub noreply address) rather than being forced onto one identity.
- **Commit dates**: each themed commit keeps the author date of its earliest constituent commit; the committer date is whenever the rewrite runs (ordinary rebase behavior).
- `.scratch/` files that cite old SHAs (e.g. `9c2baef`, the empty `e` commit, in `dispatch-notes-2026-10-11.md` and others) are left alone — `.scratch/` is explicitly frozen history per CLAUDE.md, same treatment as an ADR, not live documentation that needs to track the rewritten hashes.

## Proposed theme sequence

Chronological order preserved between themes; same-theme commits that were originally scattered across different PRs get pulled together. ~34 themes collapsing 587 commits into an estimated **38-42** final commits.

| # | Theme | Original span | Est. commits |
|---|---|---|---|
| 1 | Initial scaffold + BooleanRulesEngine v1 | `bf2ebdc`-`df8f4b9` | 1 |
| 2 | Early README/CI fixes | `027ea92`-`062e953` | 1 |
| 3 | Engine v1 feature hardening (threshold family, XNOR, RuleBuilder, Guid literals, node-shape seam, JSON schema, diff rendering, round-trip tests, metrics, benchmarks, AOT) | pre-PR1 | 1 |
| 4 | TruthWeaver rebrand (tickets 01-07) | pre-PR1 | 1 |
| 5 | Test-coverage-gaps tickets 01-32 + externally-resolved-value predicate pattern | pre-PR1 | 1-2 |
| 6 | Docs: current-state README/CONTEXT rewrite | PR1-3 | 1 |
| 7 | Strong K3 language surface (ADR-0005): operators, rewrites, diagnostics | PR4 | 1-2 |
| 8 | Predicate catalog expansion + `NotX` twin convention | PR5 | 1 |
| 9 | Library roadmap specs & tickets (planning docs) | PR6/7 | 1 |
| 10 | Testing tools (`PredicateHarness`, `RuleFuzzer`, `RuleAssertions`) | PR7 | 1 |
| 11 | Samples (Consumer / PredicateLibrary / DataSource) | PR7 | 1 |
| 12 | Rule printers: Mermaid options + equation dialects (Unicode/LaTeX/AsciiMath/MathML/Typst/C-style) + HTML/Graphviz/width-aware text | PR7 | 1-2 |
| 13 | `TruthWeaver.Generators` source generator | PR7 | 1 |
| 14 | Rewrite/lint/threshold-semantics core refactor + repo-hygiene enforcement | PR7 | 1 |
| 15 | API consistency pass (Try-first, naming guide, regex/`HasCount` predicates, `OptimizeOrder`) | PR7 | 1 |
| 16 | Tool.Core foundation: node notes, cross-format conversion, truth-table printer, validation/export | PR7 | 1-2 |
| 17 | Decision records, rule coverage, batch eval, rule diff/implication/counterfactual, `StubEvaluator`, `truthweaver` CLI, declarative rule tests | PR7 | 1-2 |
| 18 | Main-review audit: text/JSON normalization tooling | PR8/9 | 1 |
| 19 | Policies/Extensions.Configuration packages + `ActivitySource` tracing + Unknown-audit fixes | PR9 | 1 |
| 20 | Terminal UI workbench: initial build | PR9/12 | 1 |
| 21 | Dependency bumps (dependabot) | PR10, 11, 22 | 3, kept separate |
| 22 | Project READMEs (27 projects) | PR12 | 1 |
| 23 | Rule-set analysis & shadowing detection | PR12 | 1 |
| 24 | Tool.Core/Tool.Tui/Testing folder-namespace reorg + Tool.Tui release | PR12 | 1 |
| 25 | Release pipeline + badge fixes | PR13-15 | 1 |
| 26 | TUI feature completion + review-cycle fixes | PR16 | 2 |
| 27 | Architecture review 2026-10-19 | PR17 | 1 |
| 28 | Ultra-review 2026-10-21 (quantifier oracle, 67-ticket review, skill provenance) | PR18 | 1 |
| 29 | Devcontainer fixes | PR19 | 1 |
| 30 | CI build break (StyleCop) + quantifier-rewrite fixes | PR20 | 1 |
| 31 | Editors/VS Code scoping + rule-text language server plan | PR21 | 1 |
| 32 | Ultra-review verification pass | PR23 | 1 |
| 33 | Ultra-review crash-and-blowup fixes | PR24 | 1 |
| 34 | Coverage-gaps audit planning | PR25 | 1 |

Dependabot's three bumps (theme 21) stay as their own commits under `dependabot[bot]`'s authorship rather than folding into adjacent feature themes — they're mechanical and independently attributable.

**Highest-risk area:** themes 9-17 all come from PR #7's single squashed commit, which has no individual commit boundaries to lean on — only its ~150-line bulleted body. Splitting it into 9 themes is hand judgment, not a mechanical `git log` slice, and is the part most worth a careful look before execution.

## Execution plan (not started)

1. **Backup.** Create a `git bundle --all` saved outside the repo directory, and push `backup/pre-history-cleanup-2026-10-25` to `origin` pointing at current `origin/main`.
2. **Build the new history on a scratch branch** (never touch `main` directly until the final swap), replaying commits into the ~34 themes above in chronological order, writing each as a plain descriptive subject with a body summarizing what it covers, and dropping the `Co-authored-by: x <x@x>` trailer.
3. **Validate after every regrouped commit**: `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info`, `dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics`. A failure is fixed in the commit being built, not patched in a later one, so the themed history stays bisectable.
4. **Diff the final tree against current `origin/main`'s tree** to prove no content was lost or altered beyond the one trailer removal (the working tree at the end of the rewrite must match the working tree at `origin/main` today, file for file).
5. **De-anchor `CHANGELOG.md`** from the seven raw commit SHAs and the one permalink it currently cites as migration reference points, rewording those passages to name the theme/feature and date instead (e.g. "the pre-rebrand `BooleanRulesEngine` state" rather than "commit `df8f4b9`"). Re-run the full validation suite afterward since this is a tracked, tested doc.
6. **Show you the full rewritten `git log`** (subjects + stats) for review.
7. **On your go-ahead**, point `main` at the new history and `git push --force-with-lease` to `origin`.
8. **Delete the three stale branches**, locally and on `origin`.

## Rollback

If anything is found wrong after the push, `backup/pre-history-cleanup-2026-10-25` on `origin` and the local bundle both let `main` be reset and force-pushed back to the pre-cleanup state at any time.

## Risks and caveats to go in eyes-open

- This changes every commit hash on `main`. Any other local clone of this repo will need to be re-fetched and hard-reset (or re-cloned) after the push — it cannot be merged or pulled normally.
- GitHub retains merged-PR commit data for a time via its own internal refs even after the branch tip moves, so PR #1-#25 pages should keep rendering their diffs for now, but that is not a permanent guarantee once the old commits become fully unreferenced.
- `git push --force-with-lease` (not bare `--force`) will be used for the final push, so it safely aborts if `origin/main` has moved since the plan was reviewed.

## Additional cleanup folded into this effort

- **Duplicate `.scratch` snapshot.** `.scratch/TruthWeaver .scratch - 2026-10-22T171913Z/` is a complete, accidental duplicate of the `.scratch/` tree itself (62 of its 65 entries share names with the real top-level `.scratch/` folders), 3.8MB across 768 tracked files, added by commit `0c73f17` on 2026-10-22. Nothing else in the repo references it. **Settled: excluded when theme 28 ("Ultra-review 2026-10-21") is built during the rewrite**, rather than added and then deleted, so the rewritten history is smaller for real instead of just hiding the blobs under a later commit.
- **Completed scratch tickets.** Surveyed: of 138 ticket files under `.scratch/*/issues/*.md`, only **3 are terminal** (`repo-hygiene/issues/01-restore-skill-provenance-table.md`: done; `ultra-review-2026-10-21/issues/07-nand-nor-expander-throws-on-quantifiers.md` and `08-render-decision-with-quantifier-throws.md`: resolved). The other 135 are genuinely open (`needs-triage`, `on-hold`, `ready-for-agent`, `needs-owner-action`, `deferred`, `needs-human-review`, `ready`, `superseded`) and stay. This is a tidiness move, not a space reclaim (~20KB total). All 56 `spec.md`/`map.md` files are untouched. `ultra-review-2026-10-21/summary.md` links tickets 07 and 08 by path in a findings table; per decision 11, those two links are stripped (row text kept) rather than exempting the file. This lands as its own small commit near the end of the rewritten sequence (after the 34 themes, before the branch cleanup step).

## Value-added suggestions (not decided — accept, decline, or defer each independently)

1. **Block future attribution lines.** Add a cheap commit-msg hook or CI grep that fails a commit containing `Co-Authored-By:` for a non-human identity or "Generated with Claude/Anthropic" style text, so this class of noise can't quietly return.
2. **Tag the cleanup point.** Since there are currently zero tags, consider tagging the rewritten `main` HEAD (e.g. `history-cleanup-2026-10-25`) as a permanent landmark, so anyone bisecting later knows exactly where the rewrite happened.
3. **Default to squash-merge on GitHub.** Several PRs (especially #7) show that letting a branch accumulate many small WIP/fixup commits and then merging with a real merge commit is what produced the sprawl in the first place. Setting the repo's default/only merge strategy to "squash" would prevent a recurrence without needing another cleanup like this one.
4. **Smaller, single-purpose PRs.** PR #7 alone produced 9 of the 34 themes above. The `.scratch/` ticket system already tracks work at a finer grain than PRs do — closing a PR per ticket (or small ticket group) instead of batching a whole roadmap into one PR would keep future history naturally clean.
5. **A short landmark note.** A one-paragraph note (CONTRIBUTING, or a dated entry somewhere low-ceremony) recording that history was rewritten on 2026-10-25 and where the backup lives, so the discontinuity doesn't puzzle a future reader of `git log` or `git blame`.
