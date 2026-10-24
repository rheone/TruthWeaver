namespace TruthWeaver.Yaml;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Adds YAML tree support to <see cref="RuleCompiler{TContext}"/> and <see cref="CompiledRule{TContext}"/>,
/// isolated in this package so a consumer with no interest in YAML never acquires the YamlDotNet
/// dependency transitively (ADR-0004).
/// </summary>
public static class YamlRuleExtensions
{
    /// <summary>Compiles the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in YAML.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the parsed tree against.</param>
    /// <param name="yaml">The YAML tree text.</param>
    /// <returns>The compilation result.</returns>
    public static CompilationResult<TContext> CompileYaml<TContext>(this RuleCompiler<TContext> compiler, string yaml)
    {
        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = YamlTreeParser.Parse(yaml, compiler.Options.MaxDepth);
        return CompileFromParsedYaml(compiler, root, diagnostics);
    }

    /// <summary>
    /// Compiles the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in
    /// YAML, from a <see cref="YamlNode"/> already extracted from a larger document (e.g. one mapping
    /// node of a multi-rule document parsed with YamlDotNet), rather than requiring the caller to
    /// re-serialize it to standalone YAML text first. Extracting the subtree is the caller's own
    /// navigation via YamlDotNet's node APIs; the engine adds no path/pointer syntax of its own
    /// (ADR-0003).
    /// </summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the parsed tree against.</param>
    /// <param name="node">The YAML tree node.</param>
    /// <returns>The compilation result.</returns>
    public static CompilationResult<TContext> CompileYaml<TContext>(this RuleCompiler<TContext> compiler, YamlNode node)
    {
        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = YamlTreeParser.Parse(node, compiler.Options.MaxDepth);
        return CompileFromParsedYaml(compiler, root, diagnostics);
    }

    /// <summary>Compiles the YAML tree shape without a result object: the <c>Try</c> form of <see cref="CompileYaml{TContext}(RuleCompiler{TContext}, string)"/>.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the parsed tree against.</param>
    /// <param name="yaml">The YAML tree text.</param>
    /// <param name="rule">The compiled rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public static bool TryCompileYaml<TContext>(
        this RuleCompiler<TContext> compiler,
        string yaml,
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics
    )
    {
        return compiler.CompileYaml(yaml).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>Compiles the YAML tree shape from a <see cref="YamlNode"/> without a result object: the <c>Try</c> form of <see cref="CompileYaml{TContext}(RuleCompiler{TContext}, YamlNode)"/>.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the parsed tree against.</param>
    /// <param name="node">The YAML tree node.</param>
    /// <param name="rule">The compiled rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public static bool TryCompileYaml<TContext>(
        this RuleCompiler<TContext> compiler,
        YamlNode node,
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics
    )
    {
        return compiler.CompileYaml(node).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>
    /// Prints a compiled rule to the flat, key-discriminated YAML tree shape (ADR-0003), with each of its
    /// <see cref="CompiledRule{TContext}.Notes"/> as a <c>note</c> key on its node, so the YAML compiles back to the same
    /// tree and the same notes.
    /// </summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <returns>The YAML text.</returns>
    public static string PrintYaml<TContext>(this CompiledRule<TContext> rule)
    {
        return rule.PrintYaml(includeNotes: true);
    }

    /// <summary>Prints a compiled rule to the flat, key-discriminated YAML tree shape (ADR-0003), with or without its notes.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <param name="includeNotes">
    /// <see langword="true"/> to write each of <see cref="CompiledRule{TContext}.Notes"/> as a <c>note</c> key (the lossless
    /// form that <see cref="PrintYaml{TContext}(CompiledRule{TContext})"/> gives); <see langword="false"/> for the tree alone.
    /// </param>
    /// <returns>The YAML text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static string PrintYaml<TContext>(this CompiledRule<TContext> rule, bool includeNotes)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return YamlTreePrinter.Print(rule.Root, includeNotes ? rule.NoteLookup : null);
    }

    /// <summary>Writes a compiled rule to <paramref name="writer"/> as the YAML that <see cref="PrintYaml{TContext}(CompiledRule{TContext})"/> returns.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public static void PrintYaml<TContext>(this CompiledRule<TContext> rule, TextWriter writer)
    {
        rule.PrintYaml(writer, includeNotes: true);
    }

    /// <summary>Writes a compiled rule to <paramref name="writer"/> as the YAML that <see cref="PrintYaml{TContext}(CompiledRule{TContext}, bool)"/> returns.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="includeNotes"><see langword="true"/> to write the notes, as for <see cref="PrintYaml{TContext}(CompiledRule{TContext}, bool)"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public static void PrintYaml<TContext>(this CompiledRule<TContext> rule, TextWriter writer, bool includeNotes)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(rule.PrintYaml(includeNotes));
    }

    private static CompilationResult<TContext> CompileFromParsedYaml<TContext>(
        RuleCompiler<TContext> compiler,
        RuleNode? root,
        IReadOnlyList<Diagnostic> diagnostics
    )
    {
        return root is null
            ? new CompilationResult<TContext>(null, new EquatableArray<Diagnostic>(diagnostics))
            : compiler.CompileFromNode(root, diagnostics);
    }
}
