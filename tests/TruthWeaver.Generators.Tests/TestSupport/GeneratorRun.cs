namespace TruthWeaver.Generators.Tests.TestSupport;

using Microsoft.CodeAnalysis;

/// <summary>The outcome of one generator run.</summary>
/// <param name="GeneratorDiagnostics">The diagnostics the generator reported.</param>
/// <param name="CompilerErrors">The compiler errors of the consuming project after the generated source is added.</param>
/// <param name="GeneratedSource">The text of every generated file, joined.</param>
internal sealed record GeneratorRun(
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilerErrors,
    string GeneratedSource
);
