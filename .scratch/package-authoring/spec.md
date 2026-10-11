# Package authoring and build configuration

**Status:** grilled 2026-10-10, ready for implementation

## Request

- Use `GlobalPackageReference` for general-purpose analyzer packages (<https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#global-package-references>).
- Configure implicit usings per project kind: xUnit for xUnit projects, common TruthWeaver namespaces for other TruthWeaver projects.
- Configure publishing metadata (author, version, expected project metadata).
- Apply package authoring best practices and prepare for an eventual release. Do not release.
- Configure SourceLink.

## Decisions (grilled 2026-10-10)

| Question | Decision |
| --- | --- |
| Version scheme | MinVer from Git tags, tag prefix `v`. A release is a tag. |
| Scope of the global analyzer references | Every project, as the current `PackageReference` items do. Check the netstandard2.0 generator project. |

## State of the repo (checked 2026-10-10)

- Analyzers are `PackageReference` items in the root `Directory.Build.props`; versions are in `Directory.Packages.props`. `Microsoft.SourceLink.GitHub` is referenced the same way.
- Already set in the root props: `Authors`, `Copyright`, license expression (Apache-2.0), repository and project URL, `PublishRepositoryUrl`, `EmbedUntrackedSources`, `PackageTags`, `PackageReadmeFile`, snupkg symbols, `ContinuousIntegrationBuild` in CI, `Version` 1.0.0-dev. No `PackageIcon` by design.
- `Directory.Build.targets` adds `Using Xunit` for `*.Tests`. No other project kind has a `Using` set.
- Not set: `EnablePackageValidation`, per-package `PackageId`/`Description`/tags review, a version scheme, a pack check in CI.

## Extras added (not requested)

- `EnablePackageValidation` and a no-push CI pack check (ticket 04).
- A `docs/releasing.md` runbook that records the steps to release, marked as not run (ticket 06).

## Tickets

| Ticket | Change | Blocked by | Status |
| --- | --- | --- | --- |
| [01](issues/01-analyzers-as-global-package-references.md) | Analyzer packages become `GlobalPackageReference` | None | ready-for-agent |
| [02](issues/02-implicit-usings-per-project-kind.md) | Implicit usings per project kind | 01 | ready-for-agent |
| [03](issues/03-package-metadata-per-project.md) | Complete package metadata for each packable project | None | ready-for-agent |
| [04](issues/04-pack-validation-and-ci-pack-check.md) | Package validation and a no-push CI pack check | 03 | ready-for-agent |
| [05](issues/05-minver-version-scheme.md) | MinVer version scheme | None | ready-for-agent |
| [06](issues/06-sourcelink-verification-and-release-runbook.md) | Verify SourceLink and write the release runbook | 03, 05 | ready-for-agent |
