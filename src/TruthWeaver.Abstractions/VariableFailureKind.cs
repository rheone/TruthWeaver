namespace TruthWeaver.Abstractions;

/// <summary>The ways resolving a <see cref="VariableReference"/> can fail at evaluation time (ADR-0006 decision 13).</summary>
public enum VariableFailureKind
{
    /// <summary>A scalar query matched nothing.</summary>
    Missing,

    /// <summary>A scalar query matched more than one node.</summary>
    Ambiguous,

    /// <summary>A match cannot convert to the argument's kind.</summary>
    TypeMismatch,

    /// <summary>The source failed, timed out, threw, or reported a malformed query.</summary>
    SourceError,

    /// <summary>The source name was not supplied to the evaluation.</summary>
    UnsuppliedSource,
}
