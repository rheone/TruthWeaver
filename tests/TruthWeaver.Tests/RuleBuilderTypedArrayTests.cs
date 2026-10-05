namespace TruthWeaver.Tests;

using TruthWeaver.Building;

/// <summary>Typed array and list arguments of <c>RuleBuilder.Predicate</c> build the same node as the <c>object[]</c> form.</summary>
public sealed class RuleBuilderTypedArrayTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid G = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>A <c>long[]</c> argument builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_a_long_array_matches_the_object_array_Test()
    {
        AssertArray<long>([1L, 2L]);
    }

    /// <summary>A <c>decimal[]</c> argument builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_a_decimal_array_matches_the_object_array_Test()
    {
        AssertArray<decimal>([1.5m, 2.5m]);
    }

    /// <summary>A <c>bool[]</c> argument builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_a_bool_array_matches_the_object_array_Test()
    {
        AssertArray<bool>([true, false]);
    }

    /// <summary>A <c>Guid[]</c> argument builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_a_guid_array_matches_the_object_array_Test()
    {
        AssertArray<Guid>([G]);
    }

    /// <summary>A <c>DateTimeOffset[]</c> argument builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_a_date_time_offset_array_matches_the_object_array_Test()
    {
        AssertArray<DateTimeOffset>([T0]);
    }

    /// <summary>An <c>int[]</c>, <c>double[]</c> or <c>string[]</c> argument still builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_int_double_and_string_arrays_matches_the_object_array_Test()
    {
        AssertArray<int>([1, 2]);
        AssertArray<double>([1.5, 2.5]);
        AssertArray<string>(["a", "b"]);
    }

    /// <summary>A <see cref="List{T}"/> of each supported kind builds the same node as <c>object[]</c>.</summary>
    [Fact]
    public void Predicate_with_a_list_of_each_kind_matches_the_object_array_Test()
    {
        AssertSameAsObjectArray(new List<long> { 1L, 2L });
        AssertSameAsObjectArray(new List<decimal> { 1.5m });
        AssertSameAsObjectArray(new List<bool> { true });
        AssertSameAsObjectArray(new List<Guid> { G });
        AssertSameAsObjectArray(new List<DateTimeOffset> { T0 });
        AssertSameAsObjectArray(new List<string> { "a" });
    }

    /// <summary>An element of an unsupported type still throws <see cref="ArgumentException"/>.</summary>
    [Fact]
    public void Predicate_with_an_unsupported_element_type_throws_Test()
    {
        RuleBuilder builder = RuleBuilder.Predicate("p", ("values", new List<char> { 'a' }));

        Assert.Throws<ArgumentException>(() => builder.ToJson());
    }

    /// <summary>
    /// Takes a real <c>T[]</c> (a collection expression binds the array type), so the argument reaches
    /// <c>RuleBuilder.Predicate</c> as a value-type array.
    /// </summary>
    private static void AssertArray<T>(T[] typed)
        where T : notnull
    {
        AssertSameAsObjectArray(typed);
    }

    private static void AssertSameAsObjectArray<T>(IEnumerable<T> typed)
        where T : notnull
    {
        object[] expected = [.. typed.Cast<object>()];

        string actual = RuleBuilder.Predicate("p", ("values", typed)).ToJson();

        Assert.Equal(RuleBuilder.Predicate("p", ("values", expected)).ToJson(), actual);
    }
}
