# 02: Update MinVer to 8.0.0

**Priority:** highest

**What to build:** Do not suppress analyzers to pass a build and do not weaken a severity without a documented reason. Fix the cause. Nothing is pushed, tagged or released.

Update `MinVer` 7.0.0 to 8.0.0 (a major version). Read the MinVer 8 release notes for breaking changes (property names, defaults, minimum SDK, tag handling) and adapt `Directory.Build.props`, `docs/contributing.md` and `docs/releasing.md` accordingly. Verify as the earlier MinVer ticket did: an untagged build packs as the expected pre-release version, a throwaway clone with a `v2.3.0` tag gives `2.3.0` on the tag and the expected next version one commit later (the clone is deleted and no tag is created in the repository), and test and sample projects are still not versioned by MinVer. Re-run the CI pack check.

**Blocked by:** 01

**Status:** done

- [x] MinVer is 8.0.0 and the version scheme (tag prefix `v`, pre-release phase `preview.0`, minimum major.minor 1.0) behaves as before, or the documented change is applied
- [x] The tagged and untagged checks are repeated and recorded in the ticket
- [x] `scripts/check-packages.ps1` and `dotnet pack -c Release -warnaserror` into a scratch directory pass
- [x] Docs describe the final state, with no "changed from" text
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator)

## Comments

### 2026-10-11, dispatch run

MinVer 8.0.0 is in `Directory.Packages.props` (commit ab91a66). The only breaking change in MinVer 8 is that
`MinVerDefaultPreReleasePhase` fails when it is set. This repository sets `MinVerDefaultPreReleaseIdentifiers`
(`preview.0`), `MinVerTagPrefix` (`v`) and `MinVerMinimumMajorMinor` (`1.0`), so `Directory.Build.props`,
`docs/contributing.md` and `docs/releasing.md` need no change; they already describe the current behavior.

Checks (MinVer 8.0.0):

| Check | Result |
| --- | --- |
| Untagged repository, `dotnet msbuild src/TruthWeaver -t:MinVer -getProperty:PackageVersion` | `1.0.0-preview.0.268` |
| Throwaway clone (scratchpad, `git clone --no-hardlinks`), tag `v2.3.0` on HEAD | `2.3.0` |
| Same clone, one commit after the tag | `2.3.1-preview.0.1` |
| `dotnet msbuild tests/TruthWeaver.Tests -t:MinVer` | `MSB4057`: no MinVer target (tests, samples and benchmarks are `IsPackable=false` and get no MinVer reference) |
| `dotnet pack -c Release -warnaserror` into a scratch directory | 7 packages and 6 symbol packages at `1.0.0-preview.0.268` |
| `scripts/check-packages.ps1` | all packages pass |
| Validation set (restore `--locked-mode`, build `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator) | pass (4896 tests, 0 Roslynator diagnostics) |

No tag was created in the repository. The throwaway clone was left in the session scratchpad, outside the repository.
No tests were added or changed.
