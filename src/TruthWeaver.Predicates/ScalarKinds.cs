namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>The scalar kinds the catalog supports, one per existing <see cref="LiteralKind"/> pair.</summary>
internal static class ScalarKinds
{
    public static readonly ScalarKind<long> Int64 = new(
        LiteralKind.Int64,
        LiteralKind.Int64Array,
        "integer",
        static (args, name) => args.GetInt64(name),
        static (args, name) => args.GetInt64Array(name)
    );

    public static readonly ScalarKind<decimal> Decimal = new(
        LiteralKind.Decimal,
        LiteralKind.DecimalArray,
        "decimal",
        static (args, name) => args.GetDecimal(name),
        static (args, name) => args.GetDecimalArray(name)
    );

    public static readonly ScalarKind<bool> Boolean = new(
        LiteralKind.Boolean,
        LiteralKind.BooleanArray,
        "boolean",
        static (args, name) => args.GetBool(name),
        static (args, name) => args.GetBoolArray(name)
    );

    public static readonly ScalarKind<Guid> Guid = new(
        LiteralKind.Guid,
        LiteralKind.GuidArray,
        "GUID",
        static (args, name) => args.GetGuid(name),
        static (args, name) => args.GetGuidArray(name)
    );

    public static readonly ScalarKind<DateTimeOffset> DateTimeOffset = new(
        LiteralKind.DateTimeOffset,
        LiteralKind.DateTimeOffsetArray,
        "date-time",
        static (args, name) => args.GetDateTimeOffset(name),
        static (args, name) => args.GetDateTimeOffsetArray(name)
    );
}
