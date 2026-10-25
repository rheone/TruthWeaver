# 02: Public API docs and package metadata (phase 2)

**What to build:** Carry out `../../public-api-documentation/` (its tickets 02 to 09), including NuGet description, tags and readme for each package. Each of its tickets 03 to 08 also removes behind-the-scenes references from the package's XML docs and comments. Replace each behind-the-scenes reference (ADR, ticket, PRD, .scratch path) with a plain description of the behavior, constraint or reason it pointed to. Read the cited record to get it right. Do not just delete the label and do not leave a "see ADR/ticket" pointer. Do not rename members.

**Blocked by:** 01

**Status:** needs-triage (do not start or dispatch until the owner triggers the effort)

- [ ] Tickets 02 to 09 of `public-api-documentation` are done
- [ ] Every public member has an XML doc, and a missing one fails the build (CS1591 and the doc analyzers as errors, no suppressions)
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
