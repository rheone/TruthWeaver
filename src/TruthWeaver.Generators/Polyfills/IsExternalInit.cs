// IDE0130 (namespace does not match folder): the compiler looks for this exact namespace.
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices;

#pragma warning restore IDE0130

// S2094 (empty class): the compiler looks for the type by name only, so the type has no members by design.
#pragma warning disable S2094

/// <summary>
/// Lets the netstandard2.0 generator declare records and <see langword="init"/> accessors. The compiler needs a type
/// with this name, and netstandard2.0 does not have one.
/// </summary>
internal static class IsExternalInit;
#pragma warning restore S2094
