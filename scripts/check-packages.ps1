#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks the packages that dotnet pack wrote: the expected set, a README in each, symbol packages where expected,
    and no unexpected files.
.DESCRIPTION
    CI runs this after `dotnet pack`. The script only reads the .nupkg and .snupkg files in -PackageDirectory.
    It never pushes or publishes anything. It exits with code 1 when a check fails.
.PARAMETER PackageDirectory
    The directory that `dotnet pack --output` wrote to.
#>
param([Parameter(Mandatory)] [string] $PackageDirectory)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Every package that dotnet pack makes. TruthWeaver.Generators is an analyzer-only package and has no symbol package.
# TruthWeaver.Tool is the .NET tool of the command line; CI packs and checks it but does not publish it.
$packages = @(
    'TruthWeaver', 'TruthWeaver.Abstractions', 'TruthWeaver.DataSources.Json', 'TruthWeaver.Generators',
    'TruthWeaver.Predicates', 'TruthWeaver.Testing', 'TruthWeaver.Yaml', 'TruthWeaver.Tool'
)
$noSymbols = @('TruthWeaver.Generators')
# .NET tool packages carry the tool and its dependencies under tools/<tfm>/any/, not lib/.
$toolPackages = @('TruthWeaver.Tool')
$toolFolder = 'tools/net11.0/any/'

# NuGet adds the first four entries to every package. The package id decides the others.
$commonEntries = @('_rels/.rels', '[Content_Types].xml', 'package/services/metadata/core-properties/nuget.psmdcp', 'README.md')

function Get-Entries {
    param([string] $Path)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try { return @($zip.Entries | ForEach-Object { $_.FullName }) } finally { $zip.Dispose() }
}

$failures = [System.Collections.Generic.List[string]]::new()
function Add-Failure { param([string] $Message) $failures.Add($Message); [Console]::Error.WriteLine("check-packages: $Message") }

$files = @(Get-ChildItem -LiteralPath $PackageDirectory -File | Where-Object { $_.Extension -in '.nupkg', '.snupkg' })

foreach ($id in $packages) {
    # The version part starts with a digit, so TruthWeaver does not match TruthWeaver.Yaml.1.0.0.
    $pattern = '^' + [regex]::Escape($id) + '\.(\d.*)\.(nupkg|snupkg)$'
    $nupkg = @($files | Where-Object { $_.Name -match $pattern -and $_.Extension -eq '.nupkg' })
    $snupkg = @($files | Where-Object { $_.Name -match $pattern -and $_.Extension -eq '.snupkg' })

    if ($nupkg.Count -ne 1) { Add-Failure "$id : expected one .nupkg, found $($nupkg.Count)."; continue }
    $entries = Get-Entries $nupkg[0].FullName
    Write-Host "==> $($nupkg[0].Name)"

    foreach ($required in $commonEntries) {
        if ($entries -notcontains $required) { Add-Failure "$id : the package has no $required." }
    }

    if ($id -in $toolPackages) {
        # Every file of the tool lives in the tool folder; the tool settings and the tool assembly must be there.
        $allowed = $commonEntries + @("$id.nuspec") + @($entries | Where-Object { $_.StartsWith($toolFolder) })
        foreach ($required in @("${toolFolder}DotnetToolSettings.xml", "${toolFolder}$id.dll")) {
            if ($entries -notcontains $required) { Add-Failure "$id : the tool package has no $required." }
        }
        if ($snupkg.Count -ne 1) {
            Add-Failure "$id : expected one .snupkg, found $($snupkg.Count)."
        }
        elseif ((Get-Entries $snupkg[0].FullName) -notcontains "${toolFolder}$id.pdb") {
            Add-Failure "$id : the symbol package has no ${toolFolder}$id.pdb."
        }
    }
    elseif ($id -in $noSymbols) {
        # An analyzer-only package carries analyzers/ and no lib/ assembly.
        $allowed = $commonEntries + @("$id.nuspec", "analyzers/dotnet/cs/$id.dll")
        if ($snupkg.Count -ne 0) { Add-Failure "$id : a symbol package exists, but this package should have none." }
    }
    else {
        $allowed = $commonEntries + @("$id.nuspec", "lib/net11.0/$id.dll", "lib/net11.0/$id.xml")
        # The compiler package ships the rule-tree and decision-record JSON schemas as content.
        if ($id -eq 'TruthWeaver') { $allowed += 'content/rule-tree.schema.json', 'content/decision-record.schema.json' }
        # The testing package ships the JSON schema of a rule test file as content.
        if ($id -eq 'TruthWeaver.Testing') { $allowed += 'content/rule-test.schema.json' }
        if ($snupkg.Count -ne 1) {
            Add-Failure "$id : expected one .snupkg, found $($snupkg.Count)."
        }
        else {
            $symbolEntries = Get-Entries $snupkg[0].FullName
            if ($symbolEntries -notcontains "lib/net11.0/$id.pdb") { Add-Failure "$id : the symbol package has no lib/net11.0/$id.pdb." }
        }
    }

    foreach ($entry in $entries) {
        if ($entry -notin $allowed) { Add-Failure "$id : unexpected file in the package: $entry" }
    }
}

# Any other package in the directory is a project that should not pack.
foreach ($file in $files) {
    $known = $packages | Where-Object { $file.Name -match ('^' + [regex]::Escape($_) + '\.\d') }
    if (-not $known) { Add-Failure "unexpected package file: $($file.Name)" }
}

if ($failures.Count -gt 0) {
    [Console]::Error.WriteLine("check-packages: $($failures.Count) problem(s).")
    exit 1
}
Write-Host 'check-packages: all packages pass.'
