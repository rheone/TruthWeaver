# 09: Contract and remove history files

**What to build:** The migration ends and the folder holds no history. The checker drops the old section name and the Title Case headings. The untracked `TODO.md` is deleted. (`VALIDATION.md` was already deleted in ticket 02.) No `docs/strong-k3/` file remains on the lint baseline. The full validation list from `CLAUDE.md` passes.

**Blocked by:** 06, 07, 08

**Status:** done

- [x] The checker accepts only the new section names and sentence-case headings, and a fixture proves the old ones fail
- [x] `TODO.md` is gone, and no file links to it
- [x] The lint baseline holds no `docs/strong-k3/` entries
- [x] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info` and `dotnet roslynator analyze` pass
- [x] Line endings in every touched file are CRLF

## Comments

- 2026-10-04: Done. The checker requires the exact sentence-case section names and rejects "Implementation notes". `TODO.md` is deleted. The lint baseline holds no `docs/strong-k3/` entry. The full validation list from `CLAUDE.md` passed with `CI=true`: restore, build (0 warnings), 2531 tests, CSharpier, `dotnet format` and Roslynator (0 diagnostics).
