# Repository improvements, 2026-10-17

**Status:** grilled 2026-10-17. Tickets 01 to 08 and 10 to 16 are done. Ticket 09 (owner checklist) is open: the first release has twelve packages including `TruthWeaver.Tool.Tui`, and the `PackageIcon` decision is deferred.

Source: a nine-part owner request (repo layout, missed improvements, package updates, `Unknown` consistency, implicit usings, global packages, CPM, XML docs, NuGet release). An audit came first; this folder holds only the gaps.

## Audit: already done, no ticket

| Request | State |
| --- | --- |
| Global packages | Analyzers and MinVer are `GlobalPackageReference` items in `Directory.Packages.props`. Source Link comes from the SDK. |
| CPM | `ManagePackageVersionsCentrally` is on. Restore runs in `--locked-mode`. Pins carry reason comments. |
| Implicit usings | `ImplicitUsings` is on, with shared global `Using` items at three levels. No per-file `using System...` lines remain. |
| Package versions | Only `Microsoft.NET.Test.Sdk` (18.10.1 to 18.11.0) and `Microsoft.VisualStudio.Threading.Analyzers` (18.7.23 to 18.12.1) are behind. |
| Release basics | `docs/releasing.md` (a runbook, never run), `scripts/check-packages.ps1`, `EnablePackageValidation`, a CI `pack` job. |
| XML docs and comments | Planned in `.scratch/documentation-modernization` (on hold). Amended there with decisions 11 and 12. |

## Decisions (owner, 2026-10-17)

| Item | Decision |
| --- | --- |
| Overlap with existing work | Audit first; ticket only the gaps; link existing tickets. |
| Release automation | A tag-triggered GitHub Actions workflow, a protected `nuget` environment with manual approval, NuGet.org trusted publishing (OIDC), no long-lived key. |
| Publish set | All nine packages, including `TruthWeaver.Tool`. |
| Extra analyzers | None. The current set is enough. |
| Update automation | Dependabot, grouped, weekly. |
| CPM hardening | The full set: NuGetAudit, transitive pinning, version-override lockdown, source mapping, plus the two bumps. |
| `Unknown` consistency | A read-only audit that produces a matrix. Each deviation becomes its own ticket after an owner decision. |
| Repo layout | A proposal only. No moves. |
| Documentation | Adopt `documentation-modernization` with two amendments. The hold stays until the owner lifts it. |

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [01](issues/01-bump-behind-packages-and-fix-missing-file.md) | Bump the two behind packages; resolve the missing `dictionary.dic` | None |
| [02](issues/02-cpm-and-nuget-hardening.md) | NuGetAudit, transitive pinning, override lockdown, source mapping | 01 |
| [03](issues/03-dependabot.md) | Dependabot config | 02 |
| [04](issues/04-global-using-sweep.md) | Check for remaining repeated usings | None |
| [05](issues/05-unknown-consistency-audit.md) | Read-only `Unknown` audit | None |
| [06](issues/06-repo-layout-proposal.md) | Repo layout proposal | None |
| [07](issues/07-release-workflow.md) | Tag-triggered release workflow | 02, 08 |
| [08](issues/08-publish-the-tool-package.md) | Publish `TruthWeaver.Tool` with the others | None |
| [09](issues/09-first-release-owner-checklist.md) | Owner checklist for the first release | 07, 08 |

01 and 02 edit the same props files; run them in order. 07 and 08 both touch `docs/releasing.md` and `scripts/check-packages.ps1`; run 08 first.

## Out of scope

- Extra analyzer packages and `AnalysisMode` changes.
- Moving any file (ticket 06 only proposes).
- Changing `Unknown` behavior (ticket 05 only reports).
