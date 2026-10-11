namespace TruthWeaver.Generators.Model;

/// <summary>The supported return types of a predicate method.</summary>
internal enum ReturnShape
{
    /// <summary><c>TruthValue</c>: the delegate wraps the result in a completed <c>ValueTask</c>.</summary>
    TruthValue,

    /// <summary><c>ValueTask&lt;TruthValue&gt;</c>: the delegate returns the result as it is.</summary>
    ValueTask,

    /// <summary><c>Task&lt;TruthValue&gt;</c>: the delegate wraps the task in a <c>ValueTask</c>.</summary>
    Task,
}
