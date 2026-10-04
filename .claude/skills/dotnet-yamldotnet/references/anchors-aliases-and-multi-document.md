# Anchors, Aliases, and Multi-Document Streams

YAML supports reusing a node elsewhere in the same document (anchors/aliases) and packing multiple
independent documents into one stream (`---`-separated) — both are handled by YamlDotNet's default
deserializer without extra configuration, but each has a behavior worth knowing before relying on
it.

## Anchors and aliases

```yaml
defaults: &defaults
  timeout: 30
  retries: 3

service_a:
  <<: *defaults
  timeout: 60

service_b:
  <<: *defaults
```

- `&defaults` marks a node as reusable; `*defaults` references it elsewhere in the same document.
- `<<:` is the YAML **merge key** convention — it merges the referenced mapping's keys into the
  current mapping, with locally specified keys (`timeout: 60` in `service_a`) taking precedence
  over the merged-in value.
- YamlDotNet resolves anchors/aliases and merge keys automatically during deserialization; the
  deserialized object graph for `service_a` ends up with `Timeout = 60, Retries = 3` with no extra
  configuration needed.

## Reference equality after deserialization

When two mapping keys alias the *same* anchor and deserialize into a reference type, YamlDotNet
preserves reference identity — both properties point to the same object instance, not two separate
copies with equal values:

```yaml
primary: &conn
  host: db.internal
replica: *conn
```

```csharp
config.Primary.Should().BeSameAs(config.Replica); // same object reference, not just equal values
```

This matters if downstream code mutates a deserialized object expecting it to be independent —
mutating `config.Primary.Host` also changes what `config.Replica.Host` reports, because they're the
same instance.

## Multi-document streams

A single YAML file/stream can contain multiple `---`-separated documents. `Deserializer` reads only
the first document by default when you call `Deserialize<T>()` against a `TextReader`/string
containing more than one. To read all documents in a stream, drive the lower-level `Parser`
directly:

```csharp
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

using var reader = new StringReader(multiDocumentYaml);
var parser = new Parser(reader);
parser.Consume<StreamStart>();

var deserializer = new DeserializerBuilder().Build();
var documents = new List<ServiceDefinition>();

while (parser.Accept<DocumentStart>(out _))
{
    documents.Add(deserializer.Deserialize<ServiceDefinition>(parser));
}
```

Each call to `deserializer.Deserialize<T>(parser)` (the overload accepting an `IParser` rather than
a `TextReader`/string) consumes exactly one document from the stream and leaves the parser
positioned to read the next `DocumentStart`, which is what makes looping over `parser.Accept<DocumentStart>()`
work.

## Common pitfall

Calling `Deserialize<T>()` with the `TextReader`/string overload against a multi-document stream
silently returns only the first document — it does not throw or warn that additional documents were
ignored. If a YAML file might legitimately contain more than one document (common in Kubernetes
manifests bundling multiple resources in one file), always use the `Parser`-driven loop above rather
than assuming a single call captures everything.
