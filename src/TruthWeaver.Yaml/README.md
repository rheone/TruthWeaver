# TruthWeaver.Yaml

YAML support for TruthWeaver: read and write rule trees, and query YAML documents as a data source.

## Purpose

This package isolates the YamlDotNet dependency. A host that only uses rule text and JSON does not reference it, and so it
does not take YamlDotNet. The package adds extension methods to compile a rule from YAML and to print a compiled rule as
YAML. It also converts a rule between rule text, JSON and YAML, and it answers JSONPath queries against a YAML document.

## Depends on

- [TruthWeaver](../TruthWeaver/README.md) for the compiler, the rule tree and the printers.
- [TruthWeaver.DataSources.Json](../TruthWeaver.DataSources.Json/README.md) for the JSONPath query engine that `YamlDataSource` shares.

## Key types

| Type | Role |
| --- | --- |
| `YamlRuleExtensions` | `CompileYaml`, `TryCompileYaml` and `PrintYaml` on the compiler and on a compiled rule. |
| `RuleFormatExtensions` | `CompileAs`, `PrintAs` and `Convert` for any of the three formats. |
| `RuleConversionResult` | The converted text and the diagnostics of a conversion. |
| `YamlDataSource` | An `IDataSource` that reads a YAML document with the scalar rules of JSON. |
