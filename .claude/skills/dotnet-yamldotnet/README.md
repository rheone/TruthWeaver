# YamlDotNet

Guidance on YamlDotNet, the YAML parsing and emitting library for .NET, covering the high-level
`Serializer`/`Deserializer` object model, naming conventions, custom type converters, anchors and
multi-document streams, and choosing typed versus dynamic deserialization.

## When to reach for it

- Reading or writing a YAML configuration file to/from a strongly-typed C# object.
- Matching YAML property casing (camelCase, kebab-case, snake_case) to C# PascalCase properties.
- Customizing how a specific type serializes or deserializes with `IYamlTypeConverter`.
- Handling YAML anchors, aliases, merge keys, or a stream with multiple `---`-separated documents.
- Deciding between a typed POCO model and a loosely-typed `YamlNode` tree for YAML you don't
  control the shape of.

## Using it

This skill is model-invoked: it fires automatically when you're reading or writing YAML,
converting between YAML and C# objects, or handling anchors/merge keys. You can also invoke it
directly by name.

## What it covers

| Topic | Reference |
| --- | --- |
| SerializerBuilder/DeserializerBuilder, Serialize/Deserialize\<T>() | [references/core-concepts.md](references/core-concepts.md) |
| WithNamingConvention: camelCase, PascalCase, hyphenated, underscored | [references/naming-conventions.md](references/naming-conventions.md) |
| IYamlTypeConverter, registering with WithTypeConverter | [references/custom-type-converters.md](references/custom-type-converters.md) |
| &anchor/*alias reuse, merge keys, multi-document streams | [references/anchors-aliases-and-multi-document.md](references/anchors-aliases-and-multi-document.md) |
| Strongly-typed POCOs vs. YamlNode/YamlMappingNode/YamlStream, dynamic deserialization | [references/typed-vs-dynamic-deserialization.md](references/typed-vs-dynamic-deserialization.md) |
| Testing round-trip serialization and custom converters | [references/testing.md](references/testing.md) |

## Example prompts

- "Deserialize this YAML config file into a strongly-typed AppConfig object."
- "Make this serializer emit snake_case property names instead of PascalCase."
- "Write an IYamlTypeConverter for a custom Money type."
