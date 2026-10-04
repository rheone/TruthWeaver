# 09: Contract and remove history files

**What to build:** The migration ends and the folder holds no history. The checker drops the old section name and the Title Case headings. The untracked `TODO.md` is deleted. (`VALIDATION.md` was already deleted in ticket 02.) No `docs/strong-k3/` file remains on the lint baseline. The full validation list from `CLAUDE.md` passes.

**Blocked by:** 06, 07, 08

**Status:** ready-for-agent

- [ ] The checker accepts only the new section names and sentence-case headings, and a fixture proves the old ones fail
- [ ] `TODO.md` is gone, and no file links to it
- [ ] The lint baseline holds no `docs/strong-k3/` entries
- [ ] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info` and `dotnet roslynator analyze` pass
- [ ] Line endings in every touched file are CRLF
