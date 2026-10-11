# Tool core

`TruthWeaver.Tool.Core` is the UI-free library that the `truthweaver` command line and the terminal UI share: it loads rules, finds their format, converts them, validates them, measures them, reads the configuration file, loads stub values and data files, and writes diagnostics and other output deterministically.

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
| `RuleValidator` | Validates a loaded rule in levels, and gives a `ValidationReport` with the statistics. |
| `PredicateSchemaFile` | Reads the predicate schema file for the strict schema level. |
| `RuleStatistics` | Measures a compiled rule. |
| `DiagnosticWriter`, `ValidationReportWriter` | Write diagnostics, or a whole validation report, as text, JSON or SARIF 2.1.0. |
| `SeverityPolicy` | Finds the exit code from the diagnostics, with or without strict mode. |

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
| `schemaFile` | The path of the [predicate schema file](#predicate-schema-file), relative to the directory of `truthweaver.json` | None |

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

## Validation

`RuleValidator.Validate(rule, options)` validates a loaded rule in levels. Every level is on by default. `ValidationOptions.Levels` switches a level off. The result is a `ToolResult<ValidationReport>` that always has the report. `Succeeded` is `false` when a level found an error.

### Levels

| Level (`ValidationLevels`) | What it checks | Codes |
| --- | --- | --- |
| `Compile` | The syntax and the compile diagnostics of the [permissive compile](#compiling-and-converting). | The `TRE` codes that no other level takes |
| `TreeSchema` | A JSON or YAML rule against the JSON schema of the rule tree, `rule-tree.schema.json`. | `TWT0013`, and `TWT0003` for a document that is not well formed |
| `Limits` | The depth limit and the node limit of the settings. | `TRE0009`, `TRE0010` |
| `PredicateSchema` | The predicate names, the argument names and the argument kinds against the [predicate schema file](#predicate-schema-file). | `TRE0002` to `TRE0005`, and `TWT0012` for a schema file that is not usable |
| `Analysis` | The Strong K3 analysis (tautologies, contradictions, a skipped analysis) and the lints of `ValidationOptions.Lints`. The default is `LintRules.All`. Add `LintRules.Style` for the style lint. | `TRE0011` to `TRE0013`, the lint codes |
| `ConversionCheck` | The rule printed as rule text, JSON and YAML compiles back to the same tree with the same notes. | `TWT0014` |

The report has one `ValidationLevelReport` for each level, in the order of the table. Each one has a status and the diagnostics of that level only:

| Status (`ValidationStatus`) | Meaning |
| --- | --- |
| `Passed` | The level ran. It found no error and no warning. It can have information diagnostics, such as a lint finding. |
| `PassedWithWarnings` | The level ran and found warnings, but no error. |
| `Failed` | The level ran and found at least one error. |
| `Skipped` | The level is on, but it did not run. `Reason` says why: the rule is rule text (tree schema), no schema file is set (predicate schema), or the rule does not compile. |
| `Off` | `ValidationOptions.Levels` switched the level off. |

The validator compiles the rule one time and gives each compile diagnostic to its level. When the rule fails only a limit, the validator compiles it again without the depth and node limits, so the other levels and the statistics still run. With the `Limits` level off, the validator does not apply the depth and node limits, and the deep-nesting lint (`TRE0028`) does not report.

The tree schema level uses the `rule-tree.schema.json` file of the `TruthWeaver` package, embedded in the tool core. It reads a YAML scalar as the YAML rule reader does: a plain `1` is a number and a quoted `'1'` is a string. A finding has the tree path and the span of the value. When a node matches no kind of node, the finding is about the kind that it is nearest to.

### Predicate schema file

The tool has no host registry, so the compile level accepts every predicate. The predicate schema file declares the predicates that a rule may use. The `PredicateSchema` level compiles the rule again against it, in strict mode. `ValidationOptions.PredicateSchema` gives a file that is already read. Otherwise the validator reads the file of `ToolSettings.SchemaFile`.

<!-- doctest:skip a predicate schema file, not a rule -->
```yaml
predicates:
  isActive:
    description: Is the user active?
  hasRole:
    arguments:
      role: string
  inGroup:
    arguments:
      group: string
      since: { kind: dateTimeOffset, required: false }
```

- `predicates` maps a predicate name to its declaration. A declaration has the optional members `description` and `arguments`. A predicate with no arguments can have an empty declaration (`{}`).
- `arguments` maps an argument name to a kind, or to a mapping with `kind`, `required` (`true` or `false`, the default is `true`) and `description`.
- A kind is `string`, `int64`, `decimal`, `boolean`, `dateTimeOffset` or `guid`, or one of them with `Array` at the end, such as `stringArray`. The reader ignores case in a kind.

A file that is not well-formed YAML is a `TWT0003` error. An unknown member, an unknown kind, a reserved predicate name (such as `AND` or `T`) and two predicate names that differ only in case are `TWT0012` errors.

### Statistics

`ValidationReport.Statistics` has the statistics of the rule, or `null` when the rule does not compile. `RuleStatistics.Compute` measures any compiled rule. Each list is in ordinal order.

| Member of `RuleStatistics` | Meaning |
| --- | --- |
| `NodeCount`, `Depth` | The nodes of the tree, and the nodes on the longest path from the root to a leaf. |
| `TermCount`, `DistinctTermCount` | The term nodes (a term used two times counts two times), and the distinct terms. |
| `Predicates` | The distinct predicate names. |
| `OperatorHistogram` | The number of nodes of each operator, by its name in the JSON tree format, such as `and` or `atLeast`. |
| `Variables`, `DataSources` | The distinct `from("source", "query")` references, and the distinct source names. |
| `BddNodeCount` | The size of the Strong K3 analysis, or `null` over the analysis limit. |
| `Constancy` | `Tautology` (always `True`), `Contradiction` (always `False`), `Contingent`, or `Undecided` over the analysis limit. |
| `TruthTableRows`, `ClassicalTruthTableRows`, `TruthTableWithinCap` | 3 and 2 to the power of the distinct terms, and whether the terms are within the default truth-table cap of 8. |

For `AtLeast(2, isActive, hasRole(role: "editor"), NOT isActive) OR (isLocked AND inRegion(region: from("user", "$.region")))`, the node count is 9, the depth is 4, the term count is 5 and the distinct terms are 4. The histogram is `and` 1, `atLeast` 1, `not` 1 and `or` 1. The truth table has 81 rows, or 16 classical rows.

### Exit codes

`SeverityPolicy.ExitCodeFor(diagnostics, strict)` and `ValidationReport.ExitCode(strict)` give the exit code. An error fails a command. A warning fails a command only in strict mode. An information diagnostic never fails a command.

| `ToolExitCode` | Value | Meaning |
| --- | --- | --- |
| `Success` | 0 | No error, and no warning in strict mode. |
| `Failed` | 1 | An error, or a warning in strict mode. |
| `Unusable` | 2 | The tool could not do its work: a `TWT` error about the input, the configuration, the stub values, the data files, the schema file or an output file. This code wins over `Failed`. |

## Diagnostics output

`DiagnosticWriter.Format` and `DiagnosticWriter.Write` write the diagnostics of one or more inputs. A `DiagnosticDocument` holds the diagnostics of one input, with its origin, the text that the spans point into and the path of an embedded rule. `DiagnosticDocument.For(rule, diagnostics)` makes one from a loaded rule. The output is deterministic: the order is the order given, and every line ends with LF.

| `DiagnosticOutputFormat` | Output |
| --- | --- |
| `Text` | For each diagnostic, the input and then the text that [Reading diagnostics](diagnostics.md) shows: the code, the severity, the line and column, the message, the source line with a caret under the span, and the expected, found and suggestion lines. |
| `Json` | An object with a `diagnostics` array. Each item has `origin`, `code`, `name`, `severity`, `message`, and, when they apply, `rulePath`, `path`, `span` (`start`, `length`), `line`, `column`, `endLine`, `endColumn`, `expected`, `found` and `suggestion` (`kind`, `text`). |
| `Sarif` | A SARIF 2.1.0 log with one run. |

In the SARIF log, the rules of the tool are the rows of `DiagnosticCodeTable`: every `TRE` code and every `TWT` code, with its name, a short description and its usual level. Each result names its rule by `ruleId` and `ruleIndex`. The level is `error`, `warning` or `note`. A location comes from the span:

- When the whole input is the rule, the result has the file (`artifactLocation`) and a `region` with the start and end line and column and the character offset and length. Columns count UTF-16 code units.
- When the rule is embedded in a larger document, the spans point into the rule, not into the file. The result has the file without a region.
- When the diagnostic has a tree path, or the rule is embedded, the result has a logical location with the full path, such as `$.rules.publish.operands[1]`.
- Standard input has no file, so its results have no physical location.

The expected text, the found text and the suggestion are in the `properties` of the result.

`ValidationReportWriter.Format(report, format)` writes a whole validation report. Text and JSON have the status of each level, the statistics and the diagnostics. SARIF has the diagnostics only, because SARIF has no place for statistics.

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
| `TWT0012` | `InvalidSchemaFile` | Error | The predicate schema file is not usable. |
| `TWT0013` | `TreeSchemaViolation` | Error | A JSON or YAML rule does not match the JSON schema of the rule tree. |
| `TWT0014` | `ConversionMismatch` | Error | A conversion of the rule to another format does not compile to the same tree. |

## Packaging

`TruthWeaver.Tool.Core` has `IsPackable` set to `false`, so `dotnet pack` makes no package for it. The command line and terminal UI tools carry the assembly in their own tool packages. The project is in `TruthWeaver.slnx`, so the CI build, test and analysis steps include it and its test project, `TruthWeaver.Tool.Core.Tests`.

The library references `TruthWeaver`, `TruthWeaver.Yaml` and `TruthWeaver.DataSources.Json`, and no other package. It takes no command-line or terminal UI library. The architecture tests check both rules.
