namespace TruthWeaver.Tests.EditorGrammar;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// A small TextMate tokenizer for the grammar in <c>editors/vscode</c>. It reads a <c>.tmLanguage.json</c> file and
/// assigns a scope string to every character of a text, the way an editor would, so a test can check the grammar
/// without Node or an Oniguruma binding. It supports the subset that the grammar uses: <c>match</c> rules,
/// <c>begin</c>/<c>end</c> rules, <c>patterns</c>, <c>#name</c> includes, and <c>name</c>, <c>captures</c>,
/// <c>beginCaptures</c> and <c>endCaptures</c>. The grammar regular expressions use only syntax that .NET and
/// Oniguruma share (inline <c>(?i)</c>, <c>\b</c>, <c>\p{L}</c>, lookarounds), so .NET evaluates them directly.
/// </summary>
internal sealed class TextMateGrammar
{
    private readonly Dictionary<string, Rule> repository = [with(StringComparer.Ordinal)];
    private readonly List<Rule> rootPatterns = [];

    private TextMateGrammar(string scopeName)
    {
        this.ScopeName = scopeName;
    }

    /// <summary>Gets the root scope name of the grammar, which every character carries.</summary>
    public string ScopeName { get; }

    /// <summary>Loads a grammar from the text of a <c>.tmLanguage.json</c> file.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The grammar.</returns>
    public static TextMateGrammar Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        TextMateGrammar grammar = new(root.GetProperty("scopeName").GetString()!);

        if (root.TryGetProperty("repository", out JsonElement repo))
        {
            using JsonElement.ObjectEnumerator entries = repo.EnumerateObject();
            foreach (JsonProperty entry in entries)
            {
                grammar.repository[entry.Name] = Rule.FromJson(entry.Value);
            }
        }

        using JsonElement.ArrayEnumerator rootEntries = root.GetProperty("patterns").EnumerateArray();
        foreach (JsonElement pattern in rootEntries)
        {
            grammar.rootPatterns.Add(Rule.FromJson(pattern));
        }

        return grammar;
    }

    /// <summary>Assigns a scope string to each character of <paramref name="text"/>.</summary>
    /// <param name="text">The text to tokenize. Lines end at <c>\n</c>, and an open <c>begin</c> rule continues on the next line.</param>
    /// <returns>One space-separated scope string for each character. A character that no rule claims has only the root scope.</returns>
    public string[] Tokenize(string text)
    {
        string[] scopes = new string[text.Length];
        List<Rule> stack = [];
        int lineStart = 0;
        while (lineStart <= text.Length)
        {
            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            string line = text[lineStart..lineEnd].TrimEnd('\r');
            this.TokenizeLine(line, lineStart, scopes, stack);

            // The line break and any carriage return belong to whatever rule is open.
            for (int i = lineStart + line.Length; i < lineEnd + 1 && i < text.Length; i++)
            {
                scopes[i] = this.ScopeOf(stack);
            }

            lineStart = lineEnd + 1;
        }

        return scopes;
    }

    private static void Paint(string[] scopes, int offset, Match match, string scope, Dictionary<int, string> captures)
    {
        Fill(scopes, offset + match.Index, offset + match.Index + match.Length, scope);
        foreach ((int group, string name) in captures)
        {
            Group capture = match.Groups[group];
            if (capture.Success)
            {
                Fill(scopes, offset + capture.Index, offset + capture.Index + capture.Length, Append(scope, name));
            }
        }
    }

    private static void Fill(string[] scopes, int start, int end, string scope)
    {
        for (int i = start; i < end; i++)
        {
            scopes[i] = scope;
        }
    }

    private static string Append(string scope, string? name)
    {
        return name is null ? scope : scope + " " + name;
    }

    private void TokenizeLine(string line, int offset, string[] scopes, List<Rule> stack)
    {
        int position = 0;
        while (position < line.Length)
        {
            Rule? open = stack.Count == 0 ? null : stack[^1];
            string current = this.ScopeOf(stack);

            // The end pattern of the open rule is tried first, so it wins a tie against a nested pattern.
            Match? best = null;
            Rule? bestRule = null;
            if (open?.End is { } end)
            {
                Match endMatch = end.Match(line, position);
                if (endMatch.Success)
                {
                    best = endMatch;
                }
            }

            foreach (Rule candidate in this.Expand(open is null ? this.rootPatterns : open.Patterns))
            {
                Match match = (candidate.Match ?? candidate.Begin)!.Match(line, position);
                if (match.Success && (best is null || match.Index < best.Index))
                {
                    best = match;
                    bestRule = candidate;
                }
            }

            if (best is null)
            {
                break;
            }

            Fill(scopes, offset + position, offset + best.Index, current);

            if (bestRule is null)
            {
                // The end of the open rule: its name still applies, then the rule closes.
                Paint(scopes, offset, best, current, open!.EndCaptures);
                stack.RemoveAt(stack.Count - 1);
            }
            else if (bestRule.Match is not null)
            {
                Paint(scopes, offset, best, Append(current, bestRule.Name), bestRule.Captures);
            }
            else
            {
                stack.Add(bestRule);
                Paint(scopes, offset, best, this.ScopeOf(stack), bestRule.BeginCaptures);
            }

            int next = best.Index + best.Length;
            if (next <= position && best.Length == 0)
            {
                throw new InvalidOperationException($"The grammar matched an empty string at column {position} of '{line}'.");
            }

            position = Math.Max(next, position);
        }

        Fill(scopes, offset + position, offset + line.Length, this.ScopeOf(stack));
    }

    private string ScopeOf(List<Rule> stack)
    {
        string scope = this.ScopeName;
        foreach (Rule frame in stack)
        {
            scope = Append(scope, frame.Name);
        }

        return scope;
    }

    /// <summary>Resolves <c>include</c> and container rules to the rules that have a <c>match</c> or a <c>begin</c>.</summary>
    private IEnumerable<Rule> Expand(IEnumerable<Rule> rules)
    {
        foreach (Rule rule in rules)
        {
            if (rule.Include is { } include)
            {
                if (!include.StartsWith('#') || !this.repository.TryGetValue(include[1..], out Rule? target))
                {
                    throw new InvalidOperationException($"The grammar includes '{include}', which is not in its repository.");
                }

                foreach (Rule expanded in this.Expand([target]))
                {
                    yield return expanded;
                }
            }
            else if (rule.Match is null && rule.Begin is null)
            {
                foreach (Rule expanded in this.Expand(rule.Patterns))
                {
                    yield return expanded;
                }
            }
            else
            {
                yield return rule;
            }
        }
    }

    private sealed class Rule
    {
        public string? Name { get; private init; }

        public string? Include { get; private init; }

        public Regex? Match { get; private init; }

        public Regex? Begin { get; private init; }

        public Regex? End { get; private init; }

        public List<Rule> Patterns { get; } = [];

        public Dictionary<int, string> Captures { get; private init; } = [];

        public Dictionary<int, string> BeginCaptures { get; private init; } = [];

        public Dictionary<int, string> EndCaptures { get; private init; } = [];

        public static Rule FromJson(JsonElement element)
        {
            Rule rule = new()
            {
                Name = Text(element, "name"),
                Include = Text(element, "include"),
                Match = Pattern(element, "match"),
                Begin = Pattern(element, "begin"),
                End = Pattern(element, "end"),
                Captures = CaptureNames(element, "captures"),
                BeginCaptures = CaptureNames(element, "beginCaptures"),
                EndCaptures = CaptureNames(element, "endCaptures"),
            };

            // A match rule's captures may be written as "captures"; a begin/end rule may use "captures" for both ends.
            if (rule.Begin is not null && rule.Captures.Count > 0)
            {
                foreach ((int group, string name) in rule.Captures)
                {
                    rule.BeginCaptures.TryAdd(group, name);
                    rule.EndCaptures.TryAdd(group, name);
                }
            }

            if (element.TryGetProperty("patterns", out JsonElement patterns))
            {
                using JsonElement.ArrayEnumerator entries = patterns.EnumerateArray();
                foreach (JsonElement pattern in entries)
                {
                    rule.Patterns.Add(FromJson(pattern));
                }
            }

            return rule;
        }

        private static string? Text(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out JsonElement value) ? value.GetString() : null;
        }

        private static Regex? Pattern(JsonElement element, string property)
        {
            return Text(element, property) is { } pattern ? new Regex(pattern, RegexOptions.CultureInvariant) : null;
        }

        private static Dictionary<int, string> CaptureNames(JsonElement element, string property)
        {
            Dictionary<int, string> names = [];
            if (element.TryGetProperty(property, out JsonElement captures))
            {
                using JsonElement.ObjectEnumerator entries = captures.EnumerateObject();
                foreach (JsonProperty capture in entries)
                {
                    names[int.Parse(capture.Name, System.Globalization.CultureInfo.InvariantCulture)] = capture
                        .Value.GetProperty("name")
                        .GetString()!;
                }
            }

            return names;
        }
    }
}
