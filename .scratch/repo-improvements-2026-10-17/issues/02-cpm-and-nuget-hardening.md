# 02: CPM and NuGet hardening

**What to build:** Harden central package management and restore. (1) Enable NuGetAudit for direct and transitive packages at all severities (`NuGetAuditMode=all`, `NuGetAuditLevel=low`); in CI, vulnerability warnings (`NU1900` to `NU1904`) fail the build. (2) Set `CentralPackageTransitivePinningEnabled=true` and pin any transitive package that audit flags. (3) Set `CentralPackageVersionOverrideEnabled=false` so a project cannot override a central version; fix any project that does. (4) Add `packageSourceMapping` to `NuGet.config` (nuget.org only today, so one pattern `*`). (5) Keep locked-mode restore. Check that each global analyzer is still correct as a `GlobalPackageReference` with `PrivateAssets="all"` and that no shipped package leaks an analyzer dependency (`scripts/check-packages.ps1` already inspects this). Document any setting that cannot be turned on, with the reason.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Each setting is on, or its blocker is recorded in a comment next to where it would sit
- [ ] A deliberate vulnerable-package test (on a scratch branch) shows CI failing; the result is noted in the ticket
- [ ] `docs/contributing.md` describes the central package rules in the present tense
- [ ] The full validation set from `CLAUDE.md` passes
