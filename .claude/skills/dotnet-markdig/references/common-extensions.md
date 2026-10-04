# Common Extensions

Markdig ships extensions as opt-in pipeline features. These four cover the majority of real-world
Markdown-to-HTML needs beyond bare CommonMark.

## Pipe tables

```csharp
var pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();
```

```markdown
| Name  | Role      |
| ----- | --------- |
| Ada   | Engineer  |
| Grace | Architect |
```

- Included automatically by `UseAdvancedExtensions()`.
- Column alignment follows the `:---`, `:---:`, `---:` colon placement in the separator row and
  renders as a `style="text-align: ..."` attribute (or `align` attribute, depending on
  `PipeTableOptions`) on the corresponding `<td>`/`<th>` elements.
- A row with fewer cells than the header pads the missing cells as empty; a row with more cells
  truncates the extras — Markdig does not error on a malformed table, it degrades gracefully.
- Grid tables (`UseGridTables()`, also in `UseAdvancedExtensions()`) are a separate, more verbose
  ASCII-art table syntax for cases needing multi-line cell content pipe tables can't express.

## Task lists

```csharp
var pipeline = new MarkdownPipelineBuilder().UseTaskLists().Build();
```

```markdown
- [x] Done item
- [ ] Pending item
```

- Included automatically by `UseAdvancedExtensions()`.
- Renders each item's HTML as `<li class="task-list-item"><input type="checkbox" disabled ... />`
  — the checkbox is disabled by default since rendered HTML has no mechanism to write the state
  back into the source Markdown. Wire up your own interactive checkbox behavior client-side if you
  need round-tripping.
- Only recognized at the start of a list item (`- [ ]` / `- [x]` / `- [X]`); it does not activate
  inside ordinary paragraph text.

## Auto-links

```csharp
var pipeline = new MarkdownPipelineBuilder().UseAutoLinks().Build();
```

```markdown
Visit https://example.com or email someone@example.com for details.
```

- Included automatically by `UseAdvancedExtensions()`.
- Detects bare URLs (`http://`, `https://`, `ftp://`) and email addresses in plain text and
  converts them to `<a>` elements without requiring the CommonMark `<...>` autolink bracket syntax.
- Configure via `AutoLinkOptions` (passed to `UseAutoLinks(options)`) to control which schemes are
  recognized and whether a trailing `www.` prefix (no scheme at all) also triggers detection.

## YAML frontmatter

```csharp
var pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().Build();
```

```markdown
---
title: My Post
tags: [csharp, markdown]
---

# Body content starts here
```

- **Not** included by `UseAdvancedExtensions()` — always call `.UseYamlFrontMatter()` explicitly.
- Parses a leading `---`-delimited block as a `YamlFrontMatterBlock` node in the AST rather than as
  visible document content. By default this block does not render to HTML output at all (it is a
  metadata-only block); the visible HTML starts at the first content after the closing `---`.
- Enabling `UseYamlFrontMatter()` only parses the block into the AST — it does not itself parse the
  YAML content into a typed object. Combine it with a YAML library to deserialize the frontmatter
  block's raw text into a strongly typed model or dictionary, and see
  [ast-manipulation.md](ast-manipulation.md) for extracting the block from the parsed document.

## Choosing extensions deliberately for untrusted input

When rendering Markdown from an untrusted source (user comments, external content) to HTML, enable
only the extensions the product actually needs and pair them with `.DisableHtml()` on the pipeline
builder (see [pipeline-configuration.md](pipeline-configuration.md)) — auto-links and task lists
are generally safe to enable broadly; raw HTML passthrough is the actual injection risk, not the
extension set itself.
