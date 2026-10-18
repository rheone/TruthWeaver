#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs

// Normalizes the text of documentation, source, script and data files.
//
// Applies these rules to the files that Git tracks or would track (not ignored), and skips every path that has a
// segment starting with "." (.claude, .scratch, .github, .tmp and so on):
//   - The file must be valid UTF-8. An invalid file is reported and the script exits with code 1.
//   - A UTF-8 byte order mark is removed.
//   - An em dash (U+2014) becomes " - ", the ellipsis character (U+2026) becomes "..." and the curly quotes
//     (U+2018, U+2019, U+201C, U+201D) become straight quotes. The curly-quote allowlist lists the files that keep them.
//   - An en dash (U+2013) becomes "-" and a no-break space (U+00A0) becomes a space.
//   - A hidden or garbled character (see the blocklist) is reported and the script exits with code 1.
//   - Every line ending becomes LF.
//   - Trailing spaces and tabs are removed, except the two-space hard break in Markdown. C# files keep their line
//     content, because dotnet format owns it and a trim could change a string literal.
//   - A non-empty file ends with exactly one LF.
// Test fixtures are left alone: tests read them for exact text and line/column spans.
//
// Usage: dotnet run scripts/normalize-text.cs -- [--check-only] [path ...]
//   --check-only  Rewrite nothing: list the files that need a change and exit with code 1.
//   path          Limit the run to some files (the pre-commit hook passes the staged files). Paths are relative to
//                 the repository root.
using System.Text;
using System.Text.RegularExpressions;
using TruthWeaver.Scripts;

try
{
    bool checkOnly = false;
    List<string> paths = [];
    foreach (string argument in args)
    {
        if (argument == "--check-only")
        {
            checkOnly = true;
        }
        else if (argument.StartsWith("--", StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync($"normalize-text: unknown option {argument}");
            return 1;
        }
        else
        {
            paths.Add(argument);
        }
    }

    // Paths are relative to the repository root, wherever the script starts.
    string root = await Repo.FindRootAsync();
    Directory.SetCurrentDirectory(root);

    // Extensions that hold prose, code, scripts or data.
    HashSet<string> extensions = new(
        [
            ".md",
            ".txt",
            ".cs",
            ".py",
            ".mjs",
            ".sh",
            ".yml",
            ".yaml",
            ".json",
            ".props",
            ".targets",
            ".csproj",
            ".slnx",
            ".xsd",
        ],
        StringComparer.OrdinalIgnoreCase
    );

    // Test data that tests read for exact text and spans, and a vendored third-party schema.
    TimeSpan timeout = TimeSpan.FromSeconds(10);
    Regex dataFixtures = new(
        @"(^|/)tests/.*/(Fixtures|Samples|RuleTests)/|(^|/)tests/.*/TestSupport/.*\.json$",
        RegexOptions.None,
        timeout
    );

    IEnumerable<string> candidates;
    if (paths.Count > 0)
    {
        candidates = paths;
    }
    else
    {
        // core.quotepath=off and NUL separators keep non-ASCII paths readable.
        string listing = await Repo.GitAsync(
            "-c",
            "core.quotepath=off",
            "ls-files",
            "--cached",
            "--others",
            "--exclude-standard",
            "-z"
        );
        candidates = listing.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    List<string> files =
    [
        .. candidates.Where(candidate =>
        {
            string normalized = candidate.Replace('\\', '/');
            bool isHidden = normalized.Split('/').Any(segment => segment.StartsWith('.'));
            return !isHidden
                && !dataFixtures.IsMatch(normalized)
                && extensions.Contains(Path.GetExtension(candidate))
                && File.Exists(candidate);
        }),
    ];

    // Never allowed: soft hyphen, zero-width and direction marks, line and paragraph separators, bidirectional
    // controls (they can reorder displayed code), the word joiner, a byte order mark inside a file and the replacement
    // character (a sign of broken encoding). Write one as a \u escape when code needs it as data.
    int[] blockedCodes =
    [
        0xAD,
        .. Enumerable.Range(0x200B, 5),
        0x2028,
        0x2029,
        .. Enumerable.Range(0x202A, 5),
        0x2060,
        .. Enumerable.Range(0x2066, 4),
        0xFEFF,
        0xFFFD,
    ];
    Regex blocklist = new("[" + string.Concat(blockedCodes.Select(code => (char)code)) + "]", RegexOptions.None, timeout);

    // Files where a curly quote is the expected text: the AsciiMath printer maps a straight double quote to U+201D.
    HashSet<string> curlyQuoteAllowlist = new(
        [
            "docs/equations.md",
            "docs/examples.md",
            "src/TruthWeaver/Printing/AsciiMathText.cs",
            "tests/TruthWeaver.Tests/ConstantStyleTests.cs",
            "tests/TruthWeaver.Tests/EquationAsciiMathTests.cs",
        ],
        StringComparer.Ordinal
    );

    // A dash that starts a line becomes a list-style hyphen and one that ends a line loses its trailing space,
    // so the replacement never adds a stray space or joins two lines.
    Regex dashAtStart = new("(?m)^([ \\t]*)\u2014[ \\t]*", RegexOptions.None, timeout);
    Regex dashAtEnd = new("(?m)[ \\t]*\u2014[ \\t]*$", RegexOptions.None, timeout);
    Regex dashInside = new("[ \\t]*\u2014[ \\t]*", RegexOptions.None, timeout);

    // Two trailing spaces after text are a Markdown hard break, so they stay (as exactly two spaces).
    Regex markdownTrailing = new(@"(?m)(?<=\S)[ \t]{2,}$|[ \t]+$", RegexOptions.None, timeout);
    Regex plainTrailing = new(@"(?m)[ \t]+$", RegexOptions.None, timeout);

    UTF8Encoding strictUtf8 = new(false, true);
    UTF8Encoding utf8NoBom = new(false);
    List<string> changed = [];
    List<string> invalid = [];
    List<string> blocked = [];

    foreach (string file in files)
    {
        string extension = Path.GetExtension(file).ToLowerInvariant();
        byte[] bytes = await File.ReadAllBytesAsync(file);
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        int skip = hasBom ? 3 : 0;
        string text;
        try
        {
            text = strictUtf8.GetString(bytes, skip, bytes.Length - skip);
        }
        catch (DecoderFallbackException)
        {
            invalid.Add(file);
            continue;
        }

        string result = dashAtStart.Replace(text, "$1- ");
        result = dashAtEnd.Replace(result, " -");
        result = dashInside.Replace(result, " - ");
        result = result.Replace("\u2026", "...", StringComparison.Ordinal);

        // Curly quotes become straight quotes, except in the files that document or test the AsciiMath printer,
        // which replaces a straight double quote with U+201D on purpose.
        if (!curlyQuoteAllowlist.Contains(file.Replace('\\', '/')))
        {
            result = result.Replace('\u2018', '\'').Replace('\u2019', '\'').Replace('\u201C', '"').Replace('\u201D', '"');
        }

        result = result.Replace('\u2013', '-').Replace('\u00A0', ' ');

        // Characters that hide or garble text cannot be fixed by a replacement. Report them with line and code point.
        foreach (Match match in blocklist.Matches(result))
        {
            int line = result.AsSpan(0, match.Index).Count('\n') + 1;
            blocked.Add($"{file}:{line}: U+{(int)match.Value[0]:X4}");
        }

        result = result.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

        if (extension == ".md")
        {
            string current = result;
            result = markdownTrailing.Replace(
                current,
                m =>
                    m.Index > 0
                    && m.Value.Length >= 2
                    && m.Value.All(character => character == ' ')
                    && !char.IsWhiteSpace(current[m.Index - 1])
                        ? "  "
                        : string.Empty
            );
        }
        else if (extension != ".cs")
        {
            result = plainTrailing.Replace(result, string.Empty);
        }

        if (result.Length > 0)
        {
            result = result.TrimEnd('\n') + "\n";
            if (result == "\n")
            {
                result = string.Empty;
            }
        }

        if (string.Equals(result, text, StringComparison.Ordinal) && !hasBom)
        {
            continue;
        }

        changed.Add(file);
        if (!checkOnly)
        {
            await File.WriteAllBytesAsync(file, utf8NoBom.GetBytes(result));
        }
    }

    if (invalid.Count > 0)
    {
        await Console.Error.WriteLineAsync(
            $"normalize-text: {invalid.Count} file(s) are not valid UTF-8 (fix the encoding by hand):\n  {string.Join("\n  ", invalid)}"
        );
    }

    if (blocked.Count > 0)
    {
        await Console.Error.WriteLineAsync(
            $"normalize-text: {blocked.Count} blocked character(s) (remove them, or write them as \\u escapes):\n  {string.Join("\n  ", blocked)}"
        );
    }

    if (checkOnly && changed.Count > 0)
    {
        await Console.Error.WriteLineAsync(
            $"normalize-text: {changed.Count} file(s) need normalizing (run scripts/normalize-text.cs):\n  {string.Join("\n  ", changed)}"
        );
    }

    if (invalid.Count > 0 || blocked.Count > 0 || (checkOnly && changed.Count > 0))
    {
        return 1;
    }

    await Console.Out.WriteLineAsync(
        changed.Count == 0 ? "normalize-text: all files are normalized." : $"normalize-text: rewrote {changed.Count} file(s)."
    );
    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}
