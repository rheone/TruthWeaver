namespace TruthWeaver.Predicates.Tests;

using System.Globalization;
using System.Numerics;
using TruthWeaver.Abstractions;
using ObjectTest = (
    TruthWeaver.Abstractions.PredicateSchema Schema,
    System.Func<
        TruthWeaver.Predicates.Tests.ObjectContext,
        TruthWeaver.Abstractions.PredicateArguments,
        System.Threading.CancellationToken,
        System.Threading.Tasks.ValueTask<TruthWeaver.Abstractions.TruthValue>
    > Evaluate
);
using StringTest = (
    TruthWeaver.Abstractions.PredicateSchema Schema,
    System.Func<
        TruthWeaver.Predicates.Tests.TestContext,
        TruthWeaver.Abstractions.PredicateArguments,
        System.Threading.CancellationToken,
        System.Threading.Tasks.ValueTask<TruthWeaver.Abstractions.TruthValue>
    > Evaluate
);

public class TypePredicatesTests
{
    private static readonly PredicateArguments NoArgs = new(new Dictionary<string, LiteralValue>());

    /// <summary>Every accepted Guid text format is true, including the braces and no-hyphens forms.</summary>
    [Theory]
    [InlineData("d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f")]
    [InlineData("d3b07384d9a34c1e8f3b0a1b2c3d4e5f")]
    [InlineData("{d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f}")]
    [InlineData("(d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f)")]
    public async Task IsGuid_StringInAcceptedFormat_ReturnsTrue_Test(string text)
    {
        Assert.Equal(TruthValue.True, await RunAsync(TypePredicates.IsGuid<TestContext>("g", c => c.Value), text));
    }

    /// <summary>Text that is not a Guid, including one digit short, is false.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5")]
    public async Task IsGuid_StringNotAGuid_ReturnsFalse_Test(string text)
    {
        Assert.Equal(TruthValue.False, await RunAsync(TypePredicates.IsGuid<TestContext>("g", c => c.Value), text));
    }

    /// <summary>A Guid instance counts for an object selector, and so does Guid text; an int does not.</summary>
    [Fact]
    public async Task IsGuid_ObjectSelector_AcceptsGuidInstanceAndGuidText_Test()
    {
        ObjectTest predicate = TypePredicates.IsGuid<ObjectContext>("g", c => c.Value);

        Assert.Equal(TruthValue.True, await RunAsync(predicate, Guid.NewGuid()));
        Assert.Equal(TruthValue.True, await RunAsync(predicate, Guid.NewGuid().ToString()));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, 5));
    }

    /// <summary>A sign, a decimal point and an exponent are numeric.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("42")]
    [InlineData("-42")]
    [InlineData("+7")]
    [InlineData("3.14")]
    [InlineData(".5")]
    [InlineData("1e10")]
    [InlineData("-1.5E-3")]
    public async Task IsNumeric_StringInFloatStyle_ReturnsTrue_Test(string text)
    {
        Assert.Equal(TruthValue.True, await RunAsync(TypePredicates.IsNumeric<TestContext>("n", c => c.Value), text));
    }

    /// <summary>Thousands separators, currency, a comma decimal, NaN, Infinity, overflow and words are not numeric.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1,000")]
    [InlineData("$5")]
    [InlineData("3,14")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e400")]
    [InlineData("1.2.3")]
    public async Task IsNumeric_StringOutsideFloatStyle_ReturnsFalse_Test(string text)
    {
        Assert.Equal(TruthValue.False, await RunAsync(TypePredicates.IsNumeric<TestContext>("n", c => c.Value), text));
    }

    /// <summary>The current culture never changes the result: a comma-decimal culture still rejects "3,14".</summary>
    [Fact]
    public async Task IsNumeric_UnderCommaDecimalCulture_StillUsesInvariantCulture_Test()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            StringTest predicate = TypePredicates.IsNumeric<TestContext>("n", c => c.Value);

            Assert.Equal(TruthValue.False, await RunAsync(predicate, "3,14"));
            Assert.Equal(TruthValue.True, await RunAsync(predicate, "3.14"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>A numeric-typed object counts, except a NaN or infinite floating-point value; other types do not.</summary>
    [Fact]
    public async Task IsNumeric_ObjectSelector_CountsNumericTypesOnly_Test()
    {
        ObjectTest predicate = TypePredicates.IsNumeric<ObjectContext>("n", c => c.Value);

        foreach (object number in new object[] { 1, 2L, (byte)3, 4.5, 5.5f, 6.5m, new BigInteger(7), UInt128.One, "8" })
        {
            Assert.Equal(TruthValue.True, await RunAsync(predicate, number));
        }

        foreach (object other in new object[] { double.NaN, float.PositiveInfinity, true, 'c', "x", new() })
        {
            Assert.Equal(TruthValue.False, await RunAsync(predicate, other));
        }
    }

    /// <summary>An absolute http or https URL is true, whatever the host case.</summary>
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/a/b?q=1#f")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    public async Task IsUrl_AbsoluteHttpOrHttps_ReturnsTrue_Test(string text)
    {
        Assert.Equal(TruthValue.True, await RunAsync(TypePredicates.IsUrl<TestContext>("u", c => c.Value), text));
    }

    /// <summary>A relative reference, a bare host and any other scheme are rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("ftp://example.com")]
    [InlineData("mailto:a@b.c")]
    [InlineData("file:///etc/hosts")]
    public async Task IsUrl_NotAbsoluteHttpOrHttps_ReturnsFalse_Test(string text)
    {
        Assert.Equal(TruthValue.False, await RunAsync(TypePredicates.IsUrl<TestContext>("u", c => c.Value), text));
    }

    /// <summary>A Uri instance counts for an object selector when it meets the same rule.</summary>
    [Fact]
    public async Task IsUrl_ObjectSelector_AcceptsAbsoluteHttpUriInstanceOnly_Test()
    {
        ObjectTest predicate = TypePredicates.IsUrl<ObjectContext>("u", c => c.Value);

        Assert.Equal(TruthValue.True, await RunAsync(predicate, new Uri("https://example.com")));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, new Uri("ftp://example.com")));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, new Uri("/rel", UriKind.Relative)));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, 5));
    }

    /// <summary>With an object selector only a runtime string is true, the empty string included.</summary>
    [Fact]
    public async Task IsString_ObjectSelector_IsTrueOnlyForStrings_Test()
    {
        ObjectTest predicate = TypePredicates.IsString<ObjectContext>("s", c => c.Value);

        Assert.Equal(TruthValue.True, await RunAsync(predicate, "x"));
        Assert.Equal(TruthValue.True, await RunAsync(predicate, string.Empty));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, 5));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, 'c'));
    }

    /// <summary>With a string selector every non-null value is a string, so the test is a null test.</summary>
    [Fact]
    public async Task IsString_StringSelector_IsTrueForAnyNonNullValue_Test()
    {
        Assert.Equal(TruthValue.True, await RunAsync(TypePredicates.IsString<TestContext>("s", c => c.Value), "x"));
    }

    /// <summary>ISO 8601 text with Z, an offset or a fraction is true.</summary>
    [Theory]
    [InlineData("2026-10-04T12:00:00Z")]
    [InlineData("2026-10-04T12:00:00+02:00")]
    [InlineData("2026-10-04T12:00:00-04:30")]
    [InlineData("2026-10-04T12:00:00.1234567Z")]
    [InlineData("2026-10-04T12:00:00.5+01:00")]
    public async Task IsDateTimeOffset_IsoTextWithOffset_ReturnsTrue_Test(string text)
    {
        Assert.Equal(TruthValue.True, await RunAsync(TypePredicates.IsDateTimeOffset<TestContext>("d", c => c.Value), text));
    }

    /// <summary>Text without an offset, a date alone, a non-ISO layout and a bad month are rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("2026-10-04T12:00:00")]
    [InlineData("2026-10-04")]
    [InlineData("10/04/2026 12:00:00 +00:00")]
    [InlineData("2026-13-04T12:00:00Z")]
    [InlineData("not a date")]
    public async Task IsDateTimeOffset_TextWithoutOffsetOrNotIso_ReturnsFalse_Test(string text)
    {
        Assert.Equal(TruthValue.False, await RunAsync(TypePredicates.IsDateTimeOffset<TestContext>("d", c => c.Value), text));
    }

    /// <summary>A DateTimeOffset instance counts for an object selector; a DateTime does not.</summary>
    [Fact]
    public async Task IsDateTimeOffset_ObjectSelector_AcceptsDateTimeOffsetInstanceOnly_Test()
    {
        ObjectTest predicate = TypePredicates.IsDateTimeOffset<ObjectContext>("d", c => c.Value);

        Assert.Equal(TruthValue.True, await RunAsync(predicate, DateTimeOffset.UtcNow));
        Assert.Equal(TruthValue.True, await RunAsync(predicate, "2026-10-04T12:00:00Z"));
        Assert.Equal(TruthValue.False, await RunAsync(predicate, DateTime.UtcNow));
    }

    /// <summary>A null selected value is Unknown, never a fault, for every test and both selector shapes.</summary>
    [Fact]
    public async Task EveryTypeTest_NullSelectedValue_ReturnsUnknown_Test()
    {
        TestContext none = new(null);
        ObjectContext noneObject = new(null);
        StringTest[] stringTests =
        [
            TypePredicates.IsGuid<TestContext>("a", c => c.Value),
            TypePredicates.IsNotGuid<TestContext>("a", c => c.Value),
            TypePredicates.IsNumeric<TestContext>("a", c => c.Value),
            TypePredicates.IsNotNumeric<TestContext>("a", c => c.Value),
            TypePredicates.IsUrl<TestContext>("a", c => c.Value),
            TypePredicates.IsNotUrl<TestContext>("a", c => c.Value),
            TypePredicates.IsString<TestContext>("a", c => c.Value),
            TypePredicates.IsNotString<TestContext>("a", c => c.Value),
            TypePredicates.IsDateTimeOffset<TestContext>("a", c => c.Value),
            TypePredicates.IsNotDateTimeOffset<TestContext>("a", c => c.Value),
        ];
        ObjectTest[] objectTests =
        [
            TypePredicates.IsGuid<ObjectContext>("a", c => c.Value),
            TypePredicates.IsNotGuid<ObjectContext>("a", c => c.Value),
            TypePredicates.IsNumeric<ObjectContext>("a", c => c.Value),
            TypePredicates.IsNotNumeric<ObjectContext>("a", c => c.Value),
            TypePredicates.IsUrl<ObjectContext>("a", c => c.Value),
            TypePredicates.IsNotUrl<ObjectContext>("a", c => c.Value),
            TypePredicates.IsString<ObjectContext>("a", c => c.Value),
            TypePredicates.IsNotString<ObjectContext>("a", c => c.Value),
            TypePredicates.IsDateTimeOffset<ObjectContext>("a", c => c.Value),
            TypePredicates.IsNotDateTimeOffset<ObjectContext>("a", c => c.Value),
        ];

        foreach (StringTest test in stringTests)
        {
            Assert.Equal(TruthValue.Unknown, await test.Evaluate(none, NoArgs, CancellationToken.None));
        }

        foreach (ObjectTest test in objectTests)
        {
            Assert.Equal(TruthValue.Unknown, await test.Evaluate(noneObject, NoArgs, CancellationToken.None));
        }
    }

    /// <summary>Each NotX twin is the K3 complement of its positive test over accepted, rejected and null inputs.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f")]
    [InlineData("42")]
    [InlineData("https://example.com")]
    [InlineData("2026-10-04T12:00:00Z")]
    [InlineData("plain")]
    public async Task NotTwins_AnyInput_AreTheK3ComplementOfThePositiveTest_Test(string? text)
    {
        (StringTest, StringTest)[] pairs =
        [
            (TypePredicates.IsGuid<TestContext>("p", c => c.Value), TypePredicates.IsNotGuid<TestContext>("n", c => c.Value)),
            (
                TypePredicates.IsNumeric<TestContext>("p", c => c.Value),
                TypePredicates.IsNotNumeric<TestContext>("n", c => c.Value)
            ),
            (TypePredicates.IsUrl<TestContext>("p", c => c.Value), TypePredicates.IsNotUrl<TestContext>("n", c => c.Value)),
            (
                TypePredicates.IsString<TestContext>("p", c => c.Value),
                TypePredicates.IsNotString<TestContext>("n", c => c.Value)
            ),
            (
                TypePredicates.IsDateTimeOffset<TestContext>("p", c => c.Value),
                TypePredicates.IsNotDateTimeOffset<TestContext>("n", c => c.Value)
            ),
        ];

        foreach ((StringTest positive, StringTest negative) in pairs)
        {
            TestContext context = new(text);
            TruthValue expected = Complement(await positive.Evaluate(context, NoArgs, CancellationToken.None));
            Assert.Equal(expected, await negative.Evaluate(context, NoArgs, CancellationToken.None));
        }
    }

    /// <summary>The object-selector twins are also complements, for values of several runtime types.</summary>
    [Fact]
    public async Task NotTwins_ObjectSelector_AreTheK3ComplementOfThePositiveTest_Test()
    {
        foreach (object? value in new object?[] { null, 5, "x", Guid.Empty, new Uri("https://a.b"), DateTimeOffset.UnixEpoch })
        {
            ObjectContext context = new(value);
            (ObjectTest, ObjectTest)[] pairs =
            [
                (
                    TypePredicates.IsGuid<ObjectContext>("p", c => c.Value),
                    TypePredicates.IsNotGuid<ObjectContext>("n", c => c.Value)
                ),
                (
                    TypePredicates.IsNumeric<ObjectContext>("p", c => c.Value),
                    TypePredicates.IsNotNumeric<ObjectContext>("n", c => c.Value)
                ),
                (
                    TypePredicates.IsUrl<ObjectContext>("p", c => c.Value),
                    TypePredicates.IsNotUrl<ObjectContext>("n", c => c.Value)
                ),
                (
                    TypePredicates.IsString<ObjectContext>("p", c => c.Value),
                    TypePredicates.IsNotString<ObjectContext>("n", c => c.Value)
                ),
                (
                    TypePredicates.IsDateTimeOffset<ObjectContext>("p", c => c.Value),
                    TypePredicates.IsNotDateTimeOffset<ObjectContext>("n", c => c.Value)
                ),
            ];

            foreach ((ObjectTest positive, ObjectTest negative) in pairs)
            {
                TruthValue expected = Complement(await positive.Evaluate(context, NoArgs, CancellationToken.None));
                Assert.Equal(expected, await negative.Evaluate(context, NoArgs, CancellationToken.None));
            }
        }
    }

    /// <summary>The schema carries the registered name, a label, a description and no arguments.</summary>
    [Fact]
    public void IsGuid_Schema_HasNameLabelDescriptionAndNoArguments_Test()
    {
        PredicateSchema schema = TypePredicates.IsGuid<TestContext>("isGuid", c => c.Value).Schema;

        Assert.Equal("isGuid", schema.Name);
        Assert.NotEmpty(schema.Label);
        Assert.NotEmpty(schema.Description);
        Assert.Empty(schema.Arguments);
    }

    private static async Task<TruthValue> RunAsync(
        (
            PredicateSchema Schema,
            Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
        ) predicate,
        string? value
    )
    {
        return await predicate.Evaluate(new TestContext(value), NoArgs, CancellationToken.None);
    }

    private static async Task<TruthValue> RunAsync(
        (
            PredicateSchema Schema,
            Func<ObjectContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
        ) predicate,
        object? value
    )
    {
        return await predicate.Evaluate(new ObjectContext(value), NoArgs, CancellationToken.None);
    }

    private static TruthValue Complement(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }
}
