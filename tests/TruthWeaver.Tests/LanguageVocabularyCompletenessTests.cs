namespace TruthWeaver.Tests;

using System.Globalization;
using TruthWeaver.Ast;
using TruthWeaver.Parsing;

/// <summary>
/// Every word the rule language is built from is a reserved word and a keyword suggestion, so a new operator cannot
/// ship with its word missing from either list.
/// </summary>
public sealed class LanguageVocabularyCompletenessTests
{
    // The operator table holds no alias list yet, so the two spellings of EQUIVALENT that are not its label are named here.
    private static readonly string[] EquivalentAliases = ["IFF", "XNOR"];

    private static readonly string[] QuantifierAndDefinitionWords = ["FORALL", "EXISTS", "Let"];

    private static readonly string[] ConstantWords = ["True", "False", "Unknown"];

    /// <summary>
    /// The label of every operator in the table, its aliases, each symbol of it that is written as a word, the quantifier
    /// words and the constants are all reserved words, in the case they are written and in lower and upper case.
    /// </summary>
    [Fact]
    public void IsReservedWord_EveryWordOfTheLanguage_IsTrueInAnyCase_Test()
    {
        string[] missing = [.. LanguageWords().Where(word => !IsReservedInAnyCase(word))];

        Assert.True(missing.Length == 0, $"Not reserved: {string.Join(", ", missing)}");
    }

    /// <summary>The label of every operator in the table, its aliases, the quantifier words and the constants are offered as keyword suggestions in the spelling the documentation uses.</summary>
    [Fact]
    public void Keywords_EveryWordOfTheLanguage_IsOfferedAsASuggestion_Test()
    {
        string[] missing = [.. LanguageWords().Where(word => !DslVocabulary.Keywords.Contains(word, StringComparer.Ordinal))];

        Assert.True(missing.Length == 0, $"Not offered as a keyword suggestion: {string.Join(", ", missing)}");
    }

    /// <summary>A keyword suggestion that is not a word of the language, or that is listed twice, is a stale or mistyped entry.</summary>
    [Fact]
    public void Keywords_EveryEntry_IsAWordOfTheLanguageListedOnce_Test()
    {
        HashSet<string> words = [.. LanguageWords()];
        string[] unknown = [.. DslVocabulary.Keywords.Where(keyword => !words.Contains(keyword))];
        string[] duplicated =
        [
            .. DslVocabulary.Keywords.GroupBy(keyword => keyword).Where(group => group.Count() > 1).Select(group => group.Key),
        ];

        Assert.True(unknown.Length == 0, $"Not a word of the language: {string.Join(", ", unknown)}");
        Assert.True(duplicated.Length == 0, $"Listed twice: {string.Join(", ", duplicated)}");
    }

    private static bool IsReservedInAnyCase(string word)
    {
        return DslParser.IsReservedWord(word)
            && DslParser.IsReservedWord(word.ToLower(CultureInfo.InvariantCulture))
            && DslParser.IsReservedWord(word.ToUpper(CultureInfo.InvariantCulture));
    }

    // The words the language is built from: each operator's name (a label such as AtLeast({K}) is written AtLeast), the
    // symbols of an operator that are spelled as words, the aliases, the quantifier words and the constants.
    private static IEnumerable<string> LanguageWords()
    {
        foreach (OperatorDefinition definition in OperatorDefinitions.All)
        {
            yield return NameOf(definition.LabelTemplate);

            foreach (string symbol in WordSymbols(definition))
            {
                yield return symbol;
            }
        }

        foreach (string word in EquivalentAliases.Concat(QuantifierAndDefinitionWords).Concat(ConstantWords))
        {
            yield return word;
        }
    }

    private static string NameOf(string labelTemplate)
    {
        int parenthesis = labelTemplate.IndexOf('(', StringComparison.Ordinal);
        return parenthesis < 0 ? labelTemplate : labelTemplate[..parenthesis];
    }

    private static IEnumerable<string> WordSymbols(OperatorDefinition definition)
    {
        return new[] { definition.SymbolicSpelling, definition.CStyleSpelling }
            .Concat(definition.ReadSymbols ?? [])
            .OfType<string>()
            .Where(symbol => symbol.All(char.IsAsciiLetter));
    }
}
