namespace TruthWeaver.Evaluation;

/// <summary>
/// Thrown by a <c>TruthWeaver.Testing</c> test double when a test uses it wrongly, for example a
/// <c>FakePredicates.Scripted</c> fake called more often than it has answers. The evaluator does not absorb this
/// exception into <c>Unknown</c> plus a <see cref="Fault"/> (ADR-0001), so the misuse fails the test run instead of
/// passing as an <c>Unknown</c> result.
/// </summary>
/// <remarks>
/// The type is internal and only <c>TruthWeaver.Testing</c> sees it (<c>InternalsVisibleTo</c>), so a production
/// predicate cannot throw it and every exception a production predicate throws is still a fault.
/// </remarks>
/// <param name="message">The message that says what the test did wrong.</param>
internal sealed class TestDoubleMisuseException(string message) : InvalidOperationException(message);
