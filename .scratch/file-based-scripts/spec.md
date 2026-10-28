# File-based repository scripts

**Status:** done (tickets 01 to 08).

## Goal

Replace the eight PowerShell repository scripts with .NET file-based apps (`scripts/<name>.cs`), so a contributor needs only the .NET SDK (and Node for Prettier). The apps share helpers in `scripts/shared/`, pulled in with literal-path `#:include` directives (no globs, so build caching stays on). The apps inherit the root `Directory.Build.props` and `Directory.Packages.props`, and the build, CSharpier, `dotnet format` and Roslynator gates cover them.

## Conventions

| Topic | Convention |
| --- | --- |
| Run | `dotnet run scripts/<name>.cs -- <arguments>` from any directory in the repository. |
| Arguments | `--check-only` reports and changes nothing (it replaces `-CheckOnly`). File paths are positional arguments. |
| Exit codes | `0` when the work or the check succeeds, `1` when a check finds a problem or a step fails. A failing step names itself in the message. |
| `Repo` | Finds the repository root, runs `git`, lists staged files, splits them into wholly and partly staged, and runs a process in a directory. |
| `ConsoleProcess` | Runs a process with its output streamed to the console and returns the exit code. |
| `ProcessResult` | The captured standard output and standard error of a finished process. |
| `ScriptFailedException` | Stops a script with a message; the app prints it and exits `1`. |

## Scripts

`list-open-done-tickets`, `check-packages`, `normalize-text`, `normalize-text-staged`, `format-data`, `format-data-staged`, `format-staged` and `format-all`. The Husky task runner, the CI and release workflows and the contributor documentation call them. No `.ps1` file remains.

## Tickets

| Ticket | Change | Status |
| --- | --- | --- |
| 01 | Shared helpers and `list-open-done-tickets` | done |
| 02 | `check-packages` | done |
| 03 | `normalize-text` | done |
| 04 | `format-data` | done |
| 05 | `format-staged` | done |
| 06 | The staged wrappers | done |
| 07 | `format-all` | done |
| 08 | Cut over and delete the `.ps1` files | done |
