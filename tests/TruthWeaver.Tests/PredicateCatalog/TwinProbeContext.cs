namespace TruthWeaver.Tests.PredicateCatalog;

/// <summary>
/// The context every twin probe evaluates against. Each predicate in the twin table selects one property, so one context
/// type serves the whole catalog. A null property is a missing selected value.
/// </summary>
/// <param name="Text">The selected value of a string selector.</param>
/// <param name="Items">The selected value of a collection selector.</param>
/// <param name="Int64">The selected value of an <see cref="long"/> selector.</param>
/// <param name="Decimal">The selected value of a <see cref="decimal"/> selector.</param>
/// <param name="Boolean">The selected value of a <see cref="bool"/> selector.</param>
/// <param name="Guid">The selected value of a <see cref="System.Guid"/> selector.</param>
/// <param name="Instant">The selected value of a <see cref="DateTimeOffset"/> selector.</param>
/// <param name="Object">The selected value of an <see cref="object"/> selector.</param>
internal sealed record TwinProbeContext(
    string? Text = null,
    IReadOnlyCollection<string>? Items = null,
    long? Int64 = null,
    decimal? Decimal = null,
    bool? Boolean = null,
    Guid? Guid = null,
    DateTimeOffset? Instant = null,
    object? Object = null
);
