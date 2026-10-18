namespace TruthWeaver.Tool.Core.Configuration;

using TruthWeaver.Diagnostics;

/// <summary>Creates <see cref="ToolResult{T}"/> values, so each operation states its outcome in one expression.</summary>
internal static class ToolResults
{
    /// <summary>Creates a result with a value.</summary>
    public static ToolResult<T> Success<T>(T value, IEnumerable<Diagnostic>? diagnostics = null)
        where T : class
    {
        return new ToolResult<T>(value, new EquatableArray<Diagnostic>(diagnostics ?? []));
    }

    /// <summary>Creates a result with no value.</summary>
    public static ToolResult<T> Failure<T>(IEnumerable<Diagnostic> diagnostics)
        where T : class
    {
        return new ToolResult<T>(null, new EquatableArray<Diagnostic>(diagnostics));
    }

    /// <summary>Creates a result with no value and one error.</summary>
    public static ToolResult<T> Failure<T>(string code, string message, string? path = null)
        where T : class
    {
        return Failure<T>([Error(code, message, path)]);
    }

    /// <summary>Creates an error that has no location in the rule text.</summary>
    public static Diagnostic Error(string code, string message, string? path = null)
    {
        return Diagnostic.Error(code, message, SourceSpan.None, path: path);
    }

    /// <summary>Creates a warning that has no location in the rule text.</summary>
    public static Diagnostic Warning(string code, string message, string? path = null)
    {
        return Diagnostic.Warning(code, message, SourceSpan.None, path: path);
    }
}
