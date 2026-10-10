#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Formats the whole repository, then runs the same three checks as the CI build job.
.DESCRIPTION
    Run this before you push. It applies CSharpier, then dotnet format, then CSharpier again (dotnet format can
    move a token that CSharpier then moves back). It then runs the CI gates: csharpier check, dotnet format
    --verify-no-changes --severity info, and Roslynator. A failure in the first two after the formatters ran means
    they disagree about some code. Reshape that code. See CLAUDE.md, section Formatting.
    Use -CheckOnly to skip the apply step and only run the gates.
#>
param([switch] $CheckOnly)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

function Invoke-Checked {
    param([string] $Description, [scriptblock] $Command)
    Write-Host "==> $Description"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("format-all: $Description failed (exit code $LASTEXITCODE).")
        exit 1
    }
}

if (-not $CheckOnly) {
    Invoke-Checked 'csharpier format' { dotnet csharpier format . }
    Invoke-Checked 'dotnet format' { dotnet format TruthWeaver.slnx --no-restore --severity info }
    Invoke-Checked 'csharpier format (second pass)' { dotnet csharpier format . }
}

# These three commands are the CI gates in .github/workflows/ci.yml. Keep them identical to it.
Invoke-Checked 'csharpier check' { dotnet csharpier check . }
Invoke-Checked 'dotnet format check' { dotnet format TruthWeaver.slnx --no-restore --verify-no-changes --severity info }
Invoke-Checked 'roslynator' { dotnet roslynator analyze TruthWeaver.slnx }
Write-Host 'format-all: all gates pass.'
