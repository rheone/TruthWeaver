namespace TruthWeaver.Testing.Tests.Fakes;

using TruthWeaver.Abstractions;

public sealed class StubValuesTests
{
    /// <summary>A key in rule text is stored as the canonical text of the term.</summary>
    [Fact]
    public void TryNormalizeTerm_SpacedTerm_GivesTheCanonicalText_Test()
    {
        bool normalized = StubValues.TryNormalizeTerm("hasRole( role:\"admin\" )", out string? canonical, out _);

        Assert.True(normalized);
        Assert.Equal("hasRole(role: \"admin\")", canonical);
    }

    /// <summary>A key that is not one term, or that holds a from(...) reference, is not usable.</summary>
    [Theory]
    [InlineData("isActive AND isAdmin")]
    [InlineData("inRegion(region: from(\"user\", \"$.region\"))")]
    [InlineData("isActive AND")]
    public void TryNormalizeTerm_NotOnePlainTerm_ReturnsAnError_Test(string key)
    {
        bool normalized = StubValues.TryNormalizeTerm(key, out string? canonical, out string? error);

        Assert.False(normalized);
        Assert.Null(canonical);
        Assert.NotNull(error);
    }

    /// <summary>Two keys for one term are a problem, and Create throws with the message.</summary>
    [Fact]
    public void Create_SameTermTwice_ThrowsArgumentException_Test()
    {
        KeyValuePair<string, TruthValue>[] values =
        [
            new("hasRole(role: \"admin\")", TruthValue.True),
            new("hasRole( role: \"admin\" )", TruthValue.False),
        ];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => StubValues.Create(values));

        Assert.Contains("a second time", exception.Message, StringComparison.Ordinal);
    }
}
