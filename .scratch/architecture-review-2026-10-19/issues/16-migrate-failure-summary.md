# 16: Controllers use the failure summary

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 15
**Source:** architecture review of 2026-10-19, candidate 6

**What to build:** Migrate the 12+ hand-rolled first-error formatters to the shared summary, in two batches by directory (controllers and commands first, then workbench, sessions and hosting). Keep the verb prefixes ("Not applied:", "Not saved:", "No image:").

- [ ] No `FirstOrDefault` on error severity remains for status wording
- [ ] Status messages are unchanged except for fixed wording drift, which the PR lists
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
