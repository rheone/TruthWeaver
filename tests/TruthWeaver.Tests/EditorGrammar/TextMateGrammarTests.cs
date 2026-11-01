namespace TruthWeaver.Tests.EditorGrammar;

using System.Text.Json;
using System.Text.RegularExpressions;
using TruthWeaver.Tests.DocExamples;

/// <summary>
/// Checks the TextMate grammar of <c>editors/vscode</c> against the rule text of the documentation and against
/// single constructs. The test runs the grammar with <see cref="TextMateGrammar"/>, so it needs no Node and no network.
/// </summary>
public sealed partial class TextMateGrammarTests
{
    private static readonly string EditorFolder = Path.Combine(DocExampleChecker.FindRepositoryRoot(), "editors", "vscode");

    private static readonly Lazy<TextMateGrammar> Grammar = new(() =>
        TextMateGrammar.Parse(File.ReadAllText(Path.Combine(EditorFolder, "syntaxes", "truthweaver.tmLanguage.json")))
    );

    /// <summary>
    /// Every rule text block of <c>docs/rule-text.md</c> and <c>docs/examples.md</c> gets a scope on every
    /// character that is not white space, so an editor leaves no part of a documented rule uncolored.
    /// </summary>
    [Theory]
    [InlineData("rule-text.md")]
    [InlineData("examples.md")]
    public void Tokenize_DocumentedRuleTextBlocks_LeaveNoUnscopedText_Test(string page)
    {
        string path = Path.Combine(DocExampleChecker.FindRepositoryRoot(), "docs", page);
        IReadOnlyList<string> blocks = RuleTextBlocks(File.ReadAllText(path));

        List<string> failures = [];
        foreach (string block in blocks)
        {
            failures.AddRange(Unscoped(block).Select(text => $"docs/{page}: '{text}' in '{block}'"));
        }

        Assert.NotEmpty(blocks);
        Assert.Empty(failures);
    }

    /// <summary>The documentation pages hold enough rule text blocks that the check above is not vacuous.</summary>
    [Fact]
    public void RuleTextBlocks_ExamplesPage_FindsTheWorkedRule_Test()
    {
        string path = Path.Combine(DocExampleChecker.FindRepositoryRoot(), "docs", "examples.md");

        IReadOnlyList<string> blocks = RuleTextBlocks(File.ReadAllText(path));

        Assert.Contains(
            blocks,
            block => block.StartsWith("hasTopping(topping: \"greenOlives\") AND", StringComparison.Ordinal)
        );
        Assert.True(blocks.Count >= 10, $"Found only {blocks.Count} rule text blocks.");
    }

    /// <summary>A character that the DSL does not read, such as <c>^</c>, stays unscoped, so the check can fail.</summary>
    [Fact]
    public void Tokenize_CharacterOutsideTheLanguage_StaysUnscoped_Test()
    {
        IReadOnlyList<string> unscoped = Unscoped("a ^ b");

        Assert.Equal(["^"], unscoped);
    }

    /// <summary>A construct gets the scope that themes color it by.</summary>
    [Theory]
    [InlineData("a AND b", "AND", "keyword.operator.logical.infix")]
    [InlineData("a and b", "and", "keyword.operator.logical.infix")]
    [InlineData("a Equivalent b", "Equivalent", "keyword.operator.logical.infix")]
    [InlineData("a IFF b", "IFF", "keyword.operator.logical.infix")]
    [InlineData("a NOR b", "NOR", "keyword.operator.logical.infix")]
    [InlineData("NOT a", "NOT", "keyword.operator.logical.not")]
    [InlineData("!a", "!", "keyword.operator.logical.not")]
    [InlineData("¬a", "¬", "keyword.operator.logical.not")]
    [InlineData("a && b", "&&", "keyword.operator.logical.symbol")]
    [InlineData("a || b", "||", "keyword.operator.logical.symbol")]
    [InlineData("a ?? b", "??", "keyword.operator.logical.symbol")]
    [InlineData("a ∧ b", "∧", "keyword.operator.logical.symbol")]
    [InlineData("a ⊕ b", "⊕", "keyword.operator.logical.symbol")]
    [InlineData("a ↔ b", "↔", "keyword.operator.logical.symbol")]
    [InlineData("a → b", "→", "keyword.operator.logical.symbol")]
    [InlineData("a ↓ b", "↓", "keyword.operator.logical.symbol")]
    [InlineData("a ? b : c", "?", "keyword.operator.ternary")]
    [InlineData("a ? b : c", ":", "keyword.operator.ternary")]
    [InlineData("T AND f", "T", "constant.language")]
    [InlineData("T AND f", "f", "constant.language")]
    [InlineData("True OR unknown", "unknown", "constant.language")]
    [InlineData("android", "android", "variable.other.term")]
    [InlineData("order", "order", "variable.other.term")]
    [InlineData("isDineIn XOR isTakeout", "isDineIn", "variable.other.term")]
    [InlineData("AtLeast(2, a, b, c)", "AtLeast", "support.function.operator")]
    [InlineData("PARITY (a, b)", "PARITY", "support.function.operator")]
    [InlineData("AtLeast(2, a, b, c)", "2", "constant.numeric")]
    [InlineData("BETWEEN(-1, 2.5, a)", "-1", "constant.numeric")]
    [InlineData("BETWEEN(-1, 2.5, a)", "2.5", "constant.numeric")]
    [InlineData("isknown(x)", "isknown", "support.function.operator")]
    [InlineData("hasCrust(crust: \"thin\")", "hasCrust", "entity.name.function.term")]
    [InlineData("hasCrust(crust: \"thin\")", "crust", "variable.parameter.argument")]
    [InlineData("hasCrust(crust: \"thin\")", "thin", "string.quoted.double")]
    [InlineData("hasCrust(ignoreCase: true)", "true", "constant.language")]
    [InlineData("has(toppings: [\"a\", \"b\"])", "toppings", "variable.parameter.argument")]
    [InlineData("has(toppings: [\"a\", 12])", "12", "constant.numeric")]
    [InlineData("has(toppings: [\"a\", \"b\"])", "b", "string.quoted.double")]
    [InlineData("total(limit: from(\"limits\", \"$.maxTotal\"))", "from", "support.function.variable-reference")]
    [InlineData("total(limit: from(\"limits\", \"$.maxTotal\"))", "$.maxTotal", "string.quoted.double")]
    [InlineData("has(topping: \"Chef's \\\"Special\\\"\")", "\\\"Special", "constant.character.escape")]
    [InlineData("has(topping: \"a\\pb\")", "\\p", "invalid.illegal.escape")]
    [InlineData("a // a note", "note", "comment.line.double-slash")]
    [InlineData("a /* a\nnote */ AND b", "note", "comment.block")]
    [InlineData("a AND /* a note */ b", "AND", "keyword.operator.logical.infix")]
    [InlineData("a AND (b OR c)", "(", "punctuation.section.group.begin")]
    [InlineData("a AND [b OR c]", "]", "punctuation.section.group.end")]
    [InlineData("a AND {b OR c}", "{", "punctuation.section.group.begin")]
    [InlineData("a AND (b OR c)", "OR", "keyword.operator.logical.infix")]
    [InlineData("If(a, b, c)", "If", "support.function.operator")]
    [InlineData("let adult = a; adult", "let", "keyword.control.let")]
    [InlineData("let adult = a; adult", "=", "keyword.operator.assignment")]
    [InlineData("let adult = a; adult", ";", "punctuation.terminator.statement")]
    [InlineData("let adult = a; adult", "adult", "variable.other.term")]
    [InlineData("a AND rule(\"adult\")", "rule", "support.function.library-reference")]
    [InlineData("a AND RULE(\"adult\")", "adult", "string.quoted.double")]
    [InlineData("rule(\"adult\", hash: \"AB12\")", "hash", "variable.parameter.argument")]
    [InlineData("rule(\"adult\", hash: \"AB12\")", "AB12", "string.quoted.double")]
    [InlineData("rule(name: \"x\")", "rule", "entity.name.function.term")]
    public void Tokenize_Construct_GetsItsScope_Test(string text, string needle, string expectedScope)
    {
        string scope = ScopeAt(text, needle);

        Assert.Contains(scope.Split(' '), part => part.StartsWith(expectedScope, StringComparison.Ordinal));
    }

    /// <summary>A word operator inside an identifier is part of the identifier, not an operator.</summary>
    [Fact]
    public void Tokenize_KeywordInsideAnIdentifier_IsOneTerm_Test()
    {
        string[] scopes = Grammar.Value.Tokenize("hasAnyOrder AND notes");

        Assert.All(
            scopes.Take("hasAnyOrder".Length),
            scope => Assert.EndsWith("variable.other.term.truthweaver", scope, StringComparison.Ordinal)
        );
        Assert.EndsWith("variable.other.term.truthweaver", scopes[^1], StringComparison.Ordinal);
    }

    /// <summary>The language configuration and the extension manifest are valid JSON and point at files that exist.</summary>
    [Fact]
    public void EditorFolder_Files_AreConsistent_Test()
    {
        using JsonDocument configuration = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(EditorFolder, "language-configuration.json"))
        );
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(EditorFolder, "package.json")));

        JsonElement grammarEntry = manifest.RootElement.GetProperty("contributes").GetProperty("grammars")[0];
        JsonElement languageEntry = manifest.RootElement.GetProperty("contributes").GetProperty("languages")[0];

        Assert.Equal("//", configuration.RootElement.GetProperty("comments").GetProperty("lineComment").GetString());
        Assert.Equal(Grammar.Value.ScopeName, grammarEntry.GetProperty("scopeName").GetString());
        Assert.Equal(languageEntry.GetProperty("id").GetString(), grammarEntry.GetProperty("language").GetString());
        Assert.True(File.Exists(Path.Combine(EditorFolder, grammarEntry.GetProperty("path").GetString()!)));
        Assert.True(File.Exists(Path.Combine(EditorFolder, languageEntry.GetProperty("configuration").GetString()!)));
        Assert.False(manifest.RootElement.TryGetProperty("dependencies", out _));
        Assert.False(manifest.RootElement.TryGetProperty("devDependencies", out _));
        Assert.False(manifest.RootElement.TryGetProperty("main", out _));
    }

    [GeneratedRegex(@"^<!--\s*doctest:(?:rule|ruletext)\b", RegexOptions.CultureInvariant)]
    private static partial Regex RuleMarker();

    /// <summary>Returns the body of each <c>text</c> block that follows a <c>doctest:rule</c> or <c>doctest:ruletext</c> marker.</summary>
    private static List<string> RuleTextBlocks(string markdown)
    {
        string[] lines = [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];
        List<string> blocks = [];
        int i = 0;
        while (i < lines.Length)
        {
            if (!RuleMarker().IsMatch(lines[i++]))
            {
                continue;
            }

            int fence = i;
            while (fence < lines.Length && lines[fence].Length == 0)
            {
                fence++;
            }

            if (fence >= lines.Length || !lines[fence].StartsWith("```", StringComparison.Ordinal))
            {
                continue;
            }

            int close = fence + 1;
            while (close < lines.Length && !lines[close].StartsWith("```", StringComparison.Ordinal))
            {
                close++;
            }

            blocks.Add(string.Join('\n', lines[(fence + 1)..close]));
            i = close + 1;
        }

        return blocks;
    }

    /// <summary>Returns each run of non-white-space characters that no rule claims, in order.</summary>
    private static List<string> Unscoped(string text)
    {
        string[] scopes = Grammar.Value.Tokenize(text);
        List<string> runs = [];
        System.Text.StringBuilder run = new();
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]) && IsUnscoped(scopes[i]))
            {
                run.Append(text[i]);
            }
            else if (run.Length > 0)
            {
                runs.Add(run.ToString());
                run.Clear();
            }
        }

        if (run.Length > 0)
        {
            runs.Add(run.ToString());
        }

        return runs;
    }

    /// <summary>A character is unscoped when it has no scope besides the root and the structural <c>meta.</c> scopes.</summary>
    private static bool IsUnscoped(string scope)
    {
        return scope.Split(' ').Skip(1).All(part => part.StartsWith("meta.", StringComparison.Ordinal));
    }

    private static string ScopeAt(string text, string needle)
    {
        int index = text.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{needle}' is not in '{text}'.");
        return Grammar.Value.Tokenize(text)[index];
    }
}
