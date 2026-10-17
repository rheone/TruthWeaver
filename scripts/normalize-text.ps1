#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Normalizes the text of documentation, source, script and data files.
.DESCRIPTION
    Applies these rules to the files that Git tracks or would track (not ignored), and skips every path that has a
    segment starting with "." (.claude, .scratch, .github, .tmp and so on):
      - The file must be valid UTF-8. An invalid file is reported and the script exits with code 1.
      - A UTF-8 byte order mark is removed, except from *.ps1 (Windows PowerShell 5.1 reads a BOM-less file as ANSI).
      - An em dash (U+2014) becomes " - ", the ellipsis character (U+2026) becomes "..." and the curly quotes
        (U+2018, U+2019, U+201C, U+201D) become straight quotes. $curlyQuoteAllowlist lists the files that keep them.
      - An en dash (U+2013) becomes "-" and a no-break space (U+00A0) becomes a space.
      - A hidden or garbled character (see $blocklist) is reported and the script exits with code 1.
      - Every line ending becomes LF.
      - Trailing spaces and tabs are removed, except the two-space hard break in Markdown. C# files keep their line
        content, because dotnet format owns it and a trim could change a string literal.
      - A non-empty file ends with exactly one LF.
    Test fixtures (see $dataFixtures) are left alone: tests read them for exact text and line/column spans.
    Use -CheckOnly to rewrite nothing: the script lists the files that need a change and exits with code 1.
    Pass file paths as arguments to limit the run to some files (the pre-commit hook does this for the staged files).
#>
param(
    [switch] $CheckOnly,
    # ValueFromRemainingArguments lets `pwsh -File normalize-text.ps1 -Path a b c` bind all three paths.
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $Path
)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

# Extensions that hold prose, code, scripts or data.
$extensions = @(
    '.md', '.txt', '.cs', '.ps1', '.py', '.mjs', '.sh', '.yml', '.yaml', '.json',
    '.props', '.targets', '.csproj', '.slnx', '.xsd'
)

# Test data that tests read for exact text and spans, and a vendored third-party schema.
$dataFixtures = '(^|/)tests/.*/(Fixtures|Samples|RuleTests)/|(^|/)tests/.*/TestSupport/.*\.json$'

if ($Path) {
    $candidates = $Path
}
else {
    # core.quotepath=off keeps non-ASCII paths readable instead of octal-escaped.
    $candidates = @(git -c core.quotepath=off ls-files --cached --others --exclude-standard)
}

$files = $candidates | Where-Object {
    $normalized = $_ -replace '\\', '/'
    $isHidden = ($normalized -split '/') | Where-Object { $_.StartsWith('.') }
    (-not $isHidden) -and
    ($normalized -cnotmatch $dataFixtures) -and
    ($extensions -contains [IO.Path]::GetExtension($_).ToLowerInvariant()) -and
    (Test-Path -LiteralPath $_ -PathType Leaf)
}

$emDash = [string][char]0x2014
$ellipsis = [string][char]0x2026
$enDash = [string][char]0x2013
$noBreakSpace = [string][char]0x00A0
# Never allowed: soft hyphen, zero-width and direction marks, line and paragraph separators, bidirectional
# controls (they can reorder displayed code), the word joiner, a byte order mark inside a file and the replacement
# character (a sign of broken encoding). Write one as a \u escape when code needs it as data.
$blocklist = '[' + ((@(0xAD) + (0x200B..0x200F) + @(0x2028, 0x2029) + (0x202A..0x202E) + @(0x2060) + (0x2066..0x2069) + @(0xFEFF, 0xFFFD) | ForEach-Object { [string][char]$_ }) -join '') + ']'
$leftSingle = [string][char]0x2018
$rightSingle = [string][char]0x2019
$leftDouble = [string][char]0x201C
$rightDouble = [string][char]0x201D
# Files where a curly quote is the expected text: the AsciiMath printer maps a straight double quote to U+201D.
$curlyQuoteAllowlist = @(
    'docs/equations.md',
    'docs/examples.md',
    'src/TruthWeaver/Printing/AsciiMathText.cs',
    'tests/TruthWeaver.Tests/ConstantStyleTests.cs',
    'tests/TruthWeaver.Tests/EquationAsciiMathTests.cs'
)
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$utf8NoBom = [Text.UTF8Encoding]::new($false)
$changed = [Collections.Generic.List[string]]::new()
$invalid = [Collections.Generic.List[string]]::new()
$blocked = [Collections.Generic.List[string]]::new()

foreach ($file in $files) {
    $extension = [IO.Path]::GetExtension($file).ToLowerInvariant()
    $bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $file))
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $skip = if ($hasBom) { 3 } else { 0 }
    try {
        $text = $strictUtf8.GetString($bytes, $skip, $bytes.Length - $skip)
    }
    catch {
        $invalid.Add($file)
        continue
    }

    # A dash that starts a line becomes a list-style hyphen and one that ends a line loses its trailing space,
    # so the replacement never adds a stray space or joins two lines.
    $new = $text -replace "(?m)^([ \t]*)$emDash[ \t]*", '$1- '
    $new = $new -replace "(?m)[ \t]*$emDash[ \t]*$", ' -'
    $new = $new -replace "[ \t]*$emDash[ \t]*", ' - '
    $new = $new.Replace($ellipsis, '...')

    # Curly quotes become straight quotes, except in the files that document or test the AsciiMath printer, which
    # replaces a straight double quote with U+201D on purpose.
    if ($file -replace '\\', '/' -cnotin $curlyQuoteAllowlist) {
        $new = $new.Replace($leftSingle, "'").Replace($rightSingle, "'").Replace($leftDouble, '"').Replace($rightDouble, '"')
    }

    $new = $new.Replace($enDash, '-').Replace($noBreakSpace, ' ')

    # Characters that hide or garble text cannot be fixed by a replacement. Report them with line and code point.
    $hidden = [Text.RegularExpressions.Regex]::Matches($new, $blocklist)
    foreach ($match in $hidden) {
        $line = ($new.Substring(0, $match.Index) -split "`n").Count
        $blocked.Add(('{0}:{1}: U+{2:X4}' -f $file, $line, [int][char]$match.Value))
    }

    $new = $new.Replace("`r`n", "`n").Replace("`r", "`n")

    if ($extension -eq '.md') {
        # Two trailing spaces after text are a Markdown hard break, so they stay (as exactly two spaces).
        $new = [Text.RegularExpressions.Regex]::Replace($new, '(?m)(?<=\S)[ \t]{2,}$|[ \t]+$', { param($m) if ($m.Index -gt 0 -and $m.Value.Length -ge 2 -and $m.Value -match '^ +$' -and $new[$m.Index - 1] -match '\S') { '  ' } else { '' } })
    }
    elseif ($extension -ne '.cs') {
        $new = $new -replace '(?m)[ \t]+$', ''
    }

    if ($new.Length -gt 0) {
        $new = $new.TrimEnd("`n") + "`n"
        if ($new -eq "`n") { $new = '' }
    }

    $keepBom = $hasBom -and $extension -eq '.ps1'
    if ($new -ceq $text -and $hasBom -eq $keepBom) { continue }

    $changed.Add($file)
    if (-not $CheckOnly) {
        $out = $utf8NoBom.GetBytes($new)
        if ($keepBom) { $out = [byte[]](0xEF, 0xBB, 0xBF) + $out }
        [IO.File]::WriteAllBytes((Resolve-Path -LiteralPath $file), $out)
    }
}

if ($invalid.Count -gt 0) {
    [Console]::Error.WriteLine("normalize-text: $($invalid.Count) file(s) are not valid UTF-8 (fix the encoding by hand):`n  $($invalid -join "`n  ")")
}
if ($blocked.Count -gt 0) {
    [Console]::Error.WriteLine("normalize-text: $($blocked.Count) blocked character(s) (remove them, or write them as \u escapes):`n  $($blocked -join "`n  ")")
}
if ($CheckOnly -and $changed.Count -gt 0) {
    [Console]::Error.WriteLine("normalize-text: $($changed.Count) file(s) need normalizing (run scripts/normalize-text.ps1):`n  $($changed -join "`n  ")")
}
if ($invalid.Count -gt 0 -or $blocked.Count -gt 0 -or ($CheckOnly -and $changed.Count -gt 0)) { exit 1 }
if ($changed.Count -eq 0) {
    Write-Host 'normalize-text: all files are normalized.'
}
else {
    Write-Host "normalize-text: rewrote $($changed.Count) file(s)."
}
