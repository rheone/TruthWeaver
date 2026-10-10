namespace TruthWeaver.Yaml;

using TruthWeaver.Ast;
using TruthWeaver.Printing;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Prints a compiled expression tree to the identical flat, key-discriminated tree shape JSON uses
/// (ADR-0003), expressed in YAML (ticket 08) — the YAML-direction half of the round-trip guarantee.
/// String-kind literals are always emitted double-quoted so a value that happens to read as
/// <c>true</c>/<c>false</c>/a number (e.g. a role code of <c>"1"</c>) never gets reparsed as a
/// different literal kind.
/// </summary>
internal static class YamlTreePrinter
{
    /// <summary>Prints an expression tree to YAML tree text.</summary>
    /// <param name="root">The tree to print.</param>
    /// <returns>The YAML text.</returns>
    public static string Print(Expression root)
    {
        YamlNodeWriter nodeWriter = new();
        TreeFormatEmitter.Emit(root, nodeWriter);
        YamlDocument document = new(nodeWriter.Result!);
        YamlStream stream = [with(document)];
        using StringWriter writer = new();
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }
}
