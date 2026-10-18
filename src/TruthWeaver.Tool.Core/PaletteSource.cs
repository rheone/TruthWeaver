namespace TruthWeaver.Tool.Core;

/// <summary>Where an entry of a <see cref="RulePalette"/> comes from.</summary>
public enum PaletteSource
{
    /// <summary>An operator of the language, such as <c>and</c> or <c>atLeast</c>.</summary>
    Operator = 0,

    /// <summary>A constant: <c>True</c>, <c>False</c> or <c>Unknown</c>.</summary>
    Constant = 1,

    /// <summary>A predicate that the predicate schema file declares.</summary>
    SchemaFile = 2,

    /// <summary>A predicate that the open rule uses.</summary>
    Document = 3,

    /// <summary>A ready-made predicate of the <c>TruthWeaver.Predicates</c> package. A host (the terminal UI) adds these; <see cref="RulePalette.Create"/> does not.</summary>
    Catalog = 4,

    /// <summary>
    /// A <c>let</c> definition of the open rule. Its arguments are its parameters, each an expression; the filled entry is
    /// a use of the definition, which only a document with that definition can compile.
    /// </summary>
    Definition = 5,
}
