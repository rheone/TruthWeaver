# 24: Empty the lint baseline and run the full validation list

**What to build:** Every in-scope file meets the standard and `DocumentationLintBaseline` is empty. The full validation list from `CLAUDE.md` passes.

**Blocked by:** 14, 15, 22, 23

**Status:** ready-for-agent

- [ ] `DocumentationLintBaseline.Files` is empty, and a comment states that the list may only shrink
- [ ] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info` and `dotnet roslynator analyze` pass with `CI=true`
- [ ] Line endings in every touched file are CRLF

See the [plan](../readme-breakdown-plan.md).
