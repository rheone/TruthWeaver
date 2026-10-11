# 02: Update MinVer to 8.0.0

**Priority:** highest

**What to build:** Do not suppress analyzers to pass a build and do not weaken a severity without a documented reason. Fix the cause. Nothing is pushed, tagged or released.

Update `MinVer` 7.0.0 to 8.0.0 (a major version). Read the MinVer 8 release notes for breaking changes (property names, defaults, minimum SDK, tag handling) and adapt `Directory.Build.props`, `docs/contributing.md` and `docs/releasing.md` accordingly. Verify as the earlier MinVer ticket did: an untagged build packs as the expected pre-release version, a throwaway clone with a `v2.3.0` tag gives `2.3.0` on the tag and the expected next version one commit later (the clone is deleted and no tag is created in the repository), and test and sample projects are still not versioned by MinVer. Re-run the CI pack check.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] MinVer is 8.0.0 and the version scheme (tag prefix `v`, pre-release phase `preview.0`, minimum major.minor 1.0) behaves as before, or the documented change is applied
- [ ] The tagged and untagged checks are repeated and recorded in the ticket
- [ ] `scripts/check-packages.ps1` and `dotnet pack -c Release -warnaserror` into a scratch directory pass
- [ ] Docs describe the final state, with no "changed from" text
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator)
