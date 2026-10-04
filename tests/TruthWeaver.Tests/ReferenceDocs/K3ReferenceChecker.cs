namespace TruthWeaver.Tests.ReferenceDocs;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Verifies the Markdown reference under <c>docs/strong-k3</c> against the independent <see cref="K3Oracle"/>, so a documented
/// table, canonical form or link that is wrong fails the build with a <c>file:line</c> message.
/// </summary>
/// <remarks>
/// <para>
/// Verified content is announced by a marker comment on its own line, directly above the table or fenced block it checks
/// (blank lines in between are fine). <c>OP</c> is an inventory name, case-insensitive; <c>key=value</c> arguments follow.
/// </para>
/// <list type="bullet">
/// <item>
/// <c>&lt;!-- k3:truth OP [param=value ...] --&gt;</c> then a table whose last column is the result and whose other columns are
/// operands (<c>T</c>, <c>F</c>, <c>U</c>, or the full word, backticks allowed). Every one of the 3^n assignments must appear
/// once and match the oracle. A parameter that is not an integer or a truth value (<c>unknownAs=True</c>,
/// <c>policy=UnknownAsFalse</c>) is named after the operation's parameter.
/// </item>
/// <item>
/// <c>&lt;!-- k3:eval OP n=N [param=value ...] --&gt;</c> then a three-column table: definitely-true count, possibly-true count,
/// result. Every pair <c>0 &lt;= d &lt;= p &lt;= N</c> must appear once and match the oracle on an operand list of d
/// <c>True</c>, p-d <c>Unknown</c> and N-p <c>False</c>.
/// </item>
/// <item>
/// <c>&lt;!-- k3:canonical OP vars=a,b --&gt;</c> (fixed operands) or <c>n=2..4</c> (variadic; <c>...</c> splices every operand)
/// then a fenced block holding one function-call expression (see <see cref="K3Expression"/>). It must equal the oracle for
/// every assignment, every operand count and every valid parameter value; the operation's parameter names are free
/// identifiers in the expression.
/// </item>
/// </list>
/// <para>
/// A file in a category directory other than <c>README.md</c> is an operation document: it must be in the
/// inventory with the matching category directory and Kind, carry every required section non-empty, use only the known
/// section names, and its Truth table, Evaluation table and Canonical form sections must each contain the marker that
/// verifies them. Section names match without regard to case. Relative links, including
/// heading anchors in Markdown targets, must resolve everywhere. Code spans and fenced blocks are not scanned for links.
/// </para>
/// <para>Failures are returned, not thrown, so the checker can be shown to fail on deliberately wrong fixtures.</para>
/// </remarks>
internal static partial class K3ReferenceChecker
{
    private const string ReferenceRoot = "docs/strong-k3";

    private const string OperationsIndexPath = "docs/strong-k3/specification/operations.md";

    private static readonly string[] RequiredSections =
    [
        "Name",
        "Classification",
        "Kind",
        "Arity",
        "Input domain",
        "Output domain",
        "Definition",
        "Syntax",
        "Aliases",
        "Formal semantics",
    ];

    // Sections that appear only where they apply. "Implementation notes" is the previous name of "Evaluation behavior".
    private static readonly string[] OptionalSections =
    [
        "Formula",
        "Truth table",
        "Evaluation table",
        "Canonical form",
        "Equivalent forms",
        "Examples",
        "Edge cases",
        "Mermaid diagram",
        "Evaluation behavior",
        "Implementation notes",
        "Related operations",
    ];

    // The category label each directory's documents must declare (docs/strong-k3/specification/operations.md).
    private static readonly Dictionary<string, string> CategoryLabels = new(StringComparer.Ordinal)
    {
        ["gates"] = "Gates / Operators",
        ["derived"] = "Derived Logical Operations",
        ["cardinality"] = "Cardinality Functions",
        ["functions"] = "Functions",
        ["result-transformations"] = "Result Transformations",
    };

    /// <summary>Checks every Markdown file under <c>docs/strong-k3</c> in the repository at <paramref name="repositoryRoot"/>.</summary>
    /// <param name="repositoryRoot">The directory holding <c>TruthWeaver.slnx</c>.</param>
    /// <returns>One message per failure; empty when the reference is sound.</returns>
    internal static IReadOnlyList<string> CheckTree(string repositoryRoot)
    {
        string referenceDirectory = Path.Combine(repositoryRoot, "docs", "strong-k3");
        if (!Directory.Exists(referenceDirectory))
        {
            return [$"{ReferenceRoot}: the reference directory does not exist"];
        }

        bool Exists(string path)
        {
            string full = Path.Combine(repositoryRoot, path);
            return File.Exists(full) || Directory.Exists(full);
        }

        string? Read(string path)
        {
            string full = Path.Combine(repositoryRoot, path);
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }

        List<string> failures = [];
        foreach (string file in Directory.EnumerateFiles(referenceDirectory, "*.md", SearchOption.AllDirectories).Order())
        {
            string relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
            failures.AddRange(Check(relative, File.ReadAllText(file), Exists, Read));
        }

        failures.AddRange(CheckOperationsIndex(Read(OperationsIndexPath)));
        return failures;
    }

    /// <summary>Checks that the operations index links the document of every inventory Operation.</summary>
    /// <param name="markdown">The text of <c>specification/operations.md</c>, or <see langword="null"/> when it does not exist.</param>
    /// <returns>One message per Operation that the index does not link; empty when the index is complete.</returns>
    internal static IReadOnlyList<string> CheckOperationsIndex(string? markdown)
    {
        if (markdown is null)
        {
            return [$"{OperationsIndexPath}: the operations index does not exist"];
        }

        return
        [
            .. K3Operation
                .Inventory.Values.Where(operation =>
                    !markdown.Contains(
                        $"](../{operation.Directory}/{operation.Name.ToLowerInvariant()}.md)",
                        StringComparison.Ordinal
                    )
                )
                .Select(operation =>
                    $"{OperationsIndexPath}: the inventory Operation '{operation.Name}' has no link to ../{operation.Directory}/{operation.Name.ToLowerInvariant()}.md"
                ),
        ];
    }

    /// <summary>Checks one Markdown document.</summary>
    /// <param name="path">The document's repository-relative path, with <c>/</c> separators; used to locate failures.</param>
    /// <param name="markdown">The document text.</param>
    /// <param name="exists">Whether a repository-relative path names an existing file or directory.</param>
    /// <param name="read">Reads a repository-relative Markdown file, or returns <see langword="null"/> when it does not exist.</param>
    /// <returns>One <c>path:line: message</c> per failure; empty when the document is sound.</returns>
    internal static IReadOnlyList<string> Check(
        string path,
        string markdown,
        Func<string, bool> exists,
        Func<string, string?> read
    )
    {
        List<string> failures = [];
        string[] lines = [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];
        bool[] inFence = FenceMap(lines);

        void Fail(int line, string message) => failures.Add($"{path}:{line}: {message}");

        CheckLinks(path, lines, inFence, exists, read, Fail);
        CheckMarkers(lines, inFence, Fail);
        CheckOperationDocument(path, lines, inFence, Fail);
        return failures;
    }

    /// <summary>Resolves a relative link against a directory; <see langword="null"/> when it climbs out of the repository.</summary>
    internal static string? Resolve(string directory, string relative)
    {
        List<string> segments = [.. directory.Split('/', StringSplitOptions.RemoveEmptyEntries)];
        foreach (string segment in relative.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return null;
                }

                segments.RemoveAt(segments.Count - 1);
            }
            else if (segment != ".")
            {
                segments.Add(segment);
            }
        }

        return string.Join('/', segments);
    }

    /// <summary>Marks every line that is inside (or is a delimiter of) a fenced code block.</summary>
    internal static bool[] FenceMap(string[] lines)
    {
        bool[] map = new bool[lines.Length];
        bool open = false;
        for (int i = 0; i < lines.Length; i++)
        {
            bool delimiter = lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal);
            map[i] = open || delimiter;
            if (delimiter)
            {
                open = !open;
            }
        }

        return map;
    }

    private static void CheckLinks(
        string path,
        string[] lines,
        bool[] inFence,
        Func<string, bool> exists,
        Func<string, string?> read,
        Action<int, string> fail
    )
    {
        string directory = path.Contains('/', StringComparison.Ordinal) ? path[..path.LastIndexOf('/')] : string.Empty;
        for (int i = 0; i < lines.Length; i++)
        {
            if (inFence[i])
            {
                continue;
            }

            string text = CodeSpanRegex().Replace(lines[i], static m => new string(' ', m.Length));
            foreach (Match match in LinkRegex().Matches(text))
            {
                string target = Uri.UnescapeDataString(match.Groups["target"].Value);
                if (SchemeRegex().IsMatch(target))
                {
                    continue;
                }

                int hash = target.IndexOf('#', StringComparison.Ordinal);
                string filePart = hash < 0 ? target : target[..hash];
                string? anchor = hash < 0 ? null : target[(hash + 1)..];

                string resolved = filePart.Length == 0 ? path : Resolve(directory, filePart) ?? string.Empty;
                if (resolved.Length == 0 || !exists(resolved))
                {
                    fail(
                        i + 1,
                        $"broken link '{target}' (resolves to '{(resolved.Length == 0 ? "outside the repository" : resolved)}')"
                    );
                    continue;
                }

                if (anchor is { Length: > 0 } && resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    string? targetText = resolved == path ? string.Join('\n', lines) : read(resolved);
                    if (targetText is not null && !Anchors(targetText).Contains(anchor))
                    {
                        fail(i + 1, $"broken link '{target}': '{resolved}' has no heading with anchor '#{anchor}'");
                    }
                }
            }
        }
    }

    /// <summary>The GitHub heading anchors of a document: lower-cased, punctuation dropped, spaces to hyphens, duplicates numbered.</summary>
    private static HashSet<string> Anchors(string markdown)
    {
        string[] lines = [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];
        bool[] inFence = FenceMap(lines);
        HashSet<string> anchors = new(StringComparer.Ordinal);
        Dictionary<string, int> seen = new(StringComparer.Ordinal);
        for (int i = 0; i < lines.Length; i++)
        {
            Match heading = inFence[i] ? Match.Empty : HeadingRegex().Match(lines[i]);
            if (!heading.Success)
            {
                continue;
            }

            StringBuilder slug = new();
            foreach (char c in heading.Groups["title"].Value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c) || c is '-' or '_')
                {
                    slug.Append(c);
                }
                else if (c == ' ')
                {
                    slug.Append('-');
                }
            }

            string baseSlug = slug.ToString();
            int count = seen.GetValueOrDefault(baseSlug);
            seen[baseSlug] = count + 1;
            anchors.Add(count == 0 ? baseSlug : $"{baseSlug}-{count}");
        }

        return anchors;
    }

    private static void CheckMarkers(string[] lines, bool[] inFence, Action<int, string> fail)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            Match marker = inFence[i] ? Match.Empty : MarkerRegex().Match(lines[i]);
            if (!marker.Success)
            {
                continue;
            }

            string kind = marker.Groups["kind"].Value;
            string[] arguments = marker.Groups["args"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int line = i + 1;
            try
            {
                if (
                    arguments.Length == 0
                    || !K3Operation.Inventory.TryGetValue(arguments[0].ToUpperInvariant(), out K3Operation? operation)
                )
                {
                    fail(line, $"k3:{kind} marker names no inventory operation");
                    continue;
                }

                Dictionary<string, string> options = ParseOptions(arguments.Skip(1));
                switch (kind)
                {
                    case "truth":
                        CheckTruthTable(lines, i, operation, options, fail);
                        break;
                    case "eval":
                        CheckEvaluationTable(lines, i, operation, options, fail);
                        break;
                    case "canonical":
                        CheckCanonicalForm(lines, i, operation, options, fail);
                        break;
                    default:
                        fail(line, $"unknown marker k3:{kind} (expected truth, eval or canonical)");
                        break;
                }
            }
            catch (FormatException ex)
            {
                fail(line, $"k3:{kind} {arguments.FirstOrDefault()}: {ex.Message}");
            }
        }
    }

    private static Dictionary<string, string> ParseOptions(IEnumerable<string> arguments)
    {
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        foreach (string argument in arguments)
        {
            int equals = argument.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0)
            {
                throw new FormatException($"argument '{argument}' is not key=value");
            }

            options[argument[..equals]] = argument[(equals + 1)..];
        }

        return options;
    }

    /// <summary>Reads the table after a marker: the 1-based line of each data row and its trimmed cells.</summary>
    private static List<(int Line, string[] Cells)> ReadTable(string[] lines, int markerIndex)
    {
        int i = markerIndex + 1;
        while (i < lines.Length && lines[i].Length == 0)
        {
            i++;
        }

        List<(int, string[])> rows = [];
        for (; i < lines.Length && lines[i].TrimStart().StartsWith('|'); i++)
        {
            string[] cells = [.. lines[i].Trim().Trim('|').Split('|').Select(cell => cell.Trim().Trim('`').Trim())];
            rows.Add((i + 1, cells));
        }

        // Row 0 is the header and row 1 the separator.
        if (rows.Count < 3)
        {
            throw new FormatException("the marker is not followed by a table with at least one row");
        }

        return [.. rows.Skip(2)];
    }

    private static string[] RequireParameters(K3Operation operation, Dictionary<string, string> options)
    {
        return
        [
            .. operation.Parameters.Select(name =>
                options.TryGetValue(name, out string? value)
                    ? value
                    : throw new FormatException($"missing parameter {name}=...")
            ),
        ];
    }

    private static void CheckTruthTable(
        string[] lines,
        int markerIndex,
        K3Operation operation,
        Dictionary<string, string> options,
        Action<int, string> fail
    )
    {
        string[] parameters = RequireParameters(operation, options);
        List<(int Line, string[] Cells)> rows = ReadTable(lines, markerIndex);
        int arity = rows[0].Cells.Length - 1;
        if (!operation.AcceptsOperandCount(arity))
        {
            throw new FormatException($"{operation.Name} does not accept {arity} operand(s)");
        }

        HashSet<string> covered = [];
        foreach ((int line, string[] cells) in rows)
        {
            if (cells.Length != arity + 1 || !cells.All(cell => K3Operation.TryParseTruth(cell, out _) || IsOutcome(cell)))
            {
                fail(line, $"malformed truth-table row '| {string.Join(" | ", cells)} |'");
                continue;
            }

            TruthValue[] operands = [.. cells.Take(arity).Select(ParseTruth)];
            string expected = operation.Evaluate(parameters, operands);
            string actual = NormaliseResult(cells[arity]);
            if (!covered.Add(string.Join(",", operands)))
            {
                fail(line, $"duplicate row for ({string.Join(", ", operands)})");
            }

            if (actual != expected)
            {
                fail(
                    line,
                    $"{operation.Name}({string.Join(", ", operands)}) is {expected} in the oracle but the row '| {string.Join(" | ", cells)} |' says {actual}"
                );
            }
        }

        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            if (!covered.Contains(string.Join(",", assignment)))
            {
                fail(rows[0].Line, $"missing row for ({string.Join(", ", assignment)})");
            }
        }
    }

    private static void CheckEvaluationTable(
        string[] lines,
        int markerIndex,
        K3Operation operation,
        Dictionary<string, string> options,
        Action<int, string> fail
    )
    {
        string[] parameters = RequireParameters(operation, options);
        int n = options.TryGetValue("n", out string? nText)
            ? int.Parse(nText, CultureInfo.InvariantCulture)
            : throw new FormatException("missing n=...");
        if (!operation.AcceptsOperandCount(n))
        {
            throw new FormatException($"{operation.Name} does not accept {n} operand(s)");
        }

        List<(int Line, string[] Cells)> rows = ReadTable(lines, markerIndex);
        HashSet<(int, int)> covered = [];
        foreach ((int line, string[] cells) in rows)
        {
            if (
                cells.Length != 3
                || !int.TryParse(cells[0], NumberStyles.None, CultureInfo.InvariantCulture, out int definite)
                || !int.TryParse(cells[1], NumberStyles.None, CultureInfo.InvariantCulture, out int possible)
                || !K3Operation.TryParseTruth(cells[2], out _)
                || definite > possible
                || possible > n
            )
            {
                fail(
                    line,
                    $"malformed evaluation-table row '| {string.Join(" | ", cells)} |' (need d <= p <= {n} and a result)"
                );
                continue;
            }

            if (!covered.Add((definite, possible)))
            {
                fail(line, $"duplicate row for ({definite}, {possible})");
            }

            TruthValue[] operands =
            [
                .. Enumerable.Repeat(TruthValue.True, definite),
                .. Enumerable.Repeat(TruthValue.Unknown, possible - definite),
                .. Enumerable.Repeat(TruthValue.False, n - possible),
            ];
            string expected = operation.Evaluate(parameters, operands);
            string actual = NormaliseResult(cells[2]);
            if (actual != expected)
            {
                fail(
                    line,
                    $"{operation.Name} with {definite} definitely true and {possible} possibly true of {n} is {expected} in the oracle but the row '| {string.Join(" | ", cells)} |' says {actual}"
                );
            }
        }

        for (int d = 0; d <= n; d++)
        {
            for (int p = d; p <= n; p++)
            {
                if (!covered.Contains((d, p)))
                {
                    fail(rows[0].Line, $"missing row for ({d}, {p})");
                }
            }
        }
    }

    private static void CheckCanonicalForm(
        string[] lines,
        int markerIndex,
        K3Operation operation,
        Dictionary<string, string> options,
        Action<int, string> fail
    )
    {
        int i = markerIndex + 1;
        while (i < lines.Length && lines[i].Length == 0)
        {
            i++;
        }

        if (i >= lines.Length || !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
        {
            throw new FormatException("the marker is not followed by a fenced block");
        }

        int close = Array.FindIndex(lines, i + 1, line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));
        string form = string.Join(' ', lines.Skip(i + 1).Take((close < 0 ? lines.Length : close) - i - 1)).Trim();

        string[]? variables = options.TryGetValue("vars", out string? vars) ? vars.Split(',') : null;
        (int low, int high) = variables is not null ? (variables.Length, variables.Length) : ParseRange(options);

        for (int n = low; n <= high; n++)
        {
            string[] names = variables ?? [.. Enumerable.Range(0, n).Select(index => ((char)('a' + index)).ToString())];
            foreach (string[] combination in operation.ParameterCombinations(n))
            {
                foreach (TruthValue[] assignment in K3Oracle.Assignments(n))
                {
                    Dictionary<string, K3Expression.Value> environment = new(StringComparer.Ordinal);
                    for (int v = 0; v < n; v++)
                    {
                        environment[names[v]] = new K3Expression.Value(null, assignment[v]);
                    }

                    for (int p = 0; p < operation.Parameters.Length; p++)
                    {
                        environment[operation.Parameters[p]] = K3Expression.Value.Parse(combination[p]);
                    }

                    TruthValue actual = K3Expression.Evaluate(
                        form,
                        environment,
                        [.. assignment.Select(value => new K3Expression.Value(null, value))]
                    );
                    string expected = operation.Evaluate(combination, assignment);
                    if (actual.ToString() != expected)
                    {
                        string parameterText =
                            combination.Length == 0
                                ? string.Empty
                                : $" with {string.Join(", ", operation.Parameters.Zip(combination, (name, value) => $"{name}={value}"))}";
                        fail(
                            markerIndex + 1,
                            $"canonical form '{form}' of {operation.Name} gives {actual} for ({string.Join(", ", assignment)}){parameterText} but the oracle gives {expected}"
                        );
                        return;
                    }
                }
            }
        }
    }

    private static (int Low, int High) ParseRange(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("n", out string? range))
        {
            throw new FormatException("need vars=a,b (fixed operands) or n=2..4 (variadic)");
        }

        string[] parts = range.Split("..", StringSplitOptions.None);
        int low = int.Parse(parts[0], CultureInfo.InvariantCulture);
        return (low, parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : low);
    }

    private static TruthValue ParseTruth(string cell)
    {
        return K3Operation.TryParseTruth(cell, out TruthValue value)
            ? value
            : throw new FormatException($"'{cell}' is not a truth value");
    }

    private static bool IsOutcome(string cell)
    {
        return Enum.TryParse(cell, ignoreCase: true, out CollapseOutcome _);
    }

    /// <summary>Normalises a result cell to the name the oracle returns (<c>True</c>, <c>False</c>, <c>Unknown</c>, a collapse outcome).</summary>
    private static string NormaliseResult(string cell)
    {
        if (K3Operation.TryParseTruth(cell, out TruthValue value))
        {
            return value.ToString();
        }

        return Enum.TryParse(cell, ignoreCase: true, out CollapseOutcome outcome) ? outcome.ToString() : cell;
    }

    private static void CheckOperationDocument(string path, string[] lines, bool[] inFence, Action<int, string> fail)
    {
        string[] segments = path.Split('/');

        // Only docs/strong-k3/<category>/<name>.md, other than the directory's README.md, is an operation document.
        if (
            segments.Length != 4
            || !path.StartsWith(ReferenceRoot + "/", StringComparison.Ordinal)
            || !CategoryLabels.ContainsKey(segments[2])
            || segments[3].Equals("README.md", StringComparison.OrdinalIgnoreCase)
        )
        {
            return;
        }

        string directory = segments[2];
        string stem = Path.GetFileNameWithoutExtension(segments[3]);
        K3Operation.Inventory.TryGetValue(stem.ToUpperInvariant(), out K3Operation? operation);
        if (operation is null)
        {
            fail(1, $"'{stem}' is not in the inventory (docs/strong-k3/specification/operations.md)");
        }
        else if (operation.Directory != directory)
        {
            fail(1, $"{operation.Name} belongs in '{operation.Directory}/' per the inventory, not '{directory}/'");
        }

        Dictionary<string, (int Start, int End)> sections = Sections(lines, inFence);

        foreach (string required in RequiredSections)
        {
            if (!sections.TryGetValue(required, out (int Start, int End) range))
            {
                fail(1, $"missing required section '## {required}'");
            }
            else if (SectionLines(lines, range).Count == 0)
            {
                fail(range.Start + 1, $"required section '## {required}' is empty");
            }
        }

        foreach ((string title, (int Start, int End) range) in sections)
        {
            if (
                !RequiredSections.Contains(title, StringComparer.OrdinalIgnoreCase)
                && !OptionalSections.Contains(title, StringComparer.OrdinalIgnoreCase)
            )
            {
                fail(range.Start + 1, $"unknown section '## {title}'");
            }
        }

        if (
            sections.TryGetValue("Kind", out (int Start, int End) kindRange)
            && SectionLines(lines, kindRange) is [var kindLine, ..]
        )
        {
            string kind = kindLine.Text.Trim('*', '`', ' ', '-');
            string first = kind.Split(' ', ',', '.')[0];
            if (first is not ("Primitive" or "Derived"))
            {
                fail(kindLine.Line, $"Kind must start with Primitive or Derived, not '{kind}'");
            }
            else if (operation is not null && first != operation.Kind)
            {
                fail(kindLine.Line, $"Kind is {first} but the approved inventory says {operation.Name} is {operation.Kind}");
            }
        }

        if (sections.TryGetValue("Classification", out (int Start, int End) classRange))
        {
            CheckCategory(lines, classRange, CategoryLabels[directory], fail);
        }

        if (
            sections.ContainsKey("Truth table") && sections.TryGetValue("Evaluation table", out (int Start, int End) evaluation)
        )
        {
            fail(evaluation.Start + 1, "Truth table and Evaluation table are mutually exclusive");
        }

        RequireMarker(lines, sections, "Truth table", "k3:truth", fail);
        RequireMarker(lines, sections, "Evaluation table", "k3:eval", fail);
        RequireMarker(lines, sections, "Canonical form", "k3:canonical", fail);
    }

    private static void CheckCategory(string[] lines, (int Start, int End) range, string expected, Action<int, string> fail)
    {
        List<(int Line, string Text)> categories =
        [
            .. SectionLines(lines, range)
                .Where(l => l.Text.TrimStart('-', '*', ' ').StartsWith("Category:", StringComparison.OrdinalIgnoreCase)),
        ];
        if (categories.Count != 1)
        {
            fail(range.Start + 1, $"Classification must contain exactly one 'Category:' line (found {categories.Count})");
            return;
        }

        string declared = categories[0].Text.TrimStart('-', '*', ' ')["Category:".Length..].Trim('*', '`', ' ');
        if (declared != expected)
        {
            fail(categories[0].Line, $"Category is '{declared}' but documents in this directory are '{expected}'");
        }
    }

    private static void RequireMarker(
        string[] lines,
        Dictionary<string, (int Start, int End)> sections,
        string section,
        string marker,
        Action<int, string> fail
    )
    {
        if (
            sections.TryGetValue(section, out (int Start, int End) range)
            && !SectionLines(lines, range).Any(l => l.Text.Contains("<!-- " + marker, StringComparison.Ordinal))
        )
        {
            fail(
                range.Start + 1,
                $"section '## {section}' has no <!-- {marker} ... --> marker, so its content would go unchecked"
            );
        }
    }

    /// <summary>The level-2 sections, by title, as 0-based start (the heading line) and exclusive end indexes.</summary>
    private static Dictionary<string, (int Start, int End)> Sections(string[] lines, bool[] inFence)
    {
        Dictionary<string, (int, int)> sections = new(StringComparer.OrdinalIgnoreCase);
        string? current = null;
        int start = 0;
        for (int i = 0; i <= lines.Length; i++)
        {
            Match heading = i < lines.Length && !inFence[i] ? SecondLevelRegex().Match(lines[i]) : Match.Empty;
            if (i == lines.Length || heading.Success)
            {
                if (current is not null)
                {
                    sections[current] = (start, i);
                }

                if (heading.Success)
                {
                    current = heading.Groups["title"].Value.Trim();
                    start = i;
                }
            }
        }

        return sections;
    }

    /// <summary>The non-blank lines of a section after its heading, with 1-based line numbers.</summary>
    private static List<(int Line, string Text)> SectionLines(string[] lines, (int Start, int End) range)
    {
        return
        [
            .. Enumerable
                .Range(range.Start + 1, range.End - range.Start - 1)
                .Where(index => lines[index].Trim().Length > 0)
                .Select(index => (index + 1, lines[index])),
        ];
    }

    [GeneratedRegex(@"^\s*<!--\s*k3:(?<kind>\w+)\s*(?<args>.*?)\s*-->\s*$")]
    private static partial Regex MarkerRegex();

    [GeneratedRegex(@"\[[^\]]*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"`[^`]*`")]
    private static partial Regex CodeSpanRegex();

    [GeneratedRegex(@"^(?:[A-Za-z][A-Za-z0-9+.\-]*:|//)")]
    private static partial Regex SchemeRegex();

    [GeneratedRegex(@"^#{1,6}\s+(?<title>.*?)\s*#*\s*$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^##\s+(?<title>.*?)\s*#*\s*$")]
    private static partial Regex SecondLevelRegex();
}
