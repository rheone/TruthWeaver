<#
.SYNOPSIS
    Fails when any given text file contains a bare LF line ending.
.DESCRIPTION
    .gitattributes sets eol=crlf, so working-tree text files must use CRLF. Git silently normalises
    LF to CRLF-in-index/LF-in-blob on commit, which hides the problem from diffs and CI, so a
    scripted edit that writes LF goes unnoticed until it is repaired by hand. Husky passes the
    staged files; the check reads the working-tree bytes. Missing and binary (NUL-containing)
    files are skipped.
.EXAMPLE
    pwsh -NoProfile -File scripts/check-line-endings.ps1 src/Foo.cs tests/FooTests.cs
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Path = @()
)

$failed = @()
foreach ($file in $Path) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
    $bytes = [System.IO.File]::ReadAllBytes($file)
    if ([Array]::IndexOf($bytes, [byte]0) -ge 0) { continue }
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 10 -and ($i -eq 0 -or $bytes[$i - 1] -ne 13)) {
            $failed += $file
            break
        }
    }
}

if ($failed.Count -gt 0) {
    Write-Host 'Bare LF line endings found (repository requires CRLF, see .gitattributes):'
    $failed | ForEach-Object { Write-Host "  $_" }
    Write-Host 'Convert with: (Get-Content -Raw <file>) -replace "\r?\n", "`r`n" | Set-Content -NoNewline <file>'
    exit 1
}
exit 0
