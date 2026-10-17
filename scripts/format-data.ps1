#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Formats the JSON, YAML and YML files with Prettier, and checks that test fixtures parse.
.DESCRIPTION
    Files outside the test data folders are formatted (or, with -CheckOnly, only checked) by Prettier. The settings
    are in .prettierrc.json and the skipped paths are in .prettierignore. Test fixtures feed tests that read exact text
    and line/column spans, so Prettier does not reformat them. They only have to parse: a syntax error is a failure.
    Pass file paths as arguments to limit the run to some files (the pre-commit hook does this for the staged files).
    Needs Node.js. The Prettier version is pinned below so every machine and CI formats the same way.
#>
param(
    [switch] $CheckOnly,
    # Paths are positional: `pwsh -File format-data.ps1 a b c` binds all three.
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $Path
)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

$prettier = 'prettier@3.9.7'
# Same data folders that normalize-text.ps1 skips.
$fixtureFolders = '(^|/)tests/.*/(Fixtures|Samples|RuleTests)/'

function Invoke-Prettier {
    param([string[]] $PrettierArguments)
    # --no-install is not used: npx downloads the pinned version on first use and caches it.
    & npx --yes $prettier @PrettierArguments
    return $LASTEXITCODE
}

if ($Path) {
    $isData = { [IO.Path]::GetExtension($_).ToLowerInvariant() -in '.json', '.yaml', '.yml' }
    $data = @($Path | Where-Object $isData | ForEach-Object { $_ -replace '\\', '/' })
    $fixtures = @($data | Where-Object { $_ -cmatch $fixtureFolders })
    $formatted = @($data | Where-Object { $_ -cnotmatch $fixtureFolders })
}
else {
    $fixtures = @('tests/**/{Fixtures,Samples,RuleTests}/**/*.{json,yaml,yml}')
    $formatted = @('**/*.{json,yaml,yml}')
}

$failed = $false

if ($formatted.Count -gt 0) {
    $mode = if ($CheckOnly) { '--check' } else { '--write' }
    Write-Host "==> prettier $mode"
    if ((Invoke-Prettier (@($mode, '--log-level', 'warn') + $formatted)) -ne 0) { $failed = $true }
}

if ($fixtures.Count -gt 0) {
    # An empty ignore file turns off .prettierignore, which lists the fixture folders. Exit code 1 means "would be
    # reformatted", which is fine for a fixture; only exit code 2 (a parse error) fails.
    $emptyIgnore = New-TemporaryFile
    try {
        Write-Host '==> prettier syntax check of test fixtures'
        $code = Invoke-Prettier (@('--check', '--log-level', 'error', '--ignore-path', $emptyIgnore.FullName) + $fixtures)
        if ($code -eq 2) { $failed = $true }
    }
    finally {
        Remove-Item -LiteralPath $emptyIgnore.FullName -Force
    }
}

if ($failed) {
    [Console]::Error.WriteLine('format-data: JSON/YAML check failed. Run scripts/format-data.ps1 to reformat; a syntax error needs a manual fix.')
    exit 1
}
Write-Host 'format-data: JSON and YAML are formatted.'
