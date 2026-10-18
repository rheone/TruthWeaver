namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Yaml;
using YamlDotNet.RepresentationModel;

public sealed class YamlNodeCursorTests
{
    /// <summary>
    /// A YAML node that is not a constant (a number, a null, a mapping, a sequence, an unknown word) is read as no
    /// constant, with the value <c>Unknown</c>.
    /// </summary>
    /// <param name="yaml">A YAML document whose root is not a K3 constant.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("maybe")]
    public void TryGetTruthValue_YamlNonConstant_ReturnsFalseWithUnknown_Test(string yaml)
    {
        YamlStream stream = [];
        using (StringReader reader = new(yaml))
        {
            stream.Load(reader);
        }

        YamlNodeCursor cursor = new(stream.Documents[0].RootNode);

        bool read = cursor.TryGetTruthValue(out TruthValue value);

        Assert.False(read);
        Assert.Equal(TruthValue.Unknown, value);
    }
}
