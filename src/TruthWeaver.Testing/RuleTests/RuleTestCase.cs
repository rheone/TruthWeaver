namespace TruthWeaver.Testing.RuleTests;

using TruthWeaver.Testing.Fakes;

/// <summary>One named case of a rule test file: the stub values and data it gives, and what it expects.</summary>
/// <param name="Name">The name of the case, unique in its file.</param>
/// <param name="Given">The stub value of each term. A term with no value is <see cref="TruthValue.Unknown"/>.</param>
/// <param name="Variables">The data source of each <c>from("source", "query")</c> source name. A source with no entry is a fault.</param>
/// <param name="Expect">What the case expects.</param>
public sealed record RuleTestCase(
    string Name,
    StubValues Given,
    IReadOnlyDictionary<string, IDataSource> Variables,
    RuleTestExpectation Expect
);
