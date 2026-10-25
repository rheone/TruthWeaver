# 09: Test summaries and comments in the other test projects (phase 5)

**What to build:** Test summaries and comments in the remaining test projects. Replace each behind-the-scenes reference (ADR, ticket, PRD, .scratch path) with a plain description of the behavior, constraint or reason it pointed to. Read the cited record to get it right. Do not just delete the label and do not leave a "see ADR/ticket" pointer. Do not rename members.

**Blocked by:** 04

**Status:** needs-triage (do not start or dispatch until the owner triggers the effort)

- [ ] No behind-the-scenes reference remains in: all other tests/ projects
- [ ] Comments carry value (reason, constraint, side effect) and do not narrate; skills: `gen-dotnet-docs-comments`, `csharp-docs-and-comments`, `humanizer`, `voice-of-robert`
- [ ] The cleaned files are off the guard baseline
- [ ] The full validation set from `CLAUDE.md` passes
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
