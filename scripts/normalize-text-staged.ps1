#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit step: replaces em dashes and ellipsis characters in the staged files and re-stages them.
.DESCRIPTION
    A file whose changes are all staged is rewritten by normalize-text.ps1 and re-staged. A file that is only
    partly staged cannot be re-staged without also staging its unstaged edits, so it is only checked: the commit
    fails with a message and the developer runs scripts/normalize-text.ps1, then stages the hunks they want.
    Called by .husky/task-runner.json.
#>
$ErrorActionPreference = 'Stop'

# core.quotepath=off keeps non-ASCII paths readable instead of octal-escaped.
$staged = @(git -c core.quotepath=off diff --cached --name-only --diff-filter=ACMR)
if ($staged.Count -eq 0) { exit 0 }

$unstaged = @(git -c core.quotepath=off diff --name-only)
$partial = @($staged | Where-Object { $unstaged -contains $_ })
$whole = @($staged | Where-Object { $unstaged -notcontains $_ })

if ($whole.Count -gt 0) {
    pwsh -NoProfile -File scripts/normalize-text.ps1 @whole
    if ($LASTEXITCODE -ne 0) { exit 1 }
    # Re-staging an unchanged file is a no-op, so every whole file is added.
    git add -- @whole
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

if ($partial.Count -gt 0) {
    pwsh -NoProfile -File scripts/normalize-text.ps1 -CheckOnly @partial
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine('pre-commit: partly staged files above need normalize-text; run scripts/normalize-text.ps1, then stage the hunks you want.')
        exit 1
    }
}
