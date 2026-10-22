# 08: Cut over to the file-based apps and delete the PowerShell scripts

**What to build:** Every caller runs the file-based apps: `.husky/task-runner.json`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `docs/contributing.md`, `docs/releasing.md`, `README.md`, `CLAUDE.md`, `.editorconfig` and `.prettierignore`. All eight `.ps1` files are deleted. The `.ps1` BOM exception in the text normalizer and `*.ps1` in the Husky include list are removed.

**Blocked by:** 02, 03, 04, 05, 06, 07

**Status:** done (owner 2026-10-17: CI ran on the merge of PR #9; a real commit passed the hook)

- [x] No `pwsh` or `.ps1` reference remains outside `.scratch/`, `CHANGELOG.md` and `docs/adr/`
- [x] A test commit through the Husky hook formats, normalizes and builds as before (Note 2026-10-17: commit `cd49193` on `docs/project-readmes-tickets` passed the hook; it staged only Markdown files, so the C# formatting path ran earlier by direct command line)
- [x] The CI workflows pass on a branch (owner 2026-10-17: the CI run on the merge of PR #9 covers this; check-packages runs after `dotnet pack`)
- [x] The full validation in `CLAUDE.md` passes, including the documentation tests
