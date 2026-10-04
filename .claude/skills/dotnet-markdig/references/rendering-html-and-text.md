# Rendering to HTML and Plain Text

Markdig separates parsing (Markdown text → `MarkdownDocument` AST) from rendering (AST → output
format). The static `Markdown` class offers convenience methods that do both steps in one call for
the two most common output formats.

## Converting to HTML

```csharp
using Markdig;

string html = Markdown.ToHtml(markdownText, pipeline);
```

For more control over the render (writing into an existing `TextWriter`, e.g. an ASP.NET Core
response stream, without allocating an intermediate string):

```csharp
using Markdig.Renderers;

var writer = new StringWriter();
var renderer = new HtmlRenderer(writer);
pipeline.Setup(renderer);

MarkdownDocument document = Markdown.Parse(markdownText, pipeline);
renderer.Render(document);
writer.Flush();

string html = writer.ToString();
```

- `pipeline.Setup(renderer)` is required before rendering with a manually constructed renderer —
  it registers each active extension's renderer hooks (e.g. the pipe-table extension's `<table>`
  writer) onto that specific `HtmlRenderer` instance. Skipping this step means extension syntax
  parses correctly into the AST but renders as nothing or as raw text.
- `HtmlRenderer` exposes rendering options (`UseNonAsciiNoEscape`, `ImplicitParagraph`) as public
  properties settable before calling `Render` — these affect only the render step, not parsing, so
  the same parsed `MarkdownDocument` can be rendered multiple times with different renderer
  settings without re-parsing.

## Converting to plain text

```csharp
string plainText = Markdown.ToPlainText(markdownText, pipeline);
```

- Strips Markdown syntax markers and HTML tags, leaving the human-readable text content — useful
  for generating search-index content, notification previews, or plain-text email fallbacks from a
  Markdown source of truth.
- Internally still parses to a `MarkdownDocument` and renders through a text-only renderer, so the
  same pipeline (with the same extensions enabled) governs what `ToPlainText` recognizes as
  structure to strip, just as it does for `ToHtml`.

## Normalizing Markdown back to Markdown

```csharp
using Markdig.Renderers.Normalize;

string normalized = Markdown.Normalize(markdownText, options: null, pipeline: pipeline);
```

Useful for auto-formatting Markdown source consistently (e.g. a pre-commit formatting step for
documentation files) — parses and re-emits Markdown syntax rather than HTML, normalizing
whitespace, list markers, and heading styles according to `NormalizeOptions`.

## Common pitfall

Calling `Markdown.Parse` once and then passing the resulting `MarkdownDocument` through
`Markdown.ToHtml(document, pipeline)` (the document overload) versus calling `Markdown.ToHtml(text,
pipeline)` (the string overload, which parses internally) produces identical output — but if you
need to inspect or mutate the AST between parsing and rendering (see
[ast-manipulation.md](ast-manipulation.md)), you must call `Markdown.Parse` yourself and use the
document overload; the string overload gives no hook to intercept the parsed tree.
