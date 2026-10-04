---
name: dotnet-yamldotnet
description: Guidance on the YamlDotNet NuGet package (verified current release 18.1.0) — a YAML parser and emitter for .NET. Covers Serializer/Deserializer basics via SerializerBuilder/DeserializerBuilder, naming convention configuration (camelCase, PascalCase, kebab-case, underscored) with WithNamingConvention, writing a custom IYamlTypeConverter, handling YAML anchors/aliases and multi-document streams, and choosing between deserializing into strongly-typed C# objects versus dynamic/YamlNode trees. Use when reading or writing YAML configuration files, converting between YAML and C# objects, handling YAML documents with anchors/merge keys, or deciding between a typed POCO model and a loosely-typed YamlNode tree for YAML you don't control the shape of.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# YamlDotNet

Guidance on YamlDotNet, the YAML parsing/emitting library for .NET. Organized by task, not by
YamlDotNet version — the builder-based Serializer/Deserializer API has been stable across major
releases; each reference file notes a version-sensitive fact inline where one exists.

## Read this first: package facts

- **Current stable release: 18.1.0** (verified via NuGet Gallery, `nuget.org/packages/yamldotnet`).
- Provides both a low-level event-based parser/emitter and a high-level object-model
  (`Serializer`/`Deserializer`) layer; this skill covers the high-level layer, which is what most
  consumers reach for.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Serializing/deserializing a C# object to/from YAML with `SerializerBuilder`/`DeserializerBuilder` | [references/core-concepts.md](references/core-concepts.md) |
| Matching YAML property casing (camelCase, kebab-case, snake_case) to C# PascalCase properties | [references/naming-conventions.md](references/naming-conventions.md) |
| Customizing how a specific type serializes/deserializes with `IYamlTypeConverter` | [references/custom-type-converters.md](references/custom-type-converters.md) |
| Handling `&anchor`/`*alias` reuse, merge keys, or a stream containing multiple `---`-separated YAML documents | [references/anchors-aliases-and-multi-document.md](references/anchors-aliases-and-multi-document.md) |
| Choosing between a strongly-typed POCO model and a dynamic `YamlNode`/`YamlStream` tree | [references/typed-vs-dynamic-deserialization.md](references/typed-vs-dynamic-deserialization.md) |
| Testing code that serializes or deserializes YAML | [references/testing.md](references/testing.md) |

## Quick start

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

public sealed class AppConfig
{
    public string ConnectionString { get; set; } = "";
    public int RetryCount { get; set; }
}

string yaml = serializer.Serialize(new AppConfig { ConnectionString = "...", RetryCount = 3 });
AppConfig config = deserializer.Deserialize<AppConfig>(yaml);
```

Build `Serializer`/`Deserializer` instances once and reuse them — both are immutable and
thread-safe after `Build()`, the same pattern as most .NET serialization libraries.

## Out of scope

- YAML 1.1 vs. 1.2 spec edge cases beyond what the default parser settings handle — YamlDotNet
  targets the YAML 1.1 spec by default with 1.2-compatible scalar resolution in recent versions;
  a project needing strict spec-version enforcement checks `YamlDotNet.Core.Schemas` directly
  rather than relying on this skill's default examples.
- JSON interop (YamlDotNet can read a JSON document as a strict subset of YAML, but converting
  between the two object models programmatically is a separate, narrower topic not covered here).
