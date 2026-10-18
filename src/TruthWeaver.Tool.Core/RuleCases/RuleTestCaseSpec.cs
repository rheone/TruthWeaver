namespace TruthWeaver.Tool.Core.RuleCases;

using System.Text.Json.Nodes;

/// <summary>
/// One case of a rule test file: the assignment of the case and the result it expects. It holds the plain data of the case,
/// so a front end can save an assignment as a case and load a case back into an assignment, without the rule test runner.
/// </summary>
/// <param name="Name">The name of the case. Each name is used once in a file.</param>
/// <param name="Given">The value of each term, by the canonical text of the term. A term that is not here is <see cref="TruthValue.Unknown"/>.</param>
/// <param name="Variables">The data for the <c>from(...)</c> references: a JSON object that maps each source name to its data. It is empty when the case has no data.</param>
/// <param name="ExpectedResult">The result that the case expects.</param>
/// <param name="ExpectedFaults">The number of faults that the case expects, or <see langword="null"/> when the case does not state it.</param>
public sealed record RuleTestCaseSpec(
    string Name,
    IReadOnlyDictionary<string, TruthValue> Given,
    JsonObject Variables,
    TruthValue ExpectedResult,
    int? ExpectedFaults
);
