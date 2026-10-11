namespace TruthWeaver.Printing;

using TruthWeaver.Evaluation;

/// <summary>
/// A fluent way to make a <see cref="MermaidOptions"/>. Each <c>With</c> method sets one option and
/// returns the builder. <see cref="Build"/> returns the options. The builder holds no rendering logic: it
/// sets the same properties that an object initializer sets.
/// </summary>
/// <example>
/// <code>
/// MermaidOptions options = new MermaidOptionsBuilder()
///     .WithDirection(MermaidDirection.LeftRight)
///     .WithNodeShapes()
///     .Build();
/// </code>
/// </example>
public sealed class MermaidOptionsBuilder
{
    private MermaidOptions options = new();

    /// <summary>Sets <see cref="MermaidOptions.Direction"/>.</summary>
    /// <param name="direction">The layout direction of the flowchart.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithDirection(MermaidDirection direction)
    {
        this.options = this.options with { Direction = direction };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.NodeShapes"/>.</summary>
    /// <param name="enabled">Whether each node role gets its own shape. Defaults to <see langword="true"/>.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithNodeShapes(bool enabled = true)
    {
        this.options = this.options with { NodeShapes = enabled };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.OperatorStyle"/>.</summary>
    /// <param name="operatorStyle">How operator labels are rendered.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithOperatorStyle(OperatorStyle operatorStyle)
    {
        this.options = this.options with { OperatorStyle = operatorStyle };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.ShowArgumentValues"/>.</summary>
    /// <param name="enabled">Whether a term's argument values appear in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithArgumentValues(bool enabled = true)
    {
        this.options = this.options with { ShowArgumentValues = enabled };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.TwoLineTermLabels"/>.</summary>
    /// <param name="enabled">Whether a term renders as two lines. Defaults to <see langword="true"/>.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithTwoLineTermLabels(bool enabled = true)
    {
        this.options = this.options with { TwoLineTermLabels = enabled };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.Palette"/>.</summary>
    /// <param name="palette">The state, highlight and mute colors.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="palette"/> is <see langword="null"/>.</exception>
    public MermaidOptionsBuilder WithPalette(MermaidPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        this.options = this.options with { Palette = palette };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.NodeStyle"/>.</summary>
    /// <param name="nodeStyle">A callback that picks a style for a node, or <see langword="null"/> for none.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithNodeStyle(Func<OutlineNode, NodeStyle?>? nodeStyle)
    {
        this.options = this.options with { NodeStyle = nodeStyle };
        return this;
    }

    /// <summary>Sets <see cref="MermaidOptions.CompactChainThreshold"/>.</summary>
    /// <param name="threshold">The operand count above which a flat chain is boxed, or <see langword="null"/> for no compaction.</param>
    /// <returns>This builder.</returns>
    public MermaidOptionsBuilder WithCompactChainThreshold(int? threshold)
    {
        this.options = this.options with { CompactChainThreshold = threshold };
        return this;
    }

    /// <summary>Returns the options that the calls so far describe.</summary>
    /// <returns>An immutable <see cref="MermaidOptions"/>.</returns>
    public MermaidOptions Build()
    {
        return this.options;
    }
}
