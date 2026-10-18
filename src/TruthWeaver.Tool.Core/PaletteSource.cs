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
}
