# 01: Inventory of every surface (phase 1)

**What to build:** One audit that lists, per surface, what is missing, stale, duplicated or non-compliant: public members without useful XML docs, internal comments that narrate or cite records, package metadata and package READMEs, CLI help and diagnostic and exception message wording, JSON schema descriptions, and pages. It extends the inventory in `public-api-documentation` ticket 01 and does not repeat it. Defects that are not documentation go to `findings.md`.

**Blocked by:** 00

**Status:** needs-triage (do not start or dispatch until the owner triggers the effort)

- [ ] The audit table covers every surface in spec decision 3, reproducible by a script in `scripts/` or stated commands
- [ ] Nothing outside `.scratch/` and `scripts/` changed
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
