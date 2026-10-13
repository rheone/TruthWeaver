namespace TruthWeaver.Yaml;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;

/// <summary>
/// Compiles, prints and converts a rule in any of the three formats chosen by a <see cref="RuleFormat"/> value. They are
/// here, not on the core types, because this is the one package that reads and writes all three formats.
/// </summary>
/// <remarks>
/// A conversion compiles the source and prints the compiled rule in the target format, so it is lossless: the result
/// compiles to a tree equal to the source's, with the same notes. Rule text is printed as <c>PrintRuleText()</c> prints
/// it, not as <see cref="CompiledRule{TContext}.CanonicalText"/>, so its notes are kept. Comments in a YAML source (<c>#</c>) and the
/// layout of any source are not part of the rule, so a conversion does not keep them.
/// </remarks>
public static class RuleFormatExtensions
{
    /// <summary>Compiles <paramref name="source"/> as <paramref name="format"/>.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the rule against.</param>
    /// <param name="source">The rule in <paramref name="format"/>.</param>
    /// <param name="format">The format of <paramref name="source"/>.</param>
    /// <returns>The compilation result: the same result as <c>Compile</c>, <c>CompileJson</c> or <c>CompileYaml</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compiler"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static CompilationResult<TContext> CompileAs<TContext>(
        this RuleCompiler<TContext> compiler,
        string source,
        RuleFormat format
    )
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(source);

        return format switch
        {
            RuleFormat.Text => compiler.Compile(source),
            RuleFormat.Json => compiler.CompileJson(source),
            RuleFormat.Yaml => compiler.CompileYaml(source),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a defined rule format."),
        };
    }

    /// <summary>Compiles <paramref name="source"/> as <paramref name="format"/> without a result object: the <c>Try</c> form of <see cref="CompileAs{TContext}"/>.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the rule against.</param>
    /// <param name="source">The rule in <paramref name="format"/>.</param>
    /// <param name="format">The format of <paramref name="source"/>.</param>
    /// <param name="rule">The compiled rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compiler"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static bool TryCompileAs<TContext>(
        this RuleCompiler<TContext> compiler,
        string source,
        RuleFormat format,
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics
    )
    {
        return compiler.CompileAs(source, format).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>
    /// Prints <paramref name="rule"/> as <paramref name="format"/>: the text of <c>PrintRuleText</c>, <c>PrintJson</c> or
    /// <c>PrintYaml</c>, with or without the rule's notes.
    /// </summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <param name="format">The format to print.</param>
    /// <param name="includeNotes">
    /// <see langword="true"/> (the default) to write <see cref="CompiledRule{TContext}.Notes"/>, so the text compiles back to
    /// the same notes; <see langword="false"/> for the tree alone. For <see cref="RuleFormat.Text"/> without notes the
    /// result is <see cref="CompiledRule{TContext}.CanonicalText"/>.
    /// </param>
    /// <returns>The rule in <paramref name="format"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static string PrintAs<TContext>(this CompiledRule<TContext> rule, RuleFormat format, bool includeNotes = true)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return format switch
        {
            RuleFormat.Text => includeNotes ? rule.PrintRuleText() : rule.CanonicalText,
            RuleFormat.Json => rule.PrintJson(includeNotes),
            RuleFormat.Yaml => rule.PrintYaml(includeNotes),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a defined rule format."),
        };
    }

    /// <summary>
    /// Converts a rule from one format to another: compiles <paramref name="source"/> as <paramref name="from"/> and prints
    /// the compiled rule as <paramref name="to"/>. The result compiles to a tree equal to the source's, with the same
    /// notes. <paramref name="from"/> and <paramref name="to"/> may be the same format, which tidies the source into the
    /// printer's layout.
    /// </summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the rule against. The source must compile with it.</param>
    /// <param name="source">The rule in <paramref name="from"/>.</param>
    /// <param name="from">The format of <paramref name="source"/>.</param>
    /// <param name="to">The format to convert to.</param>
    /// <param name="includeNotes"><see langword="true"/> (the default) to keep the notes, as for <see cref="PrintAs{TContext}"/>.</param>
    /// <returns>The converted text, or no text and the diagnostics when the source does not compile.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compiler"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="from"/> or <paramref name="to"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static RuleConversionResult Convert<TContext>(
        this RuleCompiler<TContext> compiler,
        string source,
        RuleFormat from,
        RuleFormat to,
        bool includeNotes = true
    )
    {
        // An undefined target is a programming error; say so before compiling, whatever the source holds.
        if (!Enum.IsDefined(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to), to, "Not a defined rule format.");
        }

        CompilationResult<TContext> compiled = compiler.CompileAs(source, from);
        return new RuleConversionResult(compiled.Rule?.PrintAs(to, includeNotes), compiled.Diagnostics);
    }

    /// <summary>Converts a rule from one format to another without a result object: the <c>Try</c> form of <see cref="Convert{TContext}"/>.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the rule against.</param>
    /// <param name="source">The rule in <paramref name="from"/>.</param>
    /// <param name="from">The format of <paramref name="source"/>.</param>
    /// <param name="to">The format to convert to.</param>
    /// <param name="converted">The converted text when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised while compiling the source, including warnings on success.</param>
    /// <param name="includeNotes"><see langword="true"/> (the default) to keep the notes.</param>
    /// <returns><see langword="true"/> when the source compiled and was converted; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compiler"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="from"/> or <paramref name="to"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static bool TryConvert<TContext>(
        this RuleCompiler<TContext> compiler,
        string source,
        RuleFormat from,
        RuleFormat to,
        [NotNullWhen(true)] out string? converted,
        out EquatableArray<Diagnostic> diagnostics,
        bool includeNotes = true
    )
    {
        RuleConversionResult result = compiler.Convert(source, from, to, includeNotes);
        converted = result.Text;
        diagnostics = result.Diagnostics;
        return result.Succeeded;
    }
}
