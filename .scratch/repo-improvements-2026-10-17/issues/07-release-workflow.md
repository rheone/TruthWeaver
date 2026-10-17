# 07: Tag-triggered release workflow

**What to build:** `.github/workflows/release.yml`. Trigger: push of a tag `v*`. Jobs:

1. `verify`: checkout with full history, setup .NET from `global.json`, restore in locked mode, build, test, run the same gates as `ci.yml`, and check that the tag is on a commit on `main` and that MinVer's version equals the tag without `v`.
2. `pack`: `dotnet pack` in Release with `-warnaserror`, `scripts/check-packages.ps1`, `dotnet-validate` for SourceLink and determinism, upload the packages as an artifact, and generate build provenance with `actions/attest-build-provenance`.
3. `publish`: needs `verify` and `pack`. Runs in the protected `nuget` environment (manual approval). Gets a short-lived key from NuGet.org trusted publishing (`NuGet/login` with OIDC; `id-token: write` on this job only). Pushes every `.nupkg` and `.snupkg` with `--skip-duplicate`.
4. `release`: creates a GitHub Release with generated notes and the package files.

Pin each third-party action to a full commit SHA. Permissions default to `contents: read`. A pre-release tag (`v1.2.0-rc.1`) publishes as a pre-release. Keep `ci.yml` unchanged. Update `docs/releasing.md` so the runbook describes the workflow and keeps a manual fallback. All nine packages publish (ticket 08).

**Blocked by:** 02, 08

**Status:** ready-for-agent (the workflow cannot publish until the owner finishes ticket 09)

- [ ] A dry run with publish disabled (`workflow_dispatch` input or a fork) passes `verify` and `pack`
- [ ] Every action is pinned to a SHA; no stored secret is used; only the publish job has `id-token: write`
- [ ] `docs/releasing.md` states the present procedure; steps are marked run or not run honestly
- [ ] The full validation set from `CLAUDE.md` passes
