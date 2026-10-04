namespace TruthWeaver.Predicates.Tests;

/// <summary>A context with one selected value per kind. Each value is missing unless a test sets it.</summary>
/// <param name="Text">The selected value of a string selector.</param>
/// <param name="Items">The selected value of a collection selector.</param>
/// <param name="Int64">The selected value of a <see cref="long"/> selector.</param>
/// <param name="Decimal">The selected value of a <see cref="decimal"/> selector.</param>
/// <param name="Flag">The selected value of a <see cref="bool"/> selector.</param>
/// <param name="Id">The selected value of a <see cref="Guid"/> selector.</param>
/// <param name="At">The selected value of a <see cref="DateTimeOffset"/> selector.</param>
internal sealed record Selections(
    string? Text = null,
    IReadOnlyCollection<string>? Items = null,
    long? Int64 = null,
    decimal? Decimal = null,
    bool? Flag = null,
    Guid? Id = null,
    DateTimeOffset? At = null
);
