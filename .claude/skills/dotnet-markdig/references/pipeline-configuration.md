# Pipeline Configuration

Every Markdig operation — parsing, rendering, or both — runs against a `MarkdownPipeline` you build
once with a `MarkdownPipelineBuilder`. The pipeline is where you decide which CommonMark
extensions are active; without any configuration, Markdig parses plain CommonMark only.

## The builder shape

```csharp
using Markdig;

MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .Build();
```

- `MarkdownPipelineBuilder` exposes one `Use*()` extension method per feature (`UsePipeTables()`,
  `UseTaskLists()`, `UseAutoLinks()`, `UseYamlFrontMatter()`, and dozens more) plus the
  `UseAdvancedExtensions()` convenience bundle.
- `.Build()` produces an immutable, thread-safe `MarkdownPipeline`. Build it once (a static field
  or a singleton registered in DI) and reuse it across every parse/render call in the process —
  rebuilding it per call re-runs extension registration for no benefit.
- Chaining `Use*()` calls is idempotent for most extensions: calling the same one twice does not
  register it twice, so composing pipelines from smaller helper methods that each call their own
  subset of `Use*()` is safe.

## `UseAdvancedExtensions()` scope

`UseAdvancedExtensions()` enables the bundle of extensions considered safe defaults for rendering
arbitrary user/document Markdown to HTML: pipe tables, grid tables, task lists, auto-links,
footnotes, definition lists, abbreviations, citations, custom containers, figures, generic
attributes, media links, smarty pants (typographic quotes/dashes off by default — see below), and
more.

**Not included in `UseAdvancedExtensions()`** — these need an explicit `Use*()` call because they
change output in ways not universally desired:

- **YAML frontmatter** (`UseYamlFrontMatter()`) — parses a leading `---` fenced block as metadata
  rather than document content, but does not render it to HTML; see
  [ast-manipulation.md](ast-manipulation.md) for reading the parsed block.
- **Emoji and Smiley replacement** (`UseEmojiAndSmiley()`) — replaces `:smile:`-style shortcodes
  and ASCII smileys, which is not always wanted for technical documentation.
- **Soft line breaks as hard line breaks** (`UseSoftlineBreakAsHardlineBreak()`) — changes
  CommonMark's default line-break semantics, which not every consumer wants.
- **Bootstrap** (`UseBootstrap()`) — adds Bootstrap CSS classes to rendered elements (tables,
  blockquotes), which only makes sense when the target page uses Bootstrap.
- **JIRA links** (`UseJiraLinks(options)`) — requires a JIRA project key/URL to configure, so it
  cannot be a no-argument default.
- **SmartyPants** (`UseSmartyPants()`) — converts straight quotes/dashes to typographic equivalents,
  which alters the literal text of the document.

## Selecting extensions individually

For a pipeline that needs precise control (e.g. accepting user-submitted Markdown where certain
extensions are a deliberate security or formatting boundary), call only the specific `Use*()`
methods needed instead of `UseAdvancedExtensions()`:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UsePipeTables()
    .UseTaskLists()
    .UseAutoLinks()
    .Build();
```

## Pipeline extensions and document metadata

`MarkdownPipelineBuilder` also carries non-extension configuration relevant to how parsing behaves:

- `.DisableHtml()` — strips raw inline/block HTML from user-submitted Markdown rather than passing
  it through to rendered output verbatim. Use this whenever the Markdown source is untrusted input
  and the render target is HTML — otherwise raw `<script>` tags in the source pass straight through.
- `.UseReferralLinks("nofollow noopener")` — appends `rel` attribute values to rendered links,
  useful for user-generated content where outbound links need `nofollow`/`noopener` for security or
  SEO reasons.
- `.Extensions` — the builder's collection of registered `IMarkdownExtension` instances; inspect it
  to check whether a given extension is already active before adding a custom one that might
  conflict (see [custom-extensions.md](custom-extensions.md)).

## Common pitfall

Building a fresh `MarkdownPipeline` per request (e.g. inside an ASP.NET Core action method) works
correctly but wastes the cost of extension registration and internal parser setup on every call.
Register the built pipeline once — as a singleton service, a static readonly field, or a
DI-injected `MarkdownPipeline` instance — and pass that same instance into every `Markdown.ToHtml`/
`Markdown.Parse` call.
