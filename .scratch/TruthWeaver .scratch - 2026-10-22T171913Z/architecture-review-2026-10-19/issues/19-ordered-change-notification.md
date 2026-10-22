# 19: One ordered document-or-assignment change notification

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 03, 08
**Source:** architecture review of 2026-10-19, candidate 8

**What to build:** The window reacts to one "document or assignment changed" notification with a defined order, replacing the handler-order contract described in a code comment.

- [ ] The comment-based ordering note is deleted
- [ ] A test fails if the reset of the outcome runs after the window refresh
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
