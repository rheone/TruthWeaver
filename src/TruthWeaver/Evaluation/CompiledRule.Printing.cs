namespace TruthWeaver.Evaluation;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Rewriting;

public sealed partial class CompiledRule<TContext>
{
    /// <summary>
    /// Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003), with each of its <see cref="Notes"/> as
    /// a <c>note</c> property on its node, so the JSON compiles back to the same tree and the same notes.
    /// </summary>
    /// <returns>The JSON text.</returns>
    public string PrintJson()
    {
        return this.PrintJson(includeNotes: true);
    }

    /// <summary>Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003), with or without its notes.</summary>
    /// <param name="includeNotes">
    /// <see langword="true"/> to write each of <see cref="Notes"/> as a <c>note</c> property (the lossless form that
    /// <see cref="PrintJson()"/> gives); <see langword="false"/> for the tree alone, for example to compare two rules.
    /// </param>
    /// <returns>The JSON text.</returns>
    public string PrintJson(bool includeNotes)
    {
        return JsonTreePrinter.Print(this.Root, includeNotes ? this.NoteLookup : null);
    }

    /// <summary>Writes this rule to <paramref name="writer"/> as the JSON that <see cref="PrintJson()"/> returns.</summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintJson(TextWriter writer)
    {
        this.PrintJson(writer, includeNotes: true);
    }

    /// <summary>Writes this rule to <paramref name="writer"/> as the JSON that <see cref="PrintJson(bool)"/> returns.</summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="includeNotes"><see langword="true"/> to write the notes, as for <see cref="PrintJson(bool)"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintJson(TextWriter writer, bool includeNotes)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintJson(includeNotes));
    }

    /// <summary>
    /// Builds this rule's outline: its expression tree, recursively. Every operator's label/description (from
    /// <see cref="OperatorInfo"/>) and every term's label/description (from its predicate's registered
    /// <see cref="PredicateSchema"/>), without exposing the underlying closed-set AST types
    /// themselves. Useful for a rule-authoring UI or a generated "what does this rule mean" report.
    /// </summary>
    /// <returns>The root outline node, with every operand outlined the same way.</returns>
    public OutlineNode Outline()
    {
        return OutlineOf(this.Root, this.registry);
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, for a diagram UI. This overload always renders
    /// operator labels in <see cref="OperatorStyle.Word"/>. To choose another style, pass a <see cref="MermaidOptions"/>.
    /// </summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors. <see langword="null"/> (the default) means <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> (the default) for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> (the default) for no compaction.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public string PrintMermaid(
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        return this.PrintMermaid(
            MermaidOptions.From(
                OperatorStyle.Word,
                showArgumentValues,
                direction,
                nodeShapes,
                twoLineTermLabels,
                palette,
                nodeStyle,
                compactChainThreshold
            )
        );
    }

    /// <summary>Renders this rule's structure as Mermaid <c>flowchart</c> text, using <paramref name="options"/>.</summary>
    /// <param name="options">The diagram direction, node shapes, operator style and argument-value switch.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public string PrintMermaid(MermaidOptions options)
    {
        return MermaidTreePrinter.Print(this.Outline(), options);
    }

    /// <summary>
    /// Writes this rule's Mermaid <c>flowchart</c> text to <paramref name="writer"/>, with the operator labels in
    /// <see cref="OperatorStyle.Word"/>. The output is exactly the text that
    /// <see cref="PrintMermaid(bool, MermaidDirection, bool, bool, MermaidPalette?, Func{OutlineNode, NodeStyle?}?, int?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors, or <see langword="null"/> for <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> for no compaction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(
        TextWriter writer,
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(
            this.PrintMermaid(
                showArgumentValues,
                direction,
                nodeShapes,
                twoLineTermLabels,
                palette,
                nodeStyle,
                compactChainThreshold
            )
        );
    }

    /// <summary>
    /// Writes this rule's Mermaid <c>flowchart</c> text to <paramref name="writer"/> using <paramref name="options"/>. The
    /// output is exactly the text that <see cref="PrintMermaid(MermaidOptions)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The diagram options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(TextWriter writer, MermaidOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintMermaid(options));
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, colored by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors. <see langword="null"/> (the default) means <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> (the default) for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> (the default) for no compaction.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    /// <remarks>This overload always renders operator labels in <see cref="OperatorStyle.Word"/>. To choose another style, pass a <see cref="MermaidOptions"/>.</remarks>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintMermaid(
        Decision decision,
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        return this.PrintMermaid(
            decision,
            MermaidOptions.From(
                OperatorStyle.Word,
                showArgumentValues,
                direction,
                nodeShapes,
                twoLineTermLabels,
                palette,
                nodeStyle,
                compactChainThreshold
            )
        );
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, colored by one evaluation, using <paramref name="options"/>.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The diagram direction, node shapes, operator style and argument-value switch.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintMermaid(Decision decision, MermaidOptions options)
    {
        return MermaidTreePrinter.Print(this.Outline(), RequireTraceTree(decision), options);
    }

    /// <summary>
    /// Writes this rule's Mermaid <c>flowchart</c> text, colored by one evaluation, to <paramref name="writer"/>, with the
    /// operator labels in <see cref="OperatorStyle.Word"/>. The output is exactly the text that
    /// <see cref="PrintMermaid(Decision, bool, MermaidDirection, bool, bool, MermaidPalette?, Func{OutlineNode, NodeStyle?}?, int?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors, or <see langword="null"/> for <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> for no compaction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(
        TextWriter writer,
        Decision decision,
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(
            this.PrintMermaid(
                decision,
                showArgumentValues,
                direction,
                nodeShapes,
                twoLineTermLabels,
                palette,
                nodeStyle,
                compactChainThreshold
            )
        );
    }

    /// <summary>
    /// Writes this rule's Mermaid <c>flowchart</c> text, colored by one evaluation, to <paramref name="writer"/> using
    /// <paramref name="options"/>. The output is exactly the text that <see cref="PrintMermaid(Decision, MermaidOptions)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The diagram options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(TextWriter writer, Decision decision, MermaidOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintMermaid(decision, options));
    }

    /// <summary>
    /// Renders this rule as an HTML fragment: one infix formula in nested <c>span</c> elements with the classes
    /// <c>tw-operator</c>, <c>tw-term</c> and <c>tw-constant</c>, HTML entities for the logic symbols and a <c>sub</c> element for
    /// the subscript of a simple variable. The fragment has no <c>script</c> element and no <c>style</c> attribute, and every
    /// term, argument and constant is escaped, so it is safe to put in a page.
    /// </summary>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <returns>The HTML fragment.</returns>
    public string PrintHtml(HtmlOptions? options = null)
    {
        return HtmlPrinter.Print(this.Outline(), options ?? HtmlOptions.Default);
    }

    /// <summary>
    /// Writes this rule as an HTML fragment to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintHtml(HtmlOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintHtml(TextWriter writer, HtmlOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintHtml(options));
    }

    /// <summary>
    /// Renders this rule as an HTML fragment, with the state class <c>tw-true</c>, <c>tw-false</c>, <c>tw-unknown</c> or
    /// <c>tw-skipped</c> added to each node of one evaluation.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <returns>The HTML fragment.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintHtml(Decision decision, HtmlOptions? options = null)
    {
        return HtmlPrinter.Print(this.Outline(), RequireTraceTree(decision), options ?? HtmlOptions.Default);
    }

    /// <summary>
    /// Writes this rule as an HTML fragment, with a state class on each node of one evaluation, to
    /// <paramref name="writer"/>. The output is exactly the text that <see cref="PrintHtml(Decision, HtmlOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintHtml(TextWriter writer, Decision decision, HtmlOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintHtml(decision, options));
    }

    /// <summary>
    /// Renders this rule as a Graphviz DOT <c>digraph</c>: a hexagon for each operator, a rounded box for each term and a circle
    /// for each constant, with one edge from each operator to each operand. The node ids are <c>n0</c>, <c>n1</c> and so on
    /// in depth-first order, and the labels are escaped for DOT.
    /// </summary>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <returns>The DOT text.</returns>
    public string PrintDot(DotOptions? options = null)
    {
        return DotPrinter.Print(this.Outline(), options ?? DotOptions.Default);
    }

    /// <summary>
    /// Writes this rule as a Graphviz DOT <c>digraph</c> to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintDot(DotOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintDot(TextWriter writer, DotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintDot(options));
    }

    /// <summary>
    /// Renders this rule as a Graphviz DOT <c>digraph</c>, with a fill color on each node of one evaluation: green for
    /// <c>True</c>, red for <c>False</c>, yellow for <c>Unknown</c> and gray for a node that short-circuiting skipped.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <returns>The DOT text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintDot(Decision decision, DotOptions? options = null)
    {
        return DotPrinter.Print(this.Outline(), RequireTraceTree(decision), options ?? DotOptions.Default);
    }

    /// <summary>
    /// Writes this rule as a Graphviz DOT <c>digraph</c>, with a fill color on each node of one evaluation, to
    /// <paramref name="writer"/>. The output is exactly the text that <see cref="PrintDot(Decision, DotOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintDot(TextWriter writer, Decision decision, DotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintDot(decision, options));
    }

    /// <summary>Renders this rule's structure as an indented plain-text tree.</summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <returns>The indented tree text.</returns>
    public string PrintPlainText(bool showArgumentValues = true, ConstantFormat? constants = null)
    {
        return PlainTextTreePrinter.Print(this.Outline(), showArgumentValues: showArgumentValues, constants: constants);
    }

    /// <summary>
    /// Writes this rule's structure as an indented plain-text tree to <paramref name="writer"/>. The output is exactly the
    /// text that <see cref="PrintPlainText(bool, ConstantFormat?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintPlainText(TextWriter writer, bool showArgumentValues = true, ConstantFormat? constants = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintPlainText(showArgumentValues, constants));
    }

    /// <summary>
    /// Renders this rule's structure as an indented plain-text tree, annotated by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <returns>The indented tree text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintPlainText(Decision decision, bool showArgumentValues = true, ConstantFormat? constants = null)
    {
        return PlainTextTreePrinter.Print(
            this.Outline(),
            RequireTraceTree(decision),
            showArgumentValues: showArgumentValues,
            constants: constants
        );
    }

    /// <summary>
    /// Writes this rule's structure as an indented plain-text tree, annotated by one evaluation, to
    /// <paramref name="writer"/>. The output is exactly the text that <see cref="PrintPlainText(Decision, bool, ConstantFormat?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintPlainText(
        TextWriter writer,
        Decision decision,
        bool showArgumentValues = true,
        ConstantFormat? constants = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintPlainText(decision, showArgumentValues, constants));
    }

    /// <summary>
    /// Explains one evaluation in a sentence of fixed English, for example
    /// <c>Satisfied because Order Type (kind: "takeout") is true and Over Limit is true.</c>. Only the terms that
    /// decided the result are named, using the label of each predicate and its argument values. When the result is
    /// <see cref="TruthValue.Unknown"/> no term decided it, so the sentence names the terms that were unknown or that
    /// could not be evaluated. The wording has no template engine and no localization surface; use
    /// <see cref="ExplainOptions.Labels"/> to change the label of a predicate.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> for this same rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for the schema labels and no descriptions.</param>
    /// <returns>The sentence, ending with a period.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="decision"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>, or its trace tree does not match the shape of this rule.</exception>
    public string Explain(Decision decision, ExplainOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(decision);
        RuleCoverage coverage = RuleCoverage.FromDecisions(this, [decision]);
        return DecisionExplainer.Explain(
            this.Root,
            decision,
            coverage,
            name => this.registry.TryGetSchema(name, out PredicateSchema? schema) ? schema : null,
            options ?? new ExplainOptions()
        );
    }

    /// <summary>
    /// Renders this rule as a flat, single-line infix equation, for example <c>a ∧ (b ∨ c)</c>. The connectives
    /// <c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>IMPLIES</c>, <c>NAND</c> and <c>NOR</c> print
    /// as infix symbols. Every other operator prints in function-call form, for example <c>AtLeast(2, a, b, c)</c>.
    /// Parentheses appear only around a connective that is an operand of another connective. This is a read view:
    /// <see cref="CanonicalText"/> and the persisted DSL text do not change.
    /// </summary>
    /// <param name="options">The dialect and term options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <returns>The equation text.</returns>
    public string PrintEquation(EquationOptions? options = null)
    {
        return EquationPrinter.Print(this.Root, options ?? EquationOptions.Default);
    }

    /// <summary>
    /// Writes this rule as a flat, single-line infix equation to <paramref name="writer"/>. The output is exactly the text
    /// that <see cref="PrintEquation(EquationOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The dialect and term options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintEquation(TextWriter writer, EquationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintEquation(options));
    }

    /// <summary>
    /// Renders this rule as an equation with a letter for each term, and returns the legend that maps each letter back to
    /// its term. The call always uses simple-variable mode, so <see cref="EquationOptions.SimpleVariables"/> has no
    /// effect here. The legend shows each term as its full call, whatever <see cref="EquationOptions.ShowArgumentValues"/>
    /// says. The lettering is stable: the same rule always gets the same letters.
    /// </summary>
    /// <param name="options">The dialect options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <returns>The equation and its legend, as data and as text.</returns>
    public EquationWithLegend PrintEquationWithLegend(EquationOptions? options = null)
    {
        return EquationPrinter.PrintWithLegend(this.Root, options ?? EquationOptions.Default);
    }

    /// <summary>
    /// Writes this rule as an equation with a letter for each term, followed by its legend, to <paramref name="writer"/>.
    /// The output is the <see cref="EquationWithLegend.Equation"/> of <see cref="PrintEquationWithLegend(EquationOptions?)"/>.
    /// When the rule has at least one term, a <c>\n</c> and the <see cref="EquationWithLegend.LegendText"/> follow. The text
    /// has no trailing newline. For a rule with no term, only the equation is written.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The dialect options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintEquationWithLegend(TextWriter writer, EquationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        EquationWithLegend result = this.PrintEquationWithLegend(options);
        writer.Write(result.Equation);
        if (result.LegendText.Length > 0)
        {
            writer.Write('\n');
            writer.Write(result.LegendText);
        }
    }

    /// <summary>
    /// Prints the truth table of this rule: one row for each assignment of <c>False</c>, <c>Unknown</c> and <c>True</c> to
    /// its distinct terms (3<sup>n</sup> rows), or of <c>False</c> and <c>True</c> only when
    /// <see cref="TruthTableOptions.Classical"/> is set (2<sup>n</sup> rows), with the rule's value for the row. The table
    /// treats each term as a free variable. It calls no predicate and reads no data source, so a rule with variable
    /// references prints the same way. The first column is the first term to occur, and the first column changes slowest.
    /// The values run <c>False</c>, <c>Unknown</c>, <c>True</c>, a layout order and not a number. The text has no trailing line
    /// feed.
    /// </summary>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <returns>The table text.</returns>
    /// <exception cref="InvalidOperationException">
    /// The rule has more terms than <see cref="TruthTableOptions.DefaultTermCap"/> and <see cref="TruthTableOptions.AllowLargerTable"/>
    /// is not set, or it has more terms than <see cref="TruthTableOptions.TermLimit"/>. Call
    /// <see cref="TryPrintTruthTable(TruthTableOptions?, out string?, out IReadOnlyList{Diagnostic})"/> to avoid the exception.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value.</exception>
    public string PrintTruthTable(TruthTableOptions? options = null)
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        this.PrintTruthTable(writer, options);
        return writer.ToString();
    }

    /// <summary>
    /// Writes the truth table of this rule to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintTruthTable(TruthTableOptions?)"/> returns, without a large string that the caller must hold.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The table is refused, as for <see cref="PrintTruthTable(TruthTableOptions?)"/>. Nothing is written in this case.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintTruthTable(TextWriter writer, TruthTableOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (
            !TruthTablePrinter.TryPrint(
                this.Root,
                options ?? TruthTableOptions.Default,
                writer,
                out IReadOnlyList<Diagnostic> diagnostics
            )
        )
        {
            throw new InvalidOperationException(diagnostics[0].Message);
        }
    }

    /// <summary>
    /// Prints the truth table, like <see cref="PrintTruthTable(TruthTableOptions?)"/>, but reports a refused table through
    /// the return value and the diagnostics instead of throwing.
    /// </summary>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <param name="text">The table text, or <see langword="null"/> when this method returns <see langword="false"/>.</param>
    /// <param name="diagnostics">
    /// A <see cref="DiagnosticCodes.TruthTableCapRaised"/> warning when the table has more terms than the default cap and the
    /// options allow it, a <see cref="DiagnosticCodes.TruthTableTooLarge"/> error when the table is refused, or empty.
    /// </param>
    /// <returns><see langword="true"/> unless the table is refused.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value.</exception>
    public bool TryPrintTruthTable(
        TruthTableOptions? options,
        [NotNullWhen(true)] out string? text,
        out IReadOnlyList<Diagnostic> diagnostics
    )
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        bool printed = this.TryPrintTruthTable(writer, options, out diagnostics);
        text = printed ? writer.ToString() : null;
        return printed;
    }

    /// <summary>
    /// Writes the truth table to <paramref name="writer"/>, like <see cref="PrintTruthTable(TextWriter, TruthTableOptions?)"/>,
    /// but reports a refused table through the return value and the diagnostics instead of throwing.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it. Nothing is written when the table is refused.</param>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <param name="diagnostics">As for <see cref="TryPrintTruthTable(TruthTableOptions?, out string?, out IReadOnlyList{Diagnostic})"/>.</param>
    /// <returns><see langword="true"/> unless the table is refused.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public bool TryPrintTruthTable(TextWriter writer, TruthTableOptions? options, out IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return TruthTablePrinter.TryPrint(this.Root, options ?? TruthTableOptions.Default, writer, out diagnostics);
    }

    private static OutlineNode OutlineOf(Expression node, PredicateRegistry<TContext> registry)
    {
        if (node is TermExpression term)
        {
            (string label, string description) = registry.TryGetSchema(term.Identity.PredicateName, out PredicateSchema? schema)
                ? (schema.Label, schema.Description)
                : (term.Identity.PredicateName, "An unregistered predicate (CompilationMode.Lenient).");
            return new OutlineNode(label, description, [], ArgumentText(term.Identity), OutlineNodeKind.Term);
        }

        // A quantifier is not in the closed OperatorDefinitions table (ADR-0010: a third kind, not a connective
        // with a fixed arity), so it is outlined directly instead of through OperatorInfo.Describe, and it carries
        // its bound variable and collection name for the printers to render as part of its head, not as a term's
        // parenthesized argument text.
        if (node is QuantifierExpression quantifier)
        {
            string label = QuantifierText.Label(quantifier.Kind);
            string description =
                quantifier.Kind == QuantifierKind.ForAll
                    ? "Universal quantifier: the generalized AND over a registered collection binding's elements (ADR-0010)."
                    : "Existential quantifier: the generalized OR over a registered collection binding's elements (ADR-0010).";
            return new OutlineNode(
                label,
                description,
                [OutlineOf(quantifier.Body, registry)],
                Kind: OutlineNodeKind.Operator,
                QuantifierVariable: quantifier.Variable,
                QuantifierCollection: quantifier.CollectionName
            );
        }

        OperatorDescriptor descriptor = OperatorInfo.Describe(node);
        IReadOnlyList<Expression> operands = (node is ConstantExpression) ? [] : ExpressionShape.Of(node).Operands;

        return new OutlineNode(
            descriptor.Label,
            descriptor.Description,
            [.. operands.Select(operand => OutlineOf(operand, registry))],
            Kind: node is ConstantExpression ? OutlineNodeKind.Constant : OutlineNodeKind.Operator
        );
    }

    /// <summary>
    /// Renders a term's rule-text arguments as comma-joined <c>name: value</c> pairs, matching the
    /// per-argument formatting <see cref="TermIdentity.ToString"/> uses for its parenthesized part, but
    /// without repeating the predicate name. That comes from the term's own
    /// <see cref="OutlineNode.Label"/> instead.
    /// </summary>
    /// <param name="identity">The term's identity.</param>
    /// <returns>The joined argument text, or <see langword="null"/> for a zero-argument term.</returns>
    private static string? ArgumentText(TermIdentity identity)
    {
        return identity.FormatArguments();
    }

    private static TraceNode RequireTraceTree(Decision decision)
    {
        return decision.TraceTree
            ?? throw new ArgumentException(
                "This decision has no TraceTree to render - it must come from EvaluateAsync on this same rule.",
                nameof(decision)
            );
    }
}
