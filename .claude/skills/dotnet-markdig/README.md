# Markdig

Guidance on Markdig, the CommonMark-compliant Markdown processor for .NET built around a
configurable parsing/rendering pipeline, covering pipeline setup, common extensions, rendering,
custom extensions, and AST manipulation.

## When to reach for it

- Building a `MarkdownPipeline` and deciding between `UseAdvancedExtensions()` and opting into
  extensions individually.
- Enabling pipe tables, task lists, auto-links, or YAML frontmatter in parsed Markdown.
- Converting Markdown to HTML or to plain text.
- Writing a custom block or inline parser/renderer as an `IMarkdownExtension`.
- Walking or mutating the parsed `MarkdownDocument` AST directly.

## Using it

This skill is model-invoked: it fires automatically when you're parsing or rendering Markdown in a
.NET project, choosing Markdig extensions, or inspecting a parsed AST. You can also invoke it
directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| MarkdownPipelineBuilder, UseAdvancedExtensions(), opting into extensions individually, Build() | [references/pipeline-configuration.md](references/pipeline-configuration.md) |
| Pipe tables, task lists, auto-links, YAML frontmatter | [references/common-extensions.md](references/common-extensions.md) |
| Markdown.ToHtml, HtmlRenderer, Markdown.ToPlainText | [references/rendering-html-and-text.md](references/rendering-html-and-text.md) |
| IMarkdownExtension, custom block/inline parsers and renderers | [references/custom-extensions.md](references/custom-extensions.md) |
| MarkdownDocument, Descendants\<T>(), walking and mutating the parsed tree | [references/ast-manipulation.md](references/ast-manipulation.md) |
| Testing Markdown-to-HTML/text conversions and custom extensions | [references/testing.md](references/testing.md) |

## Example prompts

- "Set up a Markdig pipeline with pipe tables and YAML frontmatter enabled."
- "Extract the YAML frontmatter from this Markdown file before rendering it."
- "Write a custom Markdig extension that turns `:emoji:` shortcodes into Unicode characters."
