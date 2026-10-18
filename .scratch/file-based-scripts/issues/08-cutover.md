# 08: Cut over to the file-based apps and delete the PowerShell scripts

**What to build:** Every caller runs the file-based apps: `.husky/task-runner.json`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `docs/contributing.md`, `docs/releasing.md`, `README.md`, `CLAUDE.md`, `.editorconfig` and `.prettierignore`. All eight `.ps1` files are deleted. The `.ps1` BOM exception in the text normalizer and `*.ps1` in the Husky include list are removed.

**Blocked by:** 02, 03, 04, 05, 06, 07

**Status:** needs-owner-action

- [x] No `pwsh` or `.ps1` reference remains outside `.scratch/`, `CHANGELOG.md` and `docs/adr/`
- [ ] A test commit through the Husky hook (Note 2026-10-17: hook command lines were run directly and exit 0; a real commit is left for the owner) formats, normalizes and builds as before
- [ ] The CI workflows pass on a branch (Note 2026-10-17: needs a push; check-packages.cs passed on real pack output in ticket 02) (check-packages runs after `dotnet pack`)
- [x] The full validation in `CLAUDE.md` passes, including the documentation tests
