#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Replaces em dashes and ellipsis characters with their ASCII equivalents in text and source files.
.DESCRIPTION
    Em dash (U+2014) becomes " - " and the ellipsis character (U+2026) becomes "...". The script covers the
    documentation, source and script files that Git tracks or would track (not ignored), and skips every path
    that has a segment starting with "." (.claude, .scratch, .github, .tmp and so on). A file keeps its BOM
    (or lack of one) and its line endings.
    Use -CheckOnly to rewrite nothing: the script lists the files that need a change and exits with code 1.
    Pass -Path to limit the run to some files (the pre-commit hook does this for the staged files).
#>
param(
    [switch] $CheckOnly,
    [string[]] $Path
)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

# Extensions that hold prose, code or scripts. Data fixtures (*.json, *.rule, *.tmpl) are left alone on purpose:
# they can carry non-ASCII text that a test depends on.
$extensions = @(
    '.md', '.txt', '.cs', '.ps1', '.py', '.mjs', '.sh', '.yml', '.yaml',
    '.props', '.targets', '.csproj', '.slnx', '.xsd'
)

if ($Path) {
    $candidates = $Path
}
else {
    # core.quotepath=off keeps non-ASCII paths readable instead of octal-escaped.
    $candidates = @(git -c core.quotepath=off ls-files --cached --others --exclude-standard)
}

$files = $candidates | Where-Object {
    $isHidden = ($_ -split '[\\/]') | Where-Object { $_.StartsWith('.') }
    (-not $isHidden) -and ($extensions -contains [IO.Path]::GetExtension($_).ToLowerInvariant()) -and (Test-Path -LiteralPath $_ -PathType Leaf)
}

$emDash = [string][char]0x2014
$ellipsis = [string][char]0x2026
$utf8NoBom = [Text.UTF8Encoding]::new($false)
$changed = [Collections.Generic.List[string]]::new()

foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $file))
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = $utf8NoBom.GetString($bytes, $(if ($hasBom) { 3 } else { 0 }), $bytes.Length - $(if ($hasBom) { 3 } else { 0 }))
    if (-not $text.Contains($emDash) -and -not $text.Contains($ellipsis)) { continue }

    # A dash that starts a line becomes a list-style hyphen and one that ends a line loses its trailing space,
    # so the replacement never adds a stray space or joins two lines.
    $new = $text -replace "(?m)^([ \t]*)$emDash[ \t]*", '$1- '
    $new = $new -replace "(?m)[ \t]*$emDash[ \t]*$", ' -'
    $new = $new -replace "[ \t]*$emDash[ \t]*", ' - '
    $new = $new.Replace($ellipsis, '...')

    $changed.Add($file)
    if (-not $CheckOnly) {
        $out = $utf8NoBom.GetBytes($new)
        if ($hasBom) { $out = [byte[]](0xEF, 0xBB, 0xBF) + $out }
        [IO.File]::WriteAllBytes((Resolve-Path -LiteralPath $file), $out)
    }
}

if ($changed.Count -eq 0) {
    Write-Host 'normalize-ascii: no em dash or ellipsis characters found.'
    exit 0
}
if ($CheckOnly) {
    [Console]::Error.WriteLine("normalize-ascii: $($changed.Count) file(s) contain an em dash or ellipsis character (run scripts/normalize-ascii.ps1):`n  $($changed -join "`n  ")")
    exit 1
}
Write-Host "normalize-ascii: rewrote $($changed.Count) file(s)."
