namespace TruthWeaver.Printing;

/// <summary>The text format that <see cref="Evaluation.CompiledRule{TContext}.PrintTruthTable(TruthTableOptions?)"/> writes a truth table in.</summary>
public enum TruthTableFormat
{
    /// <summary>A GitHub-flavored Markdown pipe table with a header row. The characters that Markdown treats as markup are escaped.</summary>
    Markdown,

    /// <summary>Comma-separated values with a header row. A field with a comma, a quote or a line break is quoted, and rows end with a line feed.</summary>
    Csv,

    /// <summary>A JSON object with the logic, the terms and the rows.</summary>
    Json,

    /// <summary>An HTML <c>table</c> element with the classes <c>tw-true</c>, <c>tw-false</c> and <c>tw-unknown</c> on the cells.</summary>
    Html,

    /// <summary>A LaTeX <c>tabular</c> environment with the cells in math mode.</summary>
    Latex,
}
