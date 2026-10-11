#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Lists tickets marked done that still carry an unticked checklist item.
.DESCRIPTION
    Scans every Markdown file under .scratch/ whose status line starts with "done" and prints each "- [ ]" item.
    The folders engine-v1 and _superseded are skipped. An open item is "noted" when its line records why it stays
    open: a "Note YYYY-MM-DD:" marker or a "(waived:" marker. The script exits with code 1 when any open item has
    no note, so a clean run means every remaining item explains itself.
    Ticking an item or adding a note is a manual step. Check the evidence (code, tests, commit) first.
#>
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

$statusPattern = '^\s*(\*\*Status:\*\*|Status:)\s*done\b'
$notePattern = 'Note \d{4}-\d{2}-\d{2}:|\(waived:'
$unnoted = 0

Get-ChildItem -Path '.scratch' -Recurse -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '[\\/](engine-v1|_superseded)[\\/]' } |
    Sort-Object FullName |
    ForEach-Object {
        $lines = Get-Content -LiteralPath $_.FullName
        if (-not ($lines | Where-Object { $_ -match $statusPattern })) { return }

        $open = for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^\s*- \[ \]') { [pscustomobject]@{ Line = $i + 1; Text = $lines[$i].Trim() } }
        }
        if (-not $open) { return }

        $relative = Resolve-Path -LiteralPath $_.FullName -Relative
        Write-Host $relative
        foreach ($item in $open) {
            $noted = $item.Text -match $notePattern
            if (-not $noted) { $unnoted++ }
            $tag = if ($noted) { 'noted' } else { 'NO NOTE' }
            Write-Host ("  {0,4} [{1}] {2}" -f $item.Line, $tag, $item.Text)
        }
    }

if ($unnoted -gt 0) {
    [Console]::Error.WriteLine("list-open-done-tickets: $unnoted open item(s) have no note.")
    exit 1
}
Write-Host 'list-open-done-tickets: every open item in a done ticket has a note.'
