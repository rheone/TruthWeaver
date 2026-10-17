namespace TruthWeaver.Testing.Tests;

using System.Collections;
using TruthWeaver.Abstractions;

/// <summary>The argument dictionary that records which names a predicate looks up.</summary>
public sealed class ArgumentReadTrackerTests
{
    private static readonly string[] ReadNames = ["role", "undeclared"];

    private static readonly string[] DeclaredNames = ["min", "role"];

    private static readonly Dictionary<string, LiteralValue> Values = new(StringComparer.Ordinal)
    {
        ["min"] = LiteralValue.OfInt64(18),
        ["role"] = LiteralValue.OfString("editor"),
    };

    /// <summary>The indexer returns the value and records the name as read.</summary>
    [Fact]
    public void Indexer_ExistingKey_ReturnsValueAndRecordsTheRead_Test()
    {
        HashSet<string> reads = [];
        ArgumentReadTracker tracker = new(Values, reads);

        LiteralValue value = tracker["min"];

        Assert.Equal(LiteralValue.OfInt64(18), value);
        Assert.Equal(["min"], reads);
    }

    /// <summary>ContainsKey answers from the values and records the name even when it is absent.</summary>
    [Fact]
    public void ContainsKey_FoundAndMissingKeys_AnswerAndRecordBoth_Test()
    {
        HashSet<string> reads = [];
        ArgumentReadTracker tracker = new(Values, reads);

        bool found = tracker.ContainsKey("role");
        bool missing = tracker.ContainsKey("undeclared");

        Assert.True(found);
        Assert.False(missing);
        Assert.Equivalent(ReadNames, reads);
    }

    /// <summary>TryGetValue returns the value of a found name and records a missing name too.</summary>
    [Fact]
    public void TryGetValue_FoundAndMissingKeys_AnswerAndRecordBoth_Test()
    {
        HashSet<string> reads = [];
        ArgumentReadTracker tracker = new(Values, reads);

        bool found = tracker.TryGetValue("role", out LiteralValue value);
        bool missing = tracker.TryGetValue("undeclared", out _);

        Assert.True(found);
        Assert.Equal(LiteralValue.OfString("editor"), value);
        Assert.False(missing);
        Assert.Equivalent(ReadNames, reads);
    }

    /// <summary>Counting and enumerating the arguments is not a read of any name.</summary>
    [Fact]
    public void Enumeration_CountKeysValuesAndPairs_ReadNothing_Test()
    {
        HashSet<string> reads = [];
        ArgumentReadTracker tracker = new(Values, reads);

        int count = tracker.Count;
        string[] keys = [.. tracker.Keys];
        LiteralValue[] values = [.. tracker.Values];
        KeyValuePair<string, LiteralValue>[] pairs = [.. tracker];
        int untyped = 0;
        foreach (object? entry in (IEnumerable)tracker)
        {
            Assert.NotNull(entry);
            untyped++;
        }

        Assert.Equal(2, count);
        Assert.Equivalent(DeclaredNames, keys);
        Assert.Equal(2, values.Length);
        Assert.Equal(2, pairs.Length);
        Assert.Equal(2, untyped);
        Assert.Empty(reads);
    }
}
