# Repository layout proposal

Output of ticket 06. This is a proposal. No file has moved. The owner approves or declines each move (M1 to M8); each approved move becomes its own ticket.

## Current layout (counts from `git ls-files`)

```text
/
  .claude/            333 files  (skills 331, CLAUDE.md, settings.json; worktrees and local state are git-ignored)
  .github/            workflows (ci, mutation), dependabot.yml
  .husky/             task-runner.json, pre-commit
  .scratch/           570 files in about 40 feature folders (tickets, specs, logs)
  .vscode/  .mcp.json  .editorconfig  .gitattributes  .gitignore
  .csharpierrc.json  .prettierrc.json  .prettierignore
  stylecop.json  stryker-config.json  skills-lock.json  opencode.jsonc
  Directory.Build.props  Directory.Build.targets  Directory.Packages.props
  NuGet.config  global.json  dotnet-tools.json  TruthWeaver.slnx
  AGENTS.md  CLAUDE.md  CONTEXT.md  CHANGELOG.md  README.md  LICENSE
  benchmarks/  docs/ (185)  editors/vscode/  samples/  scripts/ (8)  src/ (440)  tests/ (377)
  .codex/  .cursor/   git-ignored, local tool state only
```

## Recommended layout

```text
/
  .claude/            skills stay (tool discovery); README + lock for skills live with them (M3)
  .github/  .husky/  .vscode/  .editorconfig  .gitattributes  .gitignore
  .csharpierrc.json  .prettierrc.json  .prettierignore      (stay: tools discover them here)
  Directory.*.props/targets  NuGet.config  global.json  dotnet-tools.json  TruthWeaver.slnx
  AGENTS.md  CLAUDE.md  CONTEXT.md  CHANGELOG.md  README.md  LICENSE
  eng/                stylecop.json, stryker-config.json, scripts/ (M1, M2, M5)
  docs/  src/  tests/
  extras/             benchmarks/, samples/, editors/  (M6, optional)
  (.scratch/ archived or pruned, see M7)
```

## Files that must stay at the root

| File | Why |
| --- | --- |
| `global.json`, `dotnet-tools.json`, `NuGet.config`, `Directory.*.props/targets`, `.editorconfig` | The SDK, `dotnet tool restore`, NuGet and MSBuild look upward from the project for these. |
| `.csharpierrc.json` | CSharpier searches upward from each file; the repo root is the safe place. Moving it needs an explicit `--config-path` everywhere (CI, Husky, scripts, editor integration). |
| `.prettierrc.json`, `.prettierignore` | Prettier finds both from the working directory; `scripts/format-data.ps1` relies on this. |
| `.gitattributes`, `.gitignore`, `.github/`, `.husky/` | Git, GitHub and Husky.Net discover them here. |
| `AGENTS.md`, `CLAUDE.md`, `CONTEXT.md`, `README.md`, `LICENSE`, `CHANGELOG.md` | Agents and GitHub discover them here; the `LICENSE` and `README.md` paths are also package metadata. |
| `.claude/` | Claude Code discovers project settings and skills here. |
| `opencode.jsonc` | The opencode tool reads it from the project root. It is not a candidate for a move. |

## Candidate moves and cost

| Id | Move | Target | What breaks | Risk |
| --- | --- | --- | --- | --- |
| M1 | `stylecop.json` | `eng/stylecop.json` | `Directory.Build.props` line 80 `AdditionalFiles` path; `TruthWeaver.slnx` solution-item entry (line 107). StyleCop finds the file by the `AdditionalFiles` item, not by location. | Low. One build proves it. |
| M2 | `stryker-config.json` | `eng/stryker-config.json` | `.github/workflows/mutation.yml` line 38 (`-f` path); the workflow comment on line 5; no page under `docs/` names the file; `slnx` solution item. | Low. Only the manual or scheduled mutation workflow reads it. |
| M3 | `skills-lock.json` | `.claude/skills-lock.json` | `SkillProvenanceChecker` (`tests/.../ReferenceDocs/SkillProvenanceChecker.cs`, path combine on line 33) and its tests; the Lock file section of `.claude/skills/README.md`; `slnx` entry. The vendoring tool may expect the lock at the root, so confirm that before moving. | Medium. The tool that writes the lock decides. Decline if it expects the root. |
| M4 | `opencode.jsonc` | none | The tool needs it at the root. | Decline. Listed so the audit item is closed. |
| M5 | `scripts/` | `eng/scripts/` | `.husky/task-runner.json` (4 task args), `.husky/pre-commit`, `.github/workflows/ci.yml` and `release.yml` (`scripts/...` calls), `scripts/format-data.ps1` self-references, `CLAUDE.md` (3 mentions), `docs/contributing.md` (7), `docs/releasing.md`, `CHANGELOG.md`, `README.md`, the `.github/dependabot.yml` comment. The docs-lint scope follows links, so a changed link target must still resolve. | Medium. Many call sites; CI and the hook fail loudly if one is missed. |
| M6 | `benchmarks/`, `samples/`, `editors/` | `extras/` (or keep) | `TruthWeaver.slnx` project paths, `samples/Directory.Build.props` relative imports, `docs/benchmarks.md` and `samples/README.md` links, CI and `dependabot.yml` directory entries, `Directory.Build.props` conditions that mention these folders, package `ProjectReference` relative paths. | Medium to high for little gain. Recommend decline: three top-level folders with clear names are not a problem. |
| M7 | `.scratch/` history | archive branch or prune | See the decision below. Files that link into `.scratch` (ADRs, `CLAUDE.md` "In-flight work", ticket cross-links, `scripts/list-open-done-tickets.ps1`, `normalize-text.ps1` allowlist). `DocumentationLint` already stops at `.scratch/`, so the docs-lint scope and the `DocumentationLintBaseline` are not affected. | Medium. History in git remains; only the working tree changes. |
| M8 | `.claude` content | separate repo or keep | See the decision below. `SkillProvenanceChecker` and its tests; `skills-lock.json`; Claude Code discovery. | High. Recommend keep. |

The `DocumentationLintBaseline` lists files, not folders under the candidates above, except where a listed doc mentions a
moved path. Check it after M1, M2 and M5 with `dotnet test tests/TruthWeaver.Tests --filter-class "*DocumentationLint*"`.

## Decisions

**`.scratch` (570 files, about 3 MB on disk).** Recommendation: prune the closed efforts from the working tree and keep
them in git history. Closed means every ticket is done and the effort has a summary line in an ADR, `CHANGELOG.md` or the
issues log. Keep the open efforts in place. Archive step: tag the last commit that contains the folders (for example
`archive/scratch-2026-10`) so the history stays easy to find, then delete the closed folders in one commit. Prune only
after the owner lists which folders are closed; the superseded material named in `CLAUDE.md` (`_superseded/`,
`engine-v1`) is the first candidate. A separate archive repository costs more than it saves: it breaks every relative
link and the ticket tooling.

**`.claude` (333 files, about 2 MB on disk).** Recommendation: keep it in this repository. 331 of the files are vendored
skills with a provenance table and a lock file that a test already checks (`SkillProvenanceChecker`). A separate repository
would need a submodule or a sync step, and a clone would lose the skills that the project rules depend on. If the size
becomes a problem, move only the vendored third-party skills out and keep `CLAUDE.md` and `settings.json`; do that as its
own ticket after M3 is settled.

**`.codex` and `.cursor`.** Already git-ignored and local. No action.

## Suggested order if approved

1. M1 and M2 (small, independent, one ticket each).
2. M5 (largest set of call sites; run `pwsh scripts/format-all.ps1 -CheckOnly`, the doc tests and a CI run).
3. M7 after the owner names the closed folders.
4. M3 only after the vendoring tool's lock location is confirmed.
5. M4, M6 and M8: decline unless the owner wants them.

## Owner decisions needed

| Move | Approve | Decline |
| --- | --- | --- |
| M1 | | |
| M2 | | |
| M3 | | |
| M4 | | |
| M5 | | |
| M6 | | |
| M7 | | |
| M8 | | |
