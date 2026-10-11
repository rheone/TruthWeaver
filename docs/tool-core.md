# Tool core

`TruthWeaver.Tool.Core` is the UI-free library that the `truthweaver` command line and the terminal UI share: it loads rules, finds their format, converts them, reads the configuration file, loads stub values and data files, and writes deterministic output.

The front ends parse options and draw output. The behavior is in this library, so the command line and the terminal UI behave the same way. The library is not a NuGet package (see [Packaging](#packaging)).

## Operations

| Type | Operation |
| --- | --- |
| `SourceReader` | Reads a file, a stream (such as standard input) or a string, and decodes it. |
| `FormatDetector` | Finds the format of an input: rule text, JSON or YAML. |
| `RuleLoader` | Loads a rule from a file, a stream or a string, and selects a rule inside a larger document by a path. |
| `RuleConverter` | Compiles a loaded rule and converts it to another format. |
| `ToolConfigurationFile`, `ToolConfiguration`, `ToolSettings` | Find and read `truthweaver.json`, and resolve the settings. |
| `StubAssignment` | Loads stub values: one truth value for each term. |
| `DataSourceFiles` | Loads JSON and YAML data files for `from("source", "query")` references. |
| `OutputWriter` | Writes output with LF line endings and a final newline, and compares a file with the output. |

Each operation returns a `ToolResult<T>`: the value, or no value, and every diagnostic. An operation does not throw for an expected failure, such as a missing file, a malformed document or a rule that does not compile. It throws `ArgumentNullException` for a `null` argument and `ArgumentOutOfRangeException` for an undefined `RuleFormat`, because those are programming errors. `ToolResult<T>.Succeeded` is `true` when the value is present and no diagnostic is an error. `TryGetValue` gives the value and the diagnostics without the result object.

## Loading a rule

`RuleLoader.LoadFile`, `LoadStream` and `LoadString` read the input, find its format, and return a `LoadedRule`. Loading does not compile the rule.

| Member of `LoadedRule` | Meaning |
| --- | --- |
| `Source` | The whole input: the decoded text, the origin (the file path, `-` for standard input, or a name) and the encoding. |
| `Detection` | The format of the whole input, and what decided it: `Override`, `Extension` or `Content`. |
| `Path` | The normalized path of an embedded rule, such as `$.rules.publish`, or `null` for the whole input. |
| `Format`, `Text` | The format and text of the rule itself. Diagnostic spans point into `Text`. |

### Encoding

The reader finds the encoding from the byte order mark: UTF-8, UTF-16 or UTF-32, little-endian or big-endian. It removes the mark. Input with no mark is UTF-8. A byte that is not valid in the encoding is a `TWT0002` error with the byte offset. The reader does not replace an invalid byte.

### Format detection

The format comes from the first rule in this list that applies:

1. The override, `RuleLoadOptions.Format`.
2. The file extension, with any case: `.json` is JSON, `.yaml` and `.yml` are YAML, and `.rule`, `.tw` and `.txt` are rule text. Standard input has no extension.
3. The content:
   - Text that parses as a JSON object or array is JSON. Text that starts with `{` and then `"` is JSON too, so that the JSON reader reports a malformed document.
   - Text whose first line that is not blank and not a `#` comment starts with `---`, `%YAML`, a `- ` sequence item or a `key:` mapping entry is YAML.
   - Anything else is rule text.

Rule text has no `#` comment, no leading `-` and no `name:` at the start of a line, so the content rules do not take a rule for a document.

### Embedded rules

`RuleLoadOptions.Path` selects one rule inside a JSON or YAML document. The path is a singular JSONPath-style query:

| Segment | Selects |
| --- | --- |
| `$` | The document root. It is optional. |
| `.name` | The member `name`. A name starts with a letter or `_`, and has letters, digits, `_` and `-`. Without `$`, the first name needs no dot. |
| `['key']` or `["key"]` | The member `key`, for any key. |
| `[n]` | Item `n` of an array, from 0. |

`rules.publish`, `$.rules.publish` and `$['rules'].publish` are the same path. The selected value is the rule:

- A JSON object or a YAML mapping is a rule in the format of the document. The rule text is the source text of the object or mapping, so YAML comments inside it stay. The loader indents the first line of a YAML block mapping to its column, so that the text is a YAML document by itself.
- A string is rule text.

<!-- doctest:skip a document that holds rules, not one rule -->
```yaml
service: editor
rules:
  publish:
    op: and
    note: Editors who are active.
    operands:
      - predicate: isActive
      - predicate: hasRole
        args:
          role: editor
  review: 'isActive AND hasRole(role: "reviewer")'
```

The path `rules.publish` selects a YAML rule. The path `rules.review` selects rule text.

A path that selects nothing is a `TWT0005` error that names the first segment that is missing. A path that selects a number, a Boolean, `null` or a sequence is a `TWT0006` error. A path on rule text is a `TWT0006` error, because rule text has no paths.

## Compiling and converting

The tool has no host registry. `RuleConverter.CreateCompiler` makes a permissive compiler: an empty registry in `CompilationMode.Lenient`. The compiler accepts every predicate name and every argument as written. A `from("source", "query")` reference needs no declared data source. Syntax errors, operator errors and limits are still diagnostics. The limits come from the [settings](#configuration-file).

`RuleConverter.Compile` compiles a loaded rule in its own format. `RuleConverter.Convert` converts it to rule text, JSON or YAML, and keeps the notes unless `includeNotes` is `false`. The converted text compiles to a tree equal to the source tree, with the same notes. A conversion does not keep the layout of the source, its spelling (operator case and aliases) or YAML `#` comments. [Rule formats](rule-formats.md) describes the conversion guarantee.

## Configuration file

`truthweaver.json` holds defaults for the tools. `ToolConfigurationFile.Find` looks for the file in a start directory and then in each parent directory, and returns the nearest one. `ToolConfigurationFile.Load` reads it.

<!-- doctest:skip a configuration file, not a rule -->
```json
{
  "$schema": "./truthweaver.schema.json",
  "operatorStyle": "symbolic",
  "constantStyle": "letters",
  "dialect": "latex",
  "limits": {
    "maxDepth": 16,
    "maxNodeCount": 256,
    "maxAnalysisTerms": 12,
    "maxRewriteNodeCount": 5000
  },
  "schemaFile": "predicates.yaml"
}
```

| Member | Values | Default |
| --- | --- | --- |
| `operatorStyle` | `word`, `symbolic`, `cstyle` | `word` |
| `constantStyle` | `word`, `letters`, `logicsymbols` | The default of each printer: `letters` for equations and truth tables, `word` for rule text and trees. |
| `dialect` | `unicode`, `latex`, `asciimath`, `cstyle`, `mathml`, `typst`, `ascii` | `unicode` |
| `limits.maxDepth` | A whole number from 1 | 32 |
| `limits.maxNodeCount` | A whole number from 1 | 512 |
| `limits.maxAnalysisTerms` | A whole number from 1 | 20 |
| `limits.maxRewriteNodeCount` | A whole number from 1 | 100000 |
| `schemaFile` | A file path, relative to the directory of `truthweaver.json` | None |

The reader ignores case in a value. Property names match exactly. A value of the wrong type, an unknown value or a limit below 1 is a `TWT0007` error. An unknown property is a `TWT0008` warning, and the reader ignores the property.

The JSON schema of the file is `truthweaver.schema.json`. The build copies it next to the `TruthWeaver.Tool.Core` assembly. The schema lists the values in lower case and rejects an unknown property.

### Precedence

`ToolSettings.Resolve(configurationFile, commandLine)` resolves each value separately:

1. The command line.
2. The configuration file.
3. The default.

A `ToolConfiguration` is one layer: a `null` member means that the layer does not set the value. `ToolSettings.ToCompilerOptions` gives the compiler options: the limits and `CompilationMode.Lenient`.

## Stub values

A stub value is the truth value that the tool uses for a term in place of a host predicate. A stub-value file is a JSON object or a YAML mapping from a term to a value.

<!-- doctest:skip stub values, not a rule -->
```yaml
isActive: True
'hasRole(role: "editor")': Unknown
```

- A key is one term in rule text. `StubAssignment` compiles each key and keys the value by the canonical text of the term, so a key with other spacing names the same term. Predicate and argument names keep their case.
- A value is `True`, `False` or `Unknown`, or `T`, `F` or `U`, with any case. A JSON `true` or `false` is a value too.
- `ValueOf` gives `Unknown` for a term with no entry.
- A key that is not exactly one term, a value that is not a truth value, and two keys for one term are `TWT0009` errors.

## Data files

`DataSourceFiles.Load` reads a JSON or YAML data file as a `JsonDataSource` or a `YamlDataSource`, which answer JSONPath queries. `DataSourceFiles.LoadAll` loads several files, each under a source name, into a `DataSources` collection. A name used twice is a `TWT0011` error. [Data sources](data-sources.md) describes the queries.

For stub-value files and data files, the format comes from the extension (`.json`, `.yaml` or `.yml`). With no such extension, text that parses as JSON is JSON, and other text is YAML.

## Deterministic output

`OutputWriter` makes the same bytes from the same text on every platform:

- Every line ending is LF.
- Text with content ends with exactly one LF. Text with no content is empty.
- The encoding is UTF-8 with no byte order mark.

`OutputWriter.WriteFile` writes a file and creates its directory. `OutputWriter.Write` writes to a `TextWriter`, such as standard output. `OutputWriter.Check` compares a file with the output byte for byte and does not change the file. The result is `Identical`, `Differs` (with the first line that differs) or `Missing`. A file with CRLF line endings, a byte order mark or no final newline differs. The front ends use `Check` for a `--check` option.

## Diagnostic codes

The tool core reports a problem outside the rule with a `TWT` code. Compiler diagnostics keep their `TRE` codes. [Reading diagnostics](diagnostics.md) describes the members of a diagnostic.

| Code | Name | Severity | Meaning |
| --- | --- | --- | --- |
| `TWT0001` | `InputUnreadable` | Error | The input cannot be read. |
| `TWT0002` | `InvalidEncoding` | Error | A byte is not valid in the encoding of the input. |
| `TWT0003` | `MalformedDocument` | Error | A JSON or YAML document is not well formed. |
| `TWT0004` | `InvalidPath` | Error | An embedded-rule path has a syntax error. |
| `TWT0005` | `PathNotFound` | Error | An embedded-rule path selects nothing. |
| `TWT0006` | `NotARule` | Error | The selected value is not a rule, or the input is rule text and has no paths. |
| `TWT0007` | `InvalidConfiguration` | Error | A configuration value has the wrong type or is out of range. |
| `TWT0008` | `UnknownConfigurationProperty` | Warning | A configuration property is not known. |
| `TWT0009` | `InvalidStubValue` | Error | A stub-value entry is not usable. |
| `TWT0010` | `OutputFailed` | Error | An output file cannot be written or read. |
| `TWT0011` | `DuplicateDataSource` | Error | Two data files have the same source name. |

## Packaging

`TruthWeaver.Tool.Core` has `IsPackable` set to `false`, so `dotnet pack` makes no package for it. The command line and terminal UI tools carry the assembly in their own tool packages. The project is in `TruthWeaver.slnx`, so the CI build, test and analysis steps include it and its test project, `TruthWeaver.Tool.Core.Tests`.

The library references `TruthWeaver`, `TruthWeaver.Yaml` and `TruthWeaver.DataSources.Json`, and no other package. It takes no command-line or terminal UI library. The architecture tests check both rules.
