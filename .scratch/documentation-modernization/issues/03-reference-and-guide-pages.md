# 03: Reference and guide pages (phase 3)

**What to build:** Re-audit the Markdown set against the finished implementation: `README.md`, `CONTEXT.md`, `docs/*.md`, `docs/strong-k3/`, `samples/README.md` and `docs/doc-examples.md`. `CHANGELOG.md` entries describe each change itself and drop ADR and ticket labels. Replace each behind-the-scenes reference (ADR, ticket, PRD, .scratch path) with a plain description of the behavior, constraint or reason it pointed to. Read the cited record to get it right. Do not just delete the label and do not leave a "see ADR/ticket" pointer. Do not rename members.

**Blocked by:** 02

**Status:** needs-triage (do not start or dispatch until the owner triggers the effort)

- [ ] No behind-the-scenes reference remains in these files
- [ ] Prose passes `humanizer` and `voice-of-robert`; dialect passes `github-markdown`
- [ ] `DocumentationLint`, `DocExample` and `K3Reference` tests pass
- [ ] The cleaned files are off the guard baseline
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
