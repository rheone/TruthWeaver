# 15: A result summarizes its own failure and warnings

**Status:** done
**Type:** task
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 6

**What to build:** The tool result type (or the Workbench status interface) gains one failure summary and one warning summary, so the "code: message" wording for the first error exists once.

- [ ] Both summaries are tested once, including the no-error case
- [ ] No call site is migrated in this ticket
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
