namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;

/// <summary>Builds <see cref="ToolResult{T}"/> values for the logic of the workbench, which has no access to the tool core helpers.</summary>
internal static class Results
{
    /// <summary>Creates a result with a value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="diagnostics">The diagnostics to carry (warnings and information), or <see langword="null"/>.</param>
    /// <returns>The result.</returns>
    public static ToolResult<T> Success<T>(T value, IEnumerable<Diagnostic>? diagnostics = null)
        where T : class
    {
        return new ToolResult<T>(value, new EquatableArray<Diagnostic>(diagnostics ?? []));
    }

    /// <summary>Creates a result with no value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="diagnostics">The diagnostics that say why.</param>
    /// <returns>The result.</returns>
    public static ToolResult<T> Failure<T>(IEnumerable<Diagnostic> diagnostics)
        where T : class
    {
        return new ToolResult<T>(null, new EquatableArray<Diagnostic>(diagnostics));
    }
}
