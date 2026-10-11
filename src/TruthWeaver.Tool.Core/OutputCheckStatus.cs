namespace TruthWeaver.Tool.Core;

/// <summary>How an existing file compares with the output that <see cref="OutputWriter"/> would write.</summary>
public enum OutputCheckStatus
{
    /// <summary>The file holds exactly the bytes that the writer would write.</summary>
    Identical,

    /// <summary>The file exists and holds other bytes.</summary>
    Differs,

    /// <summary>The file does not exist.</summary>
    Missing,
}
