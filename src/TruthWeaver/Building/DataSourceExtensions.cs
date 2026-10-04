namespace TruthWeaver.Building;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>Build-time access to an <see cref="IDataSource"/> for <see cref="RuleBuilder"/> (ADR-0006 decision 12).</summary>
public static class DataSourceExtensions
{
    /// <summary>
    /// Reads one value from <paramref name="source"/> now, while a rule is being assembled, so it can be passed to
    /// <see cref="RuleBuilder"/> as an ordinary literal argument. This fixes the value in the rule; it is not a variable
    /// (use <see cref="Arg.From"/> to resolve on every evaluation). The query must match exactly one node, and the
    /// conversion follows the same rules as a variable (a string converts to <see cref="DateTimeOffset"/> and
    /// <see cref="Guid"/>, an integer widens to <see cref="decimal"/>, nothing else is coerced).
    /// </summary>
    /// <typeparam name="T">One of <see cref="string"/>, <see cref="long"/>, <see cref="decimal"/>, <see cref="bool"/>, <see cref="DateTimeOffset"/> or <see cref="Guid"/>.</typeparam>
    /// <param name="source">The source to query.</param>
    /// <param name="query">The query, in the source's own dialect.</param>
    /// <param name="cancellationToken">A token to cancel the query.</param>
    /// <returns>The value the query matched.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="query"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException"><typeparamref name="T"/> is not one of the supported types.</exception>
    /// <exception cref="InvalidOperationException">The source reported an error, or the query did not match exactly one node of the requested type. The message names the cause and never the data.</exception>
    public static async ValueTask<T> GetAsync<T>(
        this IDataSource source,
        string query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        LiteralKind kind = KindOf(typeof(T));
        DataQueryResult result = await source.QueryAsync(query, cancellationToken).ConfigureAwait(false);
        if (result.ErrorKind is { } errorKind)
        {
            throw new InvalidOperationException(
                $"The data source could not answer the query ({errorKind}): {result.ErrorMessage}"
            );
        }

        if (
            !VariableConversion.TryConvert(
                result.Matches,
                kind,
                out LiteralValue value,
                out VariableFailureKind failure,
                out string message
            )
        )
        {
            throw new InvalidOperationException($"The query did not give a {kind} ({failure}): {message}");
        }

        object boxed = kind switch
        {
            LiteralKind.String => value.AsString(),
            LiteralKind.Int64 => value.AsInt64(),
            LiteralKind.Decimal => value.AsDecimal(),
            LiteralKind.Boolean => value.AsBoolean(),
            LiteralKind.DateTimeOffset => value.AsDateTimeOffset(),
            _ => value.AsGuid(),
        };
        return (T)boxed;
    }

    private static LiteralKind KindOf(Type type)
    {
        if (type == typeof(string))
        {
            return LiteralKind.String;
        }

        if (type == typeof(long))
        {
            return LiteralKind.Int64;
        }

        if (type == typeof(decimal))
        {
            return LiteralKind.Decimal;
        }

        if (type == typeof(bool))
        {
            return LiteralKind.Boolean;
        }

        if (type == typeof(DateTimeOffset))
        {
            return LiteralKind.DateTimeOffset;
        }

        return type == typeof(Guid)
            ? LiteralKind.Guid
            : throw new NotSupportedException(
                $"'{type}' has no literal kind; use string, long, decimal, bool, DateTimeOffset or Guid."
            );
    }
}
