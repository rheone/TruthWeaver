# 06: Verify SourceLink and write the release runbook

**What to build:** SourceLink is configured (`Microsoft.SourceLink.GitHub`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, snupkg, deterministic CI builds). Verify it end to end on a local pack with `ContinuousIntegrationBuild=true`, using the `sourcelink` tool or `dotnet-validate` on each package and symbol package. Write `docs/releasing.md`: the steps to release (changelog, tag, pack, push, symbols), each marked as not run, and what to check afterwards.

**Blocked by:** 03, 05

**Status:** done

- [x] Each package and its symbol package pass the SourceLink check, with the result recorded in the ticket
- [x] `docs/releasing.md` exists, is linked from `docs/contributing.md`, and states that the runbook has not been run
- [x] Nothing is pushed or released
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Verification record

Run on 2026-10-10 at commit `a77b0d3` (working tree with the ticket 04 and 06 changes), version `1.0.0-preview.0.237`.
Method: `dotnet build TruthWeaver.slnx -c Release --no-incremental -p:ContinuousIntegrationBuild=true -warnaserror`, then
`dotnet pack --no-build -warnaserror -p:ContinuousIntegrationBuild=true` into a scratch directory outside the repository.
Tools (installed with `--tool-path` outside the repository): `dotnet-validate 0.0.1-preview.582` (`package local` on each
`.nupkg`) and `sourcelink 3.1.1` (`print-json` and `print-documents` on the `.pdb` unpacked from each `.snupkg`). `sourcelink
test` was not run: it downloads the files from GitHub and the commit is not pushed.

| Package | dotnet-validate: Source Link / Deterministic / Compiler Flags | Symbol package pdb: `documents` mapping |
| --- | --- | --- |
| TruthWeaver | Valid / Valid / Valid | `/_/*` -> `raw.githubusercontent.com/rheone/TruthWeaver/<commit>/*`, documents under `/_/src/TruthWeaver/` |
| TruthWeaver.Abstractions | Valid / Valid / Valid | same mapping, documents under `/_/src/TruthWeaver.Abstractions/` |
| TruthWeaver.DataSources.Json | Valid / Valid / Valid | same mapping |
| TruthWeaver.Predicates | Valid / Valid / Valid | same mapping |
| TruthWeaver.Testing | Valid / Valid / Valid | same mapping |
| TruthWeaver.Yaml | Valid / Valid / Valid | same mapping |
| TruthWeaver.Generators | No files to validate (analyzer-only package) | No symbol package by design; `.nuspec` has `<repository>` with the URL and the commit |

### Defect found and fixed

The first run failed. With the `Microsoft.SourceLink.GitHub` GlobalPackageReference, no `*.sourcelink.json` was written
and `dotnet-validate` reported "Has Symbols, No Source Link" for Abstractions and Testing, "Contains untracked sources" for
the others, and "Non deterministic" for all of them. The package sets `SuppressImplicitGitSourceLink`, which switches off the
SDK Source Link, and no Source Link target ran in this repository (`_GenerateSourceLinkFile` was never invoked). Setting
`SuppressImplicitGitSourceLink=false` produced the file. The fix is to remove the GlobalPackageReference from
`Directory.Packages.props` and use the Source Link that the .NET SDK bundles, as `TruthWeaver.Generators` already did. The
repository has no lock file, so no lock file changed. After the change all six assemblies pass and the `.nuspec` files keep
the repository URL and commit.

An incremental build is not enough for this check: a `bin`/`obj` output from an earlier build without
`ContinuousIntegrationBuild` packs as "Non deterministic". `docs/releasing.md` therefore builds with `--no-incremental`.
