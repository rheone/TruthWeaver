<!-- docs-lint: on -->

# Releasing

This page is the runbook to publish a TruthWeaver version to NuGet. Back to [Contributing](contributing.md).

> [!IMPORTANT]
> **This runbook has not been run.** No version of TruthWeaver has been published. Each step below is marked
> *not run*. When you do a release, mark the steps as you do them, and correct this page where it is wrong.

All eight library packages and the `TruthWeaver.Tool` tool package share one version. MinVer reads the version from the Git tag. See
[How a package version is made](contributing.md#how-a-package-version-is-made).

## Before you start

- The `main` branch is up to date, and the CI build and `pack` jobs pass on the commit that you release.
- You have a NuGet API key that can push to the package ids. Do not put the key in the repository or in a command line
  that a log records.
- You have the full Git history and the tags. In a shallow clone the version is wrong.

## Steps

| Step | Action | Status |
| --- | --- | --- |
| 1 | Update the changelog. | Not run |
| 2 | Tag the commit. | Not run |
| 3 | Pack. | Not run |
| 4 | Check the packages and SourceLink. | Not run |
| 5 | Push the packages. | Not run |
| 6 | Push the tag. | Not run |
| 7 | Set the baseline for package validation. | Not run |

### 1. Update the changelog (not run)

In `CHANGELOG.md`, rename the `[Unreleased]` heading to the version and the date. Add an empty `[Unreleased]` heading
above it. Update the "Versioning and release status" section so that it no longer says that TruthWeaver has not been
published. Commit the change.

### 2. Tag the commit (not run)

Put an annotated tag with the prefix `v` on the release commit. Do not push it yet.

```powershell
git tag -a v1.0.0 -m "TruthWeaver 1.0.0"
```

Check the version that MinVer makes from the tag. It must equal the tag without the `v`.

```powershell
dotnet msbuild src/TruthWeaver -t:MinVer -getProperty:PackageVersion
```

### 3. Pack (not run)

Pack in Release configuration with a deterministic build. Write the packages to a directory outside the repository.

```powershell
dotnet build TruthWeaver.slnx --configuration Release --no-incremental -p:ContinuousIntegrationBuild=true
dotnet pack TruthWeaver.slnx --configuration Release --no-build -warnaserror -p:ContinuousIntegrationBuild=true --output $env:TEMP/tw-release
```

`--no-incremental` makes sure that no assembly from an earlier build without `ContinuousIntegrationBuild` is packed.

### 4. Check the packages and SourceLink (not run)

Run the package check from CI on the output directory.

```powershell
pwsh scripts/check-packages.ps1 -PackageDirectory $env:TEMP/tw-release
```

Then check SourceLink on each package with the `sourcelink` or `dotnet-validate` tool. Install the tool outside the
repository.

```powershell
dotnet tool install sourcelink --tool-path $env:TEMP/tw-tools
dotnet tool install dotnet-validate --prerelease --tool-path $env:TEMP/tw-tools
& "$env:TEMP/tw-tools/dotnet-validate" package local "$env:TEMP/tw-release/TruthWeaver.1.0.0.nupkg"
Expand-Archive "$env:TEMP/tw-release/TruthWeaver.1.0.0.snupkg" -DestinationPath "$env:TEMP/tw-symbols/TruthWeaver"
& "$env:TEMP/tw-tools/sourcelink" print-json "$env:TEMP/tw-symbols/TruthWeaver/lib/net11.0/TruthWeaver.pdb"
```

`dotnet-validate` must report "Valid" for Source Link, Deterministic and Compiler Flags. The `sourcelink` tool reads a
`.pdb` file, not a `.snupkg`, so unpack each symbol package first. `print-json` must print a `documents` entry that points
at `raw.githubusercontent.com` and the release commit. Repeat the checks for each package and each symbol package. The
`sourcelink test` command downloads each source file from GitHub, so it passes only after the commit is on GitHub. Run it
after step 6, or push the release commit first.

The `TruthWeaver.Generators` package is an exception. It is an analyzer-only package. It has no `lib` assembly and no
symbol package, so a SourceLink tool finds no file to check in it. Check the repository commit and URL in its `.nuspec`
instead. The repository commit and URL in the `.nuspec` come from the same .NET SDK Source Link that the other packages use. No project
references the `Microsoft.SourceLink.GitHub` package. The reason is in `Directory.Packages.props`.

### 5. Push the packages (not run)

Push each `.nupkg` and its `.snupkg`. The `--skip-duplicate` option makes a second run safe. Read the API key from an
environment variable.

```powershell
dotnet nuget push "$env:TEMP/tw-release/*.nupkg" --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY --skip-duplicate
```

`TruthWeaver.Tool` is the .NET tool package of the [command line](cli.md). The pack step makes it and the package check checks it, but it is not published: delete `TruthWeaver.Tool.*.nupkg` and its `.snupkg` from the output directory before the push.

`dotnet nuget push` of a `.nupkg` also pushes the `.snupkg` that is next to it. NuGet.org cannot unpublish a version.
After a push you can only unlist it. Do this step last, after all the checks pass.

### 6. Push the tag (not run)

```powershell
git push origin v1.0.0
```

### 7. Set the baseline for package validation (not run)

After the first release only. In `src/Directory.Build.props`, set `PackageValidationBaselineVersion` to the released
version, for example `1.0.0`. From then on `dotnet pack` fails on a breaking API change. Update the comment above
`EnablePackageValidation`.

## What to check afterwards

| Check | How |
| --- | --- |
| Each package is listed on nuget.org with the right version. | Open `https://www.nuget.org/packages/TruthWeaver/` and the six other package ids. |
| The README, license and repository link show on the package page. | Read the package page. |
| The symbol packages are accepted. | NuGet.org shows a "symbols" status after validation. |
| A consumer can restore the package. | In a new project, run `dotnet add package TruthWeaver --version 1.0.0`, then build. |
| The debugger steps into the source. | In that project, set the symbol server to NuGet.org and step into a TruthWeaver call. |
| SourceLink works against the pushed commit. | Run `sourcelink test` on each `.snupkg` or `.pdb`. |
| The Generators package works. | Reference `TruthWeaver.Generators` in the new project and build a `[Predicate]` method. |
| The tag points at the release commit. | `git show v1.0.0` |
