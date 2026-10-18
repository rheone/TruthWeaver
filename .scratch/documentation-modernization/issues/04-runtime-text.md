# 04: Runtime text (phase 4)

**What to build:** Review the text users see at run time: CLI help strings, `TRE` diagnostic messages and their fixes, exception messages, and JSON schema descriptions. Each message says what is wrong, where, and how to fix it, in the project terms. Tests that pin message text change with it. Replace each behind-the-scenes reference (ADR, ticket, PRD, .scratch path) with a plain description of the behavior, constraint or reason it pointed to. Read the cited record to get it right. Do not just delete the label and do not leave a "see ADR/ticket" pointer. Do not rename members.

**Blocked by:** 03

**Status:** on-hold (do not start or dispatch until the owner triggers the effort)

- [ ] Every message and description is reviewed against the documentation standard
- [ ] `docs/strong-k3/specification/diagnostics.md` and `docs/cli.md` match the final wording
- [ ] Prose passes `humanizer` and `voice-of-robert`
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
