---
name: dotnet-markdig
description: Guidance on the Markdig NuGet package (verified current release 1.3.2) — a fast, CommonMark-compliant, extensible Markdown processor for .NET. Covers the MarkdownPipelineBuilder configuration model, UseAdvancedExtensions and individual extension opt-in, commonly used extensions (pipe tables, task lists, auto-links, YAML frontmatter), converting Markdown to HTML with HtmlRenderer/ToHtml and to plain text with ToPlainText, writing a custom IMarkdownExtension, and walking/mutating the MarkdownDocument AST. Use when parsing or rendering Markdown in a .NET project, choosing which Markdig extensions to enable, extracting YAML frontmatter, writing a custom Markdown block/inline parser or renderer, or inspecting/transforming a parsed Markdown AST.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Markdig

Guidance on Markdig, the CommonMark-compliant Markdown processor for .NET built around a
configurable parsing/rendering pipeline. Organized by task, not by Markdig version — the pipeline
builder API and extension model have been stable across releases; each reference file notes a
version-sensitive fact inline where one exists.

## Read this first: package facts

- **Current stable release: 1.3.2** (verified via NuGet Gallery, `nuget.org/packages/Markdig`).
- Targets modern .NET and .NET Standard 2.0/2.1, so it runs on .NET Framework and current .NET
  alike without a separate package variant.
- A separate `Markdig.Signed` package exists for consumers that need a strong-named assembly;
  functionally identical to `Markdig`.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Building a `MarkdownPipeline`, choosing `UseAdvancedExtensions()` vs. opting into extensions individually | [references/pipeline-configuration.md](references/pipeline-configuration.md) |
| Enabling pipe tables, task lists, auto-links, or YAML frontmatter | [references/common-extensions.md](references/common-extensions.md) |
| Converting Markdown to HTML or to plain text | [references/rendering-html-and-text.md](references/rendering-html-and-text.md) |
| Writing a custom block/inline parser or renderer as an `IMarkdownExtension` | [references/custom-extensions.md](references/custom-extensions.md) |
| Walking or mutating the parsed `MarkdownDocument` AST | [references/ast-manipulation.md](references/ast-manipulation.md) |
| Testing code that parses, renders, or transforms Markdown | [references/testing.md](references/testing.md) |

## Quick start

```csharp
using Markdig;

var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()   // pipe tables, task lists, auto-links, footnotes, and more
    .UseYamlFrontMatter()      // not included in UseAdvancedExtensions(); opt in explicitly
    .Build();

string html = Markdown.ToHtml(markdownText, pipeline);
string plainText = Markdown.ToPlainText(markdownText, pipeline);
```

A `MarkdownPipeline` built once should be reused across conversions — it is immutable and
thread-safe after `Build()`, so rebuilding it per call only adds overhead.

## Out of scope

- Markdown editor/preview UI components — this skill covers the parsing/rendering library only,
  not any editor control built on top of it.
- Syntax highlighting of fenced code blocks beyond emitting the `language-*` CSS class Markdig
  attaches — actual highlighting is a separate rendering concern outside Markdig's own output.
- LaTeX/math rendering — Markdig has no built-in math extension; a project needing this integrates
  a separate renderer against the AST described in
  [references/ast-manipulation.md](references/ast-manipulation.md).
