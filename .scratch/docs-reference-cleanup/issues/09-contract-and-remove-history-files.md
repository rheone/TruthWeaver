# 09: Contract and remove history files

**What to build:** The migration ends and the folder holds no history. The checker drops the old section name and the Title Case headings. `VALIDATION.md` and the untracked `TODO.md` are deleted. No `docs/strong-k3/` file remains on the lint baseline. The full validation list from `CLAUDE.md` passes.

**Blocked by:** 06, 07, 08

**Status:** ready-for-agent

- [ ] The checker accepts only the new section names and sentence-case headings, and a fixture proves the old ones fail
- [ ] `VALIDATION.md` and `TODO.md` are gone, and no file links to either
- [ ] The lint baseline holds no `docs/strong-k3/` entries
- [ ] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info` and `dotnet roslynator analyze` pass
- [ ] Line endings in every touched file are CRLF
