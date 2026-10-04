namespace TruthWeaver.Predicates;

using System.Globalization;
using System.Numerics;
using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made type-test predicate factories. Each test has two overloads under one name: a <see cref="string"/> selector
/// (a parse-based test of text) and an <see cref="object"/> selector (a runtime-type test that also reads text). Each
/// positive test <c>IsX</c> has a twin <c>IsNotX</c> that is its Strong Kleene complement. A <see langword="null"/>
/// selected value answers <see cref="TruthValue.Unknown"/> for both, never a fault, because the type of a missing value is
/// not known. All parsing uses <see cref="CultureInfo.InvariantCulture"/>, so the host's current culture never changes a
/// result. No factory takes a rule-text argument.
/// </summary>
public static class TypePredicates
{
    private static readonly string[] IsoFormats = ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"];

    /// <summary>Creates a predicate that is true when the selected value is a GUID, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text is accepted by <see cref="Guid.TryParse(string?, out Guid)"/>. That covers the 32-digit form (<c>N</c>), the hyphenated form (<c>D</c>), and the brace (<c>B</c>), parenthesis (<c>P</c>) and hexadecimal-struct (<c>X</c>) forms. For an <see cref="object"/> selector a <see cref="Guid"/> instance also counts.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsGuid<TContext>(string name, Func<TContext, string?> selector, string label = "Is Guid")
    {
        return Create(
            name,
            label,
            "True when the selected value is a GUID, false when it is not, unknown when it is null.",
            selector,
            GuidTest,
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is a GUID, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text is accepted by <see cref="Guid.TryParse(string?, out Guid)"/>. That covers the 32-digit form (<c>N</c>), the hyphenated form (<c>D</c>), and the brace (<c>B</c>), parenthesis (<c>P</c>) and hexadecimal-struct (<c>X</c>) forms. For an <see cref="object"/> selector a <see cref="Guid"/> instance also counts.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsGuid<TContext>(string name, Func<TContext, object?> selector, string label = "Is Guid")
    {
        return Create(
            name,
            label,
            "True when the selected value is a GUID, false when it is not, unknown when it is null.",
            selector,
            GuidTest,
            false
        );
    }

    /// <summary>Creates the K3 complement of <c>IsGuid</c>: true when the selected value is not a GUID, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text is accepted by <see cref="Guid.TryParse(string?, out Guid)"/>. That covers the 32-digit form (<c>N</c>), the hyphenated form (<c>D</c>), and the brace (<c>B</c>), parenthesis (<c>P</c>) and hexadecimal-struct (<c>X</c>) forms. For an <see cref="object"/> selector a <see cref="Guid"/> instance also counts.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotGuid<TContext>(string name, Func<TContext, string?> selector, string label = "Is Not Guid")
    {
        return Create(
            name,
            label,
            "Complement of IsGuid: true when the selected value is not a GUID, false when it is, unknown when it is null.",
            selector,
            GuidTest,
            true
        );
    }

    /// <summary>Creates the K3 complement of <c>IsGuid</c>: true when the selected value is not a GUID, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text is accepted by <see cref="Guid.TryParse(string?, out Guid)"/>. That covers the 32-digit form (<c>N</c>), the hyphenated form (<c>D</c>), and the brace (<c>B</c>), parenthesis (<c>P</c>) and hexadecimal-struct (<c>X</c>) forms. For an <see cref="object"/> selector a <see cref="Guid"/> instance also counts.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotGuid<TContext>(string name, Func<TContext, object?> selector, string label = "Is Not Guid")
    {
        return Create(
            name,
            label,
            "Complement of IsGuid: true when the selected value is not a GUID, false when it is, unknown when it is null.",
            selector,
            GuidTest,
            true
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is numeric, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text parses as a finite <see cref="double"/> with <see cref="NumberStyles.Float"/> and <see cref="CultureInfo.InvariantCulture"/>. A sign, a decimal point and an exponent are accepted. Thousands separators, currency symbols, the texts <c>NaN</c> and <c>Infinity</c>, and a value that overflows <see cref="double"/> are rejected. For an <see cref="object"/> selector an instance of a built-in numeric type also counts (<see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="nint"/>, <see cref="nuint"/>, <see cref="Int128"/>, <see cref="UInt128"/>, <see cref="BigInteger"/>, <see cref="decimal"/>, <see cref="Half"/>, <see cref="float"/>, <see cref="double"/>), except a <c>NaN</c> or infinite floating-point value.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNumeric<TContext>(string name, Func<TContext, string?> selector, string label = "Is Numeric")
    {
        return Create(
            name,
            label,
            "True when the selected value is numeric, false when it is not, unknown when it is null.",
            selector,
            NumericTest,
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is numeric, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text parses as a finite <see cref="double"/> with <see cref="NumberStyles.Float"/> and <see cref="CultureInfo.InvariantCulture"/>. A sign, a decimal point and an exponent are accepted. Thousands separators, currency symbols, the texts <c>NaN</c> and <c>Infinity</c>, and a value that overflows <see cref="double"/> are rejected. For an <see cref="object"/> selector an instance of a built-in numeric type also counts (<see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="nint"/>, <see cref="nuint"/>, <see cref="Int128"/>, <see cref="UInt128"/>, <see cref="BigInteger"/>, <see cref="decimal"/>, <see cref="Half"/>, <see cref="float"/>, <see cref="double"/>), except a <c>NaN</c> or infinite floating-point value.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNumeric<TContext>(string name, Func<TContext, object?> selector, string label = "Is Numeric")
    {
        return Create(
            name,
            label,
            "True when the selected value is numeric, false when it is not, unknown when it is null.",
            selector,
            NumericTest,
            false
        );
    }

    /// <summary>Creates the K3 complement of <c>IsNumeric</c>: true when the selected value is not numeric, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text parses as a finite <see cref="double"/> with <see cref="NumberStyles.Float"/> and <see cref="CultureInfo.InvariantCulture"/>. A sign, a decimal point and an exponent are accepted. Thousands separators, currency symbols, the texts <c>NaN</c> and <c>Infinity</c>, and a value that overflows <see cref="double"/> are rejected. For an <see cref="object"/> selector an instance of a built-in numeric type also counts (<see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="nint"/>, <see cref="nuint"/>, <see cref="Int128"/>, <see cref="UInt128"/>, <see cref="BigInteger"/>, <see cref="decimal"/>, <see cref="Half"/>, <see cref="float"/>, <see cref="double"/>), except a <c>NaN</c> or infinite floating-point value.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNumeric<TContext>(string name, Func<TContext, string?> selector, string label = "Is Not Numeric")
    {
        return Create(
            name,
            label,
            "Complement of IsNumeric: true when the selected value is not numeric, false when it is, unknown when it is null.",
            selector,
            NumericTest,
            true
        );
    }

    /// <summary>Creates the K3 complement of <c>IsNumeric</c>: true when the selected value is not numeric, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text parses as a finite <see cref="double"/> with <see cref="NumberStyles.Float"/> and <see cref="CultureInfo.InvariantCulture"/>. A sign, a decimal point and an exponent are accepted. Thousands separators, currency symbols, the texts <c>NaN</c> and <c>Infinity</c>, and a value that overflows <see cref="double"/> are rejected. For an <see cref="object"/> selector an instance of a built-in numeric type also counts (<see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="nint"/>, <see cref="nuint"/>, <see cref="Int128"/>, <see cref="UInt128"/>, <see cref="BigInteger"/>, <see cref="decimal"/>, <see cref="Half"/>, <see cref="float"/>, <see cref="double"/>), except a <c>NaN</c> or infinite floating-point value.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotNumeric<TContext>(string name, Func<TContext, object?> selector, string label = "Is Not Numeric")
    {
        return Create(
            name,
            label,
            "Complement of IsNumeric: true when the selected value is not numeric, false when it is, unknown when it is null.",
            selector,
            NumericTest,
            true
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is a URL, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text is an absolute URI (<see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/> with <see cref="UriKind.Absolute"/>) whose scheme is <c>http</c> or <c>https</c>. A relative reference and any other scheme are rejected. For an <see cref="object"/> selector a <see cref="Uri"/> instance counts when it meets the same rule.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsUrl<TContext>(string name, Func<TContext, string?> selector, string label = "Is Url")
    {
        return Create(
            name,
            label,
            "True when the selected value is a URL, false when it is not, unknown when it is null.",
            selector,
            UrlTest,
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is a URL, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text is an absolute URI (<see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/> with <see cref="UriKind.Absolute"/>) whose scheme is <c>http</c> or <c>https</c>. A relative reference and any other scheme are rejected. For an <see cref="object"/> selector a <see cref="Uri"/> instance counts when it meets the same rule.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsUrl<TContext>(string name, Func<TContext, object?> selector, string label = "Is Url")
    {
        return Create(
            name,
            label,
            "True when the selected value is a URL, false when it is not, unknown when it is null.",
            selector,
            UrlTest,
            false
        );
    }

    /// <summary>Creates the K3 complement of <c>IsUrl</c>: true when the selected value is not a URL, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text is an absolute URI (<see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/> with <see cref="UriKind.Absolute"/>) whose scheme is <c>http</c> or <c>https</c>. A relative reference and any other scheme are rejected. For an <see cref="object"/> selector a <see cref="Uri"/> instance counts when it meets the same rule.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotUrl<TContext>(string name, Func<TContext, string?> selector, string label = "Is Not Url")
    {
        return Create(
            name,
            label,
            "Complement of IsUrl: true when the selected value is not a URL, false when it is, unknown when it is null.",
            selector,
            UrlTest,
            true
        );
    }

    /// <summary>Creates the K3 complement of <c>IsUrl</c>: true when the selected value is not a URL, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text is an absolute URI (<see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/> with <see cref="UriKind.Absolute"/>) whose scheme is <c>http</c> or <c>https</c>. A relative reference and any other scheme are rejected. For an <see cref="object"/> selector a <see cref="Uri"/> instance counts when it meets the same rule.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotUrl<TContext>(string name, Func<TContext, object?> selector, string label = "Is Not Url")
    {
        return Create(
            name,
            label,
            "Complement of IsUrl: true when the selected value is not a URL, false when it is, unknown when it is null.",
            selector,
            UrlTest,
            true
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is a string, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The selected value is a <see cref="string"/>. The empty string counts. For a <see cref="string"/> selector every non-null value is a string, so the test reduces to a null test; it is mainly useful with an <see cref="object"/> selector.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsString<TContext>(string name, Func<TContext, string?> selector, string label = "Is String")
    {
        return Create(
            name,
            label,
            "True when the selected value is a string, false when it is not, unknown when it is null.",
            selector,
            StringTest,
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is a string, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The selected value is a <see cref="string"/>. The empty string counts. For a <see cref="string"/> selector every non-null value is a string, so the test reduces to a null test; it is mainly useful with an <see cref="object"/> selector.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsString<TContext>(string name, Func<TContext, object?> selector, string label = "Is String")
    {
        return Create(
            name,
            label,
            "True when the selected value is a string, false when it is not, unknown when it is null.",
            selector,
            StringTest,
            false
        );
    }

    /// <summary>Creates the K3 complement of <c>IsString</c>: true when the selected value is not a string, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The selected value is a <see cref="string"/>. The empty string counts. For a <see cref="string"/> selector every non-null value is a string, so the test reduces to a null test; it is mainly useful with an <see cref="object"/> selector.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotString<TContext>(string name, Func<TContext, string?> selector, string label = "Is Not String")
    {
        return Create(
            name,
            label,
            "Complement of IsString: true when the selected value is not a string, false when it is, unknown when it is null.",
            selector,
            StringTest,
            true
        );
    }

    /// <summary>Creates the K3 complement of <c>IsString</c>: true when the selected value is not a string, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The selected value is a <see cref="string"/>. The empty string counts. For a <see cref="string"/> selector every non-null value is a string, so the test reduces to a null test; it is mainly useful with an <see cref="object"/> selector.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotString<TContext>(string name, Func<TContext, object?> selector, string label = "Is Not String")
    {
        return Create(
            name,
            label,
            "Complement of IsString: true when the selected value is not a string, false when it is, unknown when it is null.",
            selector,
            StringTest,
            true
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is an ISO 8601 date and time with an offset, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text is ISO 8601 extended format with a time of day to the second (<c>yyyy-MM-ddTHH:mm:ss</c>, with an optional fraction of up to seven digits) and an explicit offset (<c>+hh:mm</c> or <c>-hh:mm</c>) or <c>Z</c>. It is parsed with <see cref="DateTimeStyles.None"/> and <see cref="CultureInfo.InvariantCulture"/>. Text without an offset is rejected, so the instant is never guessed. For an <see cref="object"/> selector a <see cref="DateTimeOffset"/> instance also counts; a <see cref="DateTime"/> instance does not.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsDateTimeOffset<TContext>(string name, Func<TContext, string?> selector, string label = "Is DateTimeOffset")
    {
        return Create(
            name,
            label,
            "True when the selected value is an ISO 8601 date and time with an offset, false when it is not, unknown when it is null.",
            selector,
            DateTimeOffsetTest,
            false
        );
    }

    /// <summary>Creates a predicate that is true when the selected value is an ISO 8601 date and time with an offset, false when it is not, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition: The text is ISO 8601 extended format with a time of day to the second (<c>yyyy-MM-ddTHH:mm:ss</c>, with an optional fraction of up to seven digits) and an explicit offset (<c>+hh:mm</c> or <c>-hh:mm</c>) or <c>Z</c>. It is parsed with <see cref="DateTimeStyles.None"/> and <see cref="CultureInfo.InvariantCulture"/>. Text without an offset is rejected, so the instant is never guessed. For an <see cref="object"/> selector a <see cref="DateTimeOffset"/> instance also counts; a <see cref="DateTime"/> instance does not.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsDateTimeOffset<TContext>(string name, Func<TContext, object?> selector, string label = "Is DateTimeOffset")
    {
        return Create(
            name,
            label,
            "True when the selected value is an ISO 8601 date and time with an offset, false when it is not, unknown when it is null.",
            selector,
            DateTimeOffsetTest,
            false
        );
    }

    /// <summary>Creates the K3 complement of <c>IsDateTimeOffset</c>: true when the selected value is not an ISO 8601 date and time with an offset, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text is ISO 8601 extended format with a time of day to the second (<c>yyyy-MM-ddTHH:mm:ss</c>, with an optional fraction of up to seven digits) and an explicit offset (<c>+hh:mm</c> or <c>-hh:mm</c>) or <c>Z</c>. It is parsed with <see cref="DateTimeStyles.None"/> and <see cref="CultureInfo.InvariantCulture"/>. Text without an offset is rejected, so the instant is never guessed. For an <see cref="object"/> selector a <see cref="DateTimeOffset"/> instance also counts; a <see cref="DateTime"/> instance does not.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotDateTimeOffset<TContext>(string name, Func<TContext, string?> selector, string label = "Is Not DateTimeOffset")
    {
        return Create(
            name,
            label,
            "Complement of IsDateTimeOffset: true when the selected value is not an ISO 8601 date and time with an offset, false when it is, unknown when it is null.",
            selector,
            DateTimeOffsetTest,
            true
        );
    }

    /// <summary>Creates the K3 complement of <c>IsDateTimeOffset</c>: true when the selected value is not an ISO 8601 date and time with an offset, false when it is, and <see cref="TruthValue.Unknown"/> when it is <see langword="null"/>. Definition of the positive test: The text is ISO 8601 extended format with a time of day to the second (<c>yyyy-MM-ddTHH:mm:ss</c>, with an optional fraction of up to seven digits) and an explicit offset (<c>+hh:mm</c> or <c>-hh:mm</c>) or <c>Z</c>. It is parsed with <see cref="DateTimeStyles.None"/> and <see cref="CultureInfo.InvariantCulture"/>. Text without an offset is rejected, so the instant is never guessed. For an <see cref="object"/> selector a <see cref="DateTimeOffset"/> instance also counts; a <see cref="DateTime"/> instance does not.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the object value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) IsNotDateTimeOffset<TContext>(string name, Func<TContext, object?> selector, string label = "Is Not DateTimeOffset")
    {
        return Create(
            name,
            label,
            "Complement of IsDateTimeOffset: true when the selected value is not an ISO 8601 date and time with an offset, false when it is, unknown when it is null.",
            selector,
            DateTimeOffsetTest,
            true
        );
    }

    private static bool GuidTest(string value)
    {
        return Guid.TryParse(value, out _);
    }

    private static bool GuidTest(object value)
    {
        return value is Guid || (value is string text && GuidTest(text));
    }

    private static bool NumericTest(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            && double.IsFinite(parsed);
    }

    private static bool NumericTest(object value)
    {
        return value switch
        {
            string text => NumericTest(text),
            sbyte or byte or short or ushort or int or uint or long or ulong or nint or nuint => true,
            Int128 or UInt128 or BigInteger or decimal => true,
            Half half => Half.IsFinite(half),
            float single => float.IsFinite(single),
            double number => double.IsFinite(number),
            _ => false,
        };
    }

    private static bool IsHttpOrHttps(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool UrlTest(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && IsHttpOrHttps(uri);
    }

    private static bool UrlTest(object value)
    {
        return value switch
        {
            string text => UrlTest(text),
            Uri { IsAbsoluteUri: true } uri => IsHttpOrHttps(uri),
            _ => false,
        };
    }

    private static bool StringTest(string value)
    {
        return true;
    }

    private static bool StringTest(object value)
    {
        return value is string;
    }

    private static bool DateTimeOffsetTest(string value)
    {
        return HasExplicitOffset(value)
            && DateTimeOffset.TryParseExact(value, IsoFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool DateTimeOffsetTest(object value)
    {
        return value is DateTimeOffset || (value is string text && DateTimeOffsetTest(text));
    }

    // The K format specifier also matches no offset at all (and then assumes the local zone), so the offset must be
    // required explicitly: the text ends in Z or in +hh:mm / -hh:mm.
    private static bool HasExplicitOffset(string value)
    {
        return value.EndsWith('Z') || (value.Length > 6 && value[^6] is '+' or '-' && value[^3] == ':');
    }

    /// <summary>Builds the schema and delegate shared by every type test; a null selection is <c>Unknown</c>.</summary>
    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Create<TContext, TValue>(
        string name,
        string label,
        string description,
        Func<TContext, TValue?> selector,
        Func<TValue, bool> test,
        bool negate
    )
        where TValue : class
    {
        PredicateSchema schema = PredicateSchema.NoArguments(name, label, description);
        return (
            schema,
            (context, _, _) =>
            {
                TValue? selected = selector(context);
                return selected is null
                    ? ValueTask.FromResult(TruthValue.Unknown)
                    : PredicateResult.FromBoolAsync(test(selected) != negate);
            }
        );
    }
}
