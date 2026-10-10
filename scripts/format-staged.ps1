#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit step: formats the staged C# files and re-stages them, without sweeping in unstaged edits.
.DESCRIPTION
    A file whose changes are all staged is formatted (CSharpier, then dotnet format) and re-staged, so a commit
    never fails on formatting. A file that is only partly staged (it also has unstaged edits) cannot be
    re-staged without also staging those edits, so it is only checked: the commit fails with a message and
    the developer formats it, then stages the hunks they want. Called by .husky/task-runner.json.
#>
$ErrorActionPreference = 'Stop'

# core.quotepath=off keeps non-ASCII paths readable instead of octal-escaped.
$staged = @(git -c core.quotepath=off diff --cached --name-only --diff-filter=ACMR -- '*.cs')
if ($staged.Count -eq 0) { exit 0 }

$unstaged = @(git -c core.quotepath=off diff --name-only -- '*.cs')
$partial = @($staged | Where-Object { $unstaged -contains $_ })
$whole = @($staged | Where-Object { $unstaged -notcontains $_ })

function Invoke-Checked {
    param([string] $Description, [scriptblock] $Command)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("pre-commit: $Description failed (exit code $LASTEXITCODE).")
        exit 1
    }
}

if ($whole.Count -gt 0) {
    # Both formatters run, then CSharpier runs again, because dotnet format can move a token that CSharpier then
    # moves back. The two checks after that are the CI gates. If they still fail, the formatters disagree about
    # this code and no amount of formatting settles it: the code needs a different shape, not another run.
    Invoke-Checked 'csharpier format' { dotnet csharpier format @whole }
    Invoke-Checked 'dotnet format' { dotnet format --no-restore --severity info --include @whole }
    Invoke-Checked 'csharpier format (second pass)' { dotnet csharpier format @whole }
    Invoke-Checked 'csharpier check' { dotnet csharpier check @whole }
    Invoke-Checked 'dotnet format check (CSharpier and dotnet format disagree on these files; reshape the code, see CLAUDE.md "Formatting")' {
        dotnet format --no-restore --severity info --verify-no-changes --include @whole
    }
    Invoke-Checked 'git add' { git add -- @whole }
}

if ($partial.Count -gt 0) {
    Write-Host "Partly staged files are checked, not rewritten (a rewrite would stage your unstaged edits):`n  $($partial -join "`n  ")"
    Invoke-Checked 'csharpier check' { dotnet csharpier check @partial }
    Invoke-Checked 'dotnet format check' { dotnet format --no-restore --severity info --verify-no-changes --include @partial }
}
