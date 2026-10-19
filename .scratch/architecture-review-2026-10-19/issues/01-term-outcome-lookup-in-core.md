# 01: Core finds the outcome for one term

**Status:** ready-for-agent
**Type:** task
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 1

**What to build:** Core answers "what is the outcome for this term?" when pointed at it by tree path, source span or normalized call. The answer carries the value, the key and the faults attributed to that term as structured data. Faults are attributed when they are recorded, not by parsing text afterwards.

- [ ] Two same-name predicates with different arguments resolve to different terms
- [ ] A `from(...)` call with an escaped quote resolves to the right term
- [ ] A fault whose message starts with the display text of another term is attributed to the right term
- [ ] Plain Core tests cover these with no Workbench or terminal
- [ ] The term-identity concept in `CONTEXT.md` names the lookup consistently
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
