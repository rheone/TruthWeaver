# Contributing: the pre-commit hook and format-all

This page describes the two tools that keep a change formatted and building: the Git pre-commit hook and
`scripts/format-all.cs`. Back to the [README](../README.md).

## The pre-commit hook

The first `dotnet restore` of a clone installs the hook. The hook runs the `pre-commit` group of
`.husky/task-runner.json` through Husky.Net. Set `HUSKY=0` to skip the hook. A restore with `CI=true` does not install it.

Husky.Net runs a task only when a staged file matches its `include` patterns. A commit that stages no C# or project file
therefore skips all three tasks. The tasks run in this order, and a failed task stops the commit:

| Step | Runs when a staged file matches | What it does |
| --- | --- | --- |
| `format-staged` | `**/*.cs` | Formats each fully staged C# file and stages the result again. Checks, but does not rewrite, a partly staged file. |
| `build` | `**/*.cs`, `**/*.csproj`, `**/*.props`, `**/*.targets`, `**/*.slnx`, `global.json` | Runs `dotnet build --no-restore --configuration Release -p:CI=true`. With `CI=true`, an analyzer warning fails the build, as it does in CI. |
| `test` | The same files as `build` | Runs `dotnet test --no-build --configuration Release`. It runs the whole test suite of the solution, not only the tests for the staged files. |

### How `format-staged` treats a file

`scripts/format-staged.cs` splits the staged C# files into two groups.

- **A fully staged file** has no unstaged edits. The script runs CSharpier, then `dotnet format`, then CSharpier again.
  It then checks the file with both tools and stages the formatted file. A commit never fails on the formatting of such
  a file.
- **A partly staged file** also has unstaged edits. The script cannot stage the formatted file without also staging those
  edits. It only checks the file with both tools. If the file needs formatting, the commit fails. Format the file, then
  stage the hunks that you want.

If the check fails for a fully staged file, CSharpier and `dotnet format` disagree about some code. Change the shape of
the code. Do not suppress the finding, and do not edit `.editorconfig` for it.

Roslynator does not run in the hook. It runs in CI and in `scripts/format-all.cs`.

## format-all

Run `dotnet run scripts/format-all.cs` before you push. It works on the whole repository, not only on staged files.

By default the script **rewrites files in your working tree**. It runs these steps and stops at the first failure:

1. `dotnet csharpier format .`
2. `dotnet format TruthWeaver.slnx --no-restore --severity info`
3. `dotnet csharpier format .` again, because `dotnet format` can move a token that CSharpier then moves back.
4. `dotnet csharpier check .`
5. `dotnet format TruthWeaver.slnx --no-restore --verify-no-changes --severity info`
6. `dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics`

Steps 4 to 6 are the formatting gates of the CI build job. Read the whole output, because findings at the info level fail
the CI step too.

Pass `--check-only` to skip steps 1 to 3. The script then changes no file and only runs the three gates:

```powershell
dotnet run scripts/format-all.cs -- --check-only
```

## Before you push

The hook builds and tests only the files that you stage, and it does not run Roslynator. Run `format-all`, then
`dotnet test`, before you push. The full list of checks is in the Required validation section of `CLAUDE.md`.

## API conventions

- **Try first.** Every public member named `XOrThrow` has a public sibling named `TryX` that returns `bool` and reports the
  failure through an `out` parameter. For example, `CompilationResult<TContext>.GetRuleOrThrow` has `TryGetRule`.
  `TruthWeaver.Architecture.Tests` fails when a sibling is missing.
- **Docs show the `Try` form first.** An example in a documentation page uses the `Try` form. It uses the `OrThrow` form
  only where a failure is a bug, such as at startup or in a test, and says so.

## Central package rules

Every package version is in `Directory.Packages.props`. These rules apply to every project:

- **One version per package.** A project writes `<PackageReference Include="X" />` with no version. A `VersionOverride`
  attribute is an error (`CentralPackageVersionOverrideEnabled` is `false`).
- **Analyzers are global.** The analyzer packages and MinVer are `GlobalPackageReference` items with
  `PrivateAssets="all"`, so they apply to every project and never become a dependency of a shipped package.
  The pack check (`scripts/check-packages.cs`) fails when a package contains an unexpected file.
- **Transitive pinning is on.** A `PackageVersion` item for a package that only a dependency pulls in raises that
  package to the version in the item. Use it to fix a vulnerable transitive package, and name the advisory in a comment.
  If a pin is lower than a dependency needs, restore fails with NU1109. Raise the pin.
- **NuGetAudit checks every package.** The audit covers direct and transitive packages at every severity
  (`NuGetAuditMode` is `all`, `NuGetAuditLevel` is `low`). A vulnerability warning (NU1900 to NU1904) is a warning in a
  local build and an error when `CI=true`.
- **Restore is mapped.** `NuGet.config` has one source, nuget.org, and a `packageSourceMapping` entry with the
  pattern `*`. When you add a source, add its own patterns in the same change. Run restore with `--locked-mode`.

## How package updates arrive

Dependabot (`.github/dependabot.yml`) checks NuGet packages and GitHub Actions once a week and opens grouped pull
requests: analyzers, test packages, and all other packages (and `Microsoft.Extensions.*` after .NET 11 GA). It updates
the versions in `Directory.Packages.props`. The deliberate pins are on its ignore list, each with a reason: the
`Microsoft.Extensions.*` RC line, `StyleCop.Analyzers` beta, `Microsoft.CodeAnalysis.CSharp` and
`Microsoft.CodeAnalysis.Analyzers`. When you change a pin, change the ignore list in the same pull request.

Dependabot does not check the local tools in `dotnet-tools.json`. Check them by hand with `dotnet tool list` and update the
versions in the file.

## How a package version is made

[MinVer](https://github.com/adamralph/minver) derives the version of every package from the Git tags. No project sets a
`Version`. All packages share one version. The settings are in `Directory.Build.props` and the MinVer package reference
is in `Directory.Packages.props`. Only projects that pack get MinVer.

| Commit | Version |
| --- | --- |
| A commit with the tag `v1.2.3` | `1.2.3` |
| The 4th commit after the tag `v1.2.3` | `1.2.4-preview.0.4` |
| Any commit, when the repository has no `v` tag | `1.0.0-preview.0.N` (`N` is the commit height) |
| Any commit, with a tag below `v1.0` | `1.0.0-preview.0.N` (`MinVerMinimumMajorMinor` is `1.0`) |

To make a release version, put a tag with the prefix `v` on the commit, for example `v1.0.0`. A tag without the prefix
is ignored. To override the version for one build, pass `-p:MinVerVersionOverride=1.2.3`.

MinVer needs the tags and the commit history. CI checks out with `fetch-depth: 0` for this reason. In a shallow clone
the version is wrong. After `git clone --depth`, run `git fetch --unshallow --tags`.

To see the version without packing, run `dotnet msbuild src/TruthWeaver -t:MinVer -getProperty:PackageVersion`.

## The pack check

The `pack` job in `.github/workflows/ci.yml` packs every package and checks the result. It never pushes or publishes a
package. It has no NuGet API key and the job permission is `contents: read`. The job runs these steps:

1. `dotnet pack TruthWeaver.slnx --configuration Release -warnaserror`. A pack warning fails the job. Package validation
   (`EnablePackageValidation` in `src/Directory.Build.props`) runs in this step. It checks the package layout and the
   public API across target frameworks.
2. `dotnet run scripts/check-packages.cs -- <directory>`. For each of the ten library packages and the tool package, the script checks that
   the `.nupkg` exists, contains `README.md` and contains only the expected files. It also checks that the symbol package
   (`.snupkg`) contains the `.pdb` file. `TruthWeaver.Generators` is an analyzer-only package and has no symbol package.
   The script fails when it finds a symbol package for it.

To run the same checks on your machine, pack into a directory outside the repository, then run the script on it:

```powershell
dotnet pack TruthWeaver.slnx --configuration Release -warnaserror --output $env:TEMP/tw-packages
dotnet run scripts/check-packages.cs -- $env:TEMP/tw-packages
```

`PackageValidationBaselineVersion` is not set, because no version has been released. After the first release, set it to
that version so that pack fails on a breaking API change.

## Repository scripts

New repository scripts are .NET file-based apps. You run them with `dotnet run scripts/<name>.cs`. Shared helpers are in
`scripts/shared/`. A script includes a helper with a literal-path `#:include shared/<file>.cs` line.

`scripts/list-open-done-tickets.cs` lists the `.scratch` files marked done that still have an unticked checklist item. It exits
with code 1 when an open item has no note:

```powershell
dotnet run scripts/list-open-done-tickets.cs
```

`scripts/check-packages.cs` checks the `.nupkg` and `.snupkg` files that `dotnet pack` wrote: the expected package set, a
README in each package, symbol packages where expected and no unexpected files. The package directory is a required
argument. The script exits with code 1 when a check fails:

```powershell
dotnet pack --output artifacts/packages
dotnet run scripts/check-packages.cs -- artifacts/packages
```

`scripts/normalize-text.cs` makes text files valid UTF-8 with no byte order mark, LF line endings, no trailing whitespace
and one final newline, and replaces em dashes, ellipsis characters and curly quotes. It rewrites the files that Git tracks
or would track. Pass `--check-only` to rewrite nothing and exit with code 1 when a file needs a change. Pass file paths to
limit the run to those files. The script also exits with code 1 for a file that is not valid UTF-8 or that holds a hidden
character:

```powershell
dotnet run scripts/normalize-text.cs
dotnet run scripts/normalize-text.cs -- --check-only
dotnet run scripts/normalize-text.cs -- docs/contributing.md README.md
```

`scripts/format-data.cs` runs the pinned Prettier over the JSON, YAML and YML files, and only checks that the test
fixture folders parse. It needs Node.js and stops with a message and exit code 1 when `node` is not on `PATH`. Pass
`--check-only` to exit with code 1 when a file needs formatting. Pass file paths to limit the run to those files:

```powershell
dotnet run scripts/format-data.cs
dotnet run scripts/format-data.cs -- --check-only
```

`scripts/format-staged.cs` is the pre-commit step for C# files. It formats the fully staged `.cs` files (CSharpier, `dotnet format`,
CSharpier again) and stages them again. It only checks a partly staged file (one with unstaged edits) and exits with code 1
when that file needs formatting. `scripts/normalize-text-staged.cs` and `scripts/format-data-staged.cs` do the same with
`normalize-text.cs` (all staged files) and `format-data.cs` (staged JSON, YAML and YML files). They take no arguments:

```powershell
dotnet run scripts/format-staged.cs
dotnet run scripts/normalize-text-staged.cs
dotnet run scripts/format-data-staged.cs
```

`scripts/format-all.cs` formats the whole repository and then runs the CI gates, in this order: `normalize-text`, `format-data`,
`csharpier format`, `dotnet format`, `csharpier format` again, the `normalize-text` and `format-data` checks, `csharpier check`,
`dotnet format` check and Roslynator. It stops at the first failing step, names the step and exits with code 1. Pass
`--check-only` to skip the first five (apply) steps:

```powershell
dotnet run scripts/format-all.cs
dotnet run scripts/format-all.cs -- --check-only
```

## Releasing

[Releasing](releasing.md) lists the steps to publish a version and the checks to make afterwards.
