# Testing

Markdig's pipeline is a pure function of (input text, pipeline configuration) → output — no I/O,
no shared mutable state — which makes it straightforward to test with plain input/output
assertions and no mocking.

## Testing HTML output for a given pipeline configuration

The most common test: given specific extensions enabled, does specific Markdown produce the
expected HTML fragment.

```csharp
[Theory]
[InlineData("**bold**", "<p><strong>bold</strong></p>\n")]
[InlineData("- [x] done\n- [ ] pending",
    "<ul>\n<li class=\"task-list-item\"><input disabled=\"disabled\" type=\"checkbox\" checked=\"checked\" /> done</li>\n" +
    "<li class=\"task-list-item\"><input disabled=\"disabled\" type=\"checkbox\" /> pending</li>\n</ul>\n")]
public void ToHtml_WithAdvancedExtensions_RendersExpectedMarkup(string markdown, string expectedHtml)
{
    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    string html = Markdown.ToHtml(markdown, pipeline);

    html.Should().Be(expectedHtml);
}
```

Pin the pipeline configuration inside the test (or a shared test fixture building the same
pipeline the production code uses) rather than depending on Markdig's global defaults — a test
asserting on `UseAdvancedExtensions()` output will fail confusingly if the production pipeline
actually only calls `UsePipeTables()`.

## Testing frontmatter extraction

```csharp
[Fact]
public void ExtractFrontMatter_WithYamlBlock_ReturnsRawYamlText()
{
    const string markdown = "---\ntitle: Sample\n---\n\n# Body";
    var pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().Build();

    var document = Markdown.Parse(markdown, pipeline);
    var frontMatter = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

    frontMatter.Should().NotBeNull();
    frontMatter!.Lines.ToString().Should().Contain("title: Sample");
}
```

Assert against the raw YAML text captured by Markdig, not against a deserialized object, if the
system under test is only responsible for extraction — deserialization is a separate concern
(and a separate library's responsibility) worth testing independently of Markdig's parsing.

## Testing a custom extension

Test a custom `IMarkdownExtension` the same way as a built-in one: build a pipeline with it
registered, and assert on the rendered output rather than on internal parser state.

```csharp
[Fact]
public void ToHtml_WithHighlightExtension_WrapsTextInMarkTag()
{
    var pipeline = new MarkdownPipelineBuilder().UseHighlight().Build();

    string html = Markdown.ToHtml("==important==", pipeline);

    html.Should().Be("<p><mark>important</mark></p>\n");
}
```

Also cover the negative case — the same syntax rendered through a pipeline that does *not*
register the extension should render as literal text (or fall through to whatever built-in
behavior applies to those characters), confirming the extension's parser doesn't accidentally
activate itself unconditionally.

## Most likely scenarios

| Scenario | What to assert |
| --- | --- |
| Converting Markdown to HTML for display | Exact HTML string, or a normalized/parsed-DOM comparison if whitespace differences are not significant to the caller |
| Extracting and using YAML frontmatter | The raw block is found and its text contains the expected keys; deserialization tested separately |
| A custom extension's syntax | Both the positive case (extension enabled, syntax recognized) and the negative case (extension disabled, syntax passes through as plain text) |
