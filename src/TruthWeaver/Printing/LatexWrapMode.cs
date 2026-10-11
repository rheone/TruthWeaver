namespace TruthWeaver.Printing;

/// <summary>The envelope around an equation in the <see cref="EquationDialect.LaTeX"/> dialect.</summary>
public enum LatexWrapMode
{
    /// <summary>No delimiters. Use this for a raw <c>.tex</c> file or a Pandoc pipeline that adds its own math mode.</summary>
    None,

    /// <summary>The equation between <c>$$</c> delimiters, which GitHub Markdown and most LaTeX tools read as display math.</summary>
    DoubleDollar,

    /// <summary>
    /// The <c>$`...`$</c> form for GitHub Markdown and MathJax. The backticks keep Markdown from reading an underscore
    /// as emphasis. The equation uses only commands in the MathJax subset that GitHub renders.
    /// </summary>
    MathJaxSafe,
}
