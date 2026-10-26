# 09: First release, owner checklist

**What to build:** These steps need the owner's accounts and cannot be done by an agent. Write them as a checklist that `/wizard` can turn into a script.

1. Reserve the twelve package ids on nuget.org (or confirm they are free): the ten library packages (including `TruthWeaver.Policies` and `TruthWeaver.Extensions.Configuration`) and the two tools, `TruthWeaver.Tool` and `TruthWeaver.Tool.Tui` (owner 2026-10-17: the Tui ships in the first release).
2. Add a trusted publishing policy on nuget.org for the repository, the `release.yml` workflow and the `nuget` environment.
3. Create the `nuget` GitHub environment with required reviewers and a tag restriction.
4. Protect `v*` tags.
5. Confirm the GitHub repository is public and the URLs in the package metadata resolve.
6. Decide on a `PackageIcon` (the metadata comment says none exists).
7. Update `CHANGELOG.md`. Tag `v1.0.0-rc.1` first as a rehearsal, then `v1.0.0`.
8. After the first stable release, set `PackageValidationBaselineVersion` (step 7 of `docs/releasing.md`).
9. Record the first run honestly in `docs/releasing.md`.

**Blocked by:** 07, 08

**Status:** needs-triage

- [ ] Each step above is done by the owner and ticked here
- [ ] The rehearsal tag `v1.0.0-rc.1` published all twelve packages
- [ ] `docs/releasing.md` records what ran
