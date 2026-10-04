# Writing a Custom Extension

Every built-in Markdig feature (pipe tables, task lists, YAML frontmatter) is itself an
`IMarkdownExtension` registered onto the pipeline builder — a custom extension follows the same
contract and plugs into the pipeline the same way a built-in one does.

## The `IMarkdownExtension` contract

```csharp
using Markdig;
using Markdig.Renderers;

public sealed class HighlightExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<HighlightInlineParser>())
        {
            pipeline.InlineParsers.Add(new HighlightInlineParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer &&
            !htmlRenderer.ObjectRenderers.Contains<HighlightInlineRenderer>())
        {
            htmlRenderer.ObjectRenderers.Add(new HighlightInlineRenderer());
        }
    }
}
```

- `Setup(MarkdownPipelineBuilder)` runs when the extension is added to the pipeline and registers
  parsers (block parsers, inline parsers, or both).
- `Setup(MarkdownPipeline, IMarkdownRenderer)` runs once per renderer created against the pipeline
  (typically an `HtmlRenderer`) and registers the corresponding object renderer(s) for the syntax
  node type the parser produces. Guard both registrations with a `.Contains<T>()` check so calling
  `UseHighlight()` more than once on the same builder doesn't double-register.
- Register the extension with a discoverable extension method, matching the built-in naming
  convention:

```csharp
public static class MarkdownPipelineBuilderExtensions
{
    public static MarkdownPipelineBuilder UseHighlight(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready<HighlightExtension>();
        return pipeline;
    }
}
```

`AddIfNotAlready<T>()` is the same idempotency guard the built-in `Use*()` methods use internally,
so chaining `.UseHighlight().UseHighlight()` is harmless.

## Inline parser for a custom syntax (worked example: `==highlight==`)

```csharp
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;

public sealed class HighlightInline : EmphasisInline { }

public sealed class HighlightInlineParser : InlineParser
{
    public HighlightInlineParser() => OpeningCharacters = ['='];

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        // Delimiter-based parsing: Markdig's EmphasisInlineParser is the reference
        // implementation for handling paired == ... == delimiters correctly, including
        // nesting and "can open"/"can close" rules borrowed from CommonMark emphasis parsing.
        // A minimal custom parser typically subclasses or composes EmphasisInlineParser
        // rather than reimplementing delimiter-run detection from scratch.
        return false; // replace with real delimiter matching for production use
    }
}
```

Reach for `EmphasisInlineParser`/`EmphasisInline` as the base when the new syntax is a paired
delimiter (like `**bold**` or `==highlight==`) — Markdig's own bold/italic support is built exactly
this way, and re-deriving delimiter-run matching from scratch is the most error-prone part of
writing a Markdown inline extension.

## Block parser for a custom syntax

For block-level syntax (a construct that occupies whole lines, like a custom admonition block),
implement `BlockParser` and register it via `pipeline.BlockParsers.Add(...)` in `Setup` instead of
`InlineParsers`. `Markdig.Extensions.CustomContainers.CustomContainerParser` (part of the built-in
custom-containers extension, active under `UseAdvancedExtensions()`) is a working reference
implementation for a fenced, attribute-bearing block syntax.

## Common pitfall

Registering a renderer for `HtmlRenderer` only, then rendering through `Markdown.ToPlainText` or a
custom renderer type, produces no output for the custom syntax node — plain-text and other
renderers each need their own registered `IMarkdownObjectRenderer` in `Setup(MarkdownPipeline,
IMarkdownRenderer)`, checked via the renderer's runtime type, if the custom extension needs to
support more than HTML output.
