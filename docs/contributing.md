# Contributing: the pre-commit hook and format-all

This page describes the two tools that keep a change formatted and building: the Git pre-commit hook and
`scripts/format-all.ps1`. Back to the [README](../README.md).

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

`scripts/format-staged.ps1` splits the staged C# files into two groups.

- **A fully staged file** has no unstaged edits. The script runs CSharpier, then `dotnet format`, then CSharpier again.
  It then checks the file with both tools and stages the formatted file. A commit never fails on the formatting of such
  a file.
- **A partly staged file** also has unstaged edits. The script cannot stage the formatted file without also staging those
  edits. It only checks the file with both tools. If the file needs formatting, the commit fails. Format the file, then
  stage the hunks that you want.

If the check fails for a fully staged file, CSharpier and `dotnet format` disagree about some code. Change the shape of
the code. Do not suppress the finding, and do not edit `.editorconfig` for it.

Roslynator does not run in the hook. It runs in CI and in `format-all.ps1`.

## format-all.ps1

Run `pwsh scripts/format-all.ps1` before you push. It works on the whole repository, not only on staged files.

By default the script **rewrites files in your working tree**. It runs these steps and stops at the first failure:

1. `dotnet csharpier format .`
2. `dotnet format TruthWeaver.slnx --no-restore --severity info`
3. `dotnet csharpier format .` again, because `dotnet format` can move a token that CSharpier then moves back.
4. `dotnet csharpier check .`
5. `dotnet format TruthWeaver.slnx --no-restore --verify-no-changes --severity info`
6. `dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics`

Steps 4 to 6 are the formatting gates of the CI build job. Read the whole output, because findings at the info level fail
the CI step too.

Pass `-CheckOnly` to skip steps 1 to 3. The script then changes no file and only runs the three gates:

```powershell
pwsh scripts/format-all.ps1 -CheckOnly
```

## Before you push

The hook builds and tests only the files that you stage, and it does not run Roslynator. Run `format-all.ps1`, then
`dotnet test`, before you push. The full list of checks is in the Required validation section of `CLAUDE.md`.
