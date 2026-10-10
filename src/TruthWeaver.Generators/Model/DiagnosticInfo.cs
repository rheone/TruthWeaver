namespace TruthWeaver.Generators.Model;

using Microsoft.CodeAnalysis;

/// <summary>A diagnostic to report, kept as plain values so the pipeline can cache it.</summary>
/// <param name="Descriptor">The descriptor of the diagnostic. Descriptors are static, so reference equality holds.</param>
/// <param name="Location">The source location, or <see langword="null"/> for none.</param>
/// <param name="Arguments">The message format arguments.</param>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> Arguments)
{
    /// <summary>Creates a diagnostic to report later.</summary>
    /// <param name="descriptor">The descriptor of the diagnostic.</param>
    /// <param name="location">The source location.</param>
    /// <param name="arguments">The message format arguments.</param>
    /// <returns>The diagnostic.</returns>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(location), new EquatableArray<string>([.. arguments]));
    }

    /// <summary>Creates the Roslyn diagnostic.</summary>
    /// <returns>The diagnostic.</returns>
    public Diagnostic ToDiagnostic()
    {
        return Diagnostic.Create(this.Descriptor, this.Location?.ToLocation(), [.. this.Arguments.Items]);
    }
}
