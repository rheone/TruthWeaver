namespace TruthWeaver.Tests;

using System.Reflection;
using TruthWeaver.Ast;

/// <summary>
/// Replaces one operator's display label in the live <see cref="OperatorDefinitions"/> table for the life of the
/// returned scope. The table exposes no setter, so the backing dictionary is reached by reflection, the same
/// approach <see cref="HostTimeZone"/> uses for <see cref="TimeZoneInfo.Local"/>. Every caller runs in the
/// <see cref="OperatorLabelMutationCollection"/> collection, because the table is process-wide state.
/// </summary>
internal static class OperatorLabel
{
    /// <summary>
    /// Makes <paramref name="opName"/> show <paramref name="label"/> until the scope is disposed, which restores it.
    /// Only patches the <c>ByOpName</c> lookup (what <c>TraceText.LabelOf</c> and the evaluator read); the table's
    /// <c>ByLabel</c> and <c>BySymbol</c> lookups keep the original label for the life of the scope, so a caller
    /// that resolves a definition by its old or new label rather than by op-name during the swap sees stale data.
    /// </summary>
    /// <param name="opName">The operator's canonical op-name (a <see cref="NodeShape.OpName"/> value), e.g. <c>"Parity"</c>.</param>
    /// <param name="label">The label to use while the scope is active.</param>
    /// <returns>A scope that restores the previous label.</returns>
    public static IDisposable Swap(string opName, string label)
    {
        Dictionary<string, OperatorDefinition> byOpName = ByOpName();
        OperatorDefinition original = byOpName[opName];
        byOpName[opName] = original with { LabelTemplate = label };
        return new Scope(byOpName, opName, original);
    }

    private static Dictionary<string, OperatorDefinition> ByOpName()
    {
        FieldInfo field =
            typeof(OperatorDefinitions).GetField("ByOpName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("OperatorDefinitions.ByOpName is missing.");
        return (Dictionary<string, OperatorDefinition>)field.GetValue(null)!;
    }

    private sealed class Scope(Dictionary<string, OperatorDefinition> byOpName, string opName, OperatorDefinition original)
        : IDisposable
    {
        public void Dispose()
        {
            byOpName[opName] = original;
        }
    }
}
