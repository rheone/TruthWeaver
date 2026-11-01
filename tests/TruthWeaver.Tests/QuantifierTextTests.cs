namespace TruthWeaver.Tests;

using TruthWeaver.Ast;

/// <summary>
/// <see cref="QuantifierText"/> is the one home of each <see cref="QuantifierKind"/>'s spelling, so a new kind
/// cannot be added without a label, a tree-format word and a description.
/// </summary>
public sealed class QuantifierTextTests
{
    /// <summary>Every quantifier kind has a non-empty label, tree-format word and description.</summary>
    [Fact]
    public void Spellings_EveryQuantifierKind_HaveLabelTreeWordAndDescription_Test()
    {
        foreach (QuantifierKind kind in Enum.GetValues<QuantifierKind>())
        {
            Assert.False(string.IsNullOrWhiteSpace(QuantifierText.Label(kind)), $"{kind} label");
            Assert.False(string.IsNullOrWhiteSpace(QuantifierText.TreeFormatWord(kind)), $"{kind} tree word");
            Assert.False(string.IsNullOrWhiteSpace(QuantifierText.Description(kind)), $"{kind} description");
        }
    }

    /// <summary>The tree-format words are the lowercase labels and the readable names list each one.</summary>
    [Fact]
    public void TreeFormatWord_EveryQuantifierKind_IsLowercaseLabelAndReadable_Test()
    {
        foreach (QuantifierKind kind in Enum.GetValues<QuantifierKind>())
        {
            Assert.Equal(QuantifierText.Label(kind).ToLowerInvariant(), QuantifierText.TreeFormatWord(kind));
            Assert.Contains(QuantifierText.TreeFormatWord(kind), TreeFormatOpNames.ReadableNames);
        }
    }

    /// <summary>An undefined quantifier kind throws instead of falling through to Exists.</summary>
    [Fact]
    public void Description_UndefinedKind_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QuantifierText.Description((QuantifierKind)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuantifierText.TreeFormatWord((QuantifierKind)999));
    }
}
