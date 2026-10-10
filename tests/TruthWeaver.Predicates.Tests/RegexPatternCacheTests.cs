namespace TruthWeaver.Predicates.Tests;

using System.Text.RegularExpressions;

public class RegexPatternCacheTests
{
    /// <summary>The cache never holds more patterns than its capacity: the pattern that exceeds it clears the cache first.</summary>
    [Fact]
    public void Get_MorePatternsThanCapacity_NeverHoldsMoreThanCapacity_Test()
    {
        RegexPatternCache cache = new(RegexOptions.None, TimeSpan.FromSeconds(1), capacity: 3);

        for (int i = 0; i < 20; i++)
        {
            _ = cache.Get($"pattern{i}");
            Assert.True(cache.Count <= cache.Capacity);
        }
    }

    /// <summary>The pattern that exceeds the capacity is still cached, so the cache keeps serving the hot pattern.</summary>
    [Fact]
    public void Get_PatternAfterTheCacheClears_IsCachedAgain_Test()
    {
        RegexPatternCache cache = new(RegexOptions.None, TimeSpan.FromSeconds(1), capacity: 2);
        _ = cache.Get("a");
        _ = cache.Get("b");

        Regex third = cache.Get("c");

        Assert.Equal(1, cache.Count);
        Assert.Same(third, cache.Get("c"));
    }

    /// <summary>A pattern already in the cache is returned as the same instance, so it is compiled once.</summary>
    [Fact]
    public void Get_SamePatternTwice_ReturnsTheSameInstance_Test()
    {
        RegexPatternCache cache = new(RegexOptions.None, TimeSpan.FromSeconds(1), capacity: 3);

        Assert.Same(cache.Get("^a"), cache.Get("^a"));
        Assert.Equal(1, cache.Count);
    }

    /// <summary>An invalid pattern throws and takes no cache slot.</summary>
    [Fact]
    public void Get_InvalidPattern_ThrowsAndCachesNothing_Test()
    {
        RegexPatternCache cache = new(RegexOptions.None, TimeSpan.FromSeconds(1), capacity: 3);

        _ = Assert.ThrowsAny<ArgumentException>(() => cache.Get("("));

        Assert.Equal(0, cache.Count);
    }

    /// <summary>Validating a pattern does not cache it.</summary>
    [Fact]
    public void Validate_ValidPattern_CachesNothing_Test()
    {
        RegexPatternCache cache = new(RegexOptions.None, TimeSpan.FromSeconds(1), capacity: 3);

        cache.Validate("^a");

        Assert.Equal(0, cache.Count);
    }

    /// <summary>A capacity below one is rejected, because a cache that cannot hold a pattern is a defect.</summary>
    [Fact]
    public void Constructor_CapacityBelowOne_Throws_Test()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RegexPatternCache(RegexOptions.None, TimeSpan.FromSeconds(1), capacity: 0)
        );
    }

    /// <summary>The public factories use a cache with the documented bound.</summary>
    [Fact]
    public void PatternCacheCapacity_Constant_Is256_Test()
    {
        Assert.Equal(256, RegexPredicates.PatternCacheCapacity);
    }
}
