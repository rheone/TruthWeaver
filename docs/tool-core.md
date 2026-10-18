# Tool core

`TruthWeaver.Tool.Core` is the UI-free library that the `truthweaver` command line and the terminal UI share: it loads rules, finds their format, converts them, writes edited rules back into their documents, validates them, measures them, exports them as artifacts and reports, reads the configuration file, loads stub values and data files, and writes diagnostics and other output deterministically.

The types are in one namespace for each folder of the project, `TruthWeaver.Tool.Core.<Folder>`. The [project README](../src/TruthWeaver.Tool.Core/README.md) lists the folders.

The front ends parse options and draw output. The behavior is in this library, so the command line and the terminal UI behave the same way. The library is not a NuGet package (see [Packaging](#packaging)).

## Operations

| Type | Operation |
| --- | --- |
| `SourceReader` | Reads a file, a stream (such as standard input) or a string, and decodes it. |
| `FormatDetector` | Finds the format of an input: rule text, JSON or YAML. |
| `RuleLoader` | Loads a rule from a file, a stream or a string, and selects a rule inside a larger document by a path. |
| `RuleConverter` | Compiles a loaded rule and converts it to another format. |
| `JsonLogicImporter` | Imports a JsonLogic rule as a rule tree (JSON). See [JsonLogic import](#jsonlogic-import). |
| `ToolConfigurationFile`, `ToolConfiguration`, `ToolSettings` | Find and read `truthweaver.json`, and resolve the settings. |
| `StubAssignment` | Loads stub values: one truth value for each term. |
| `DataSourceFiles` | Loads JSON and YAML data files for `from("source", "query")` references. |
| `OutputWriter` | Writes output with LF line endings and a final newline, and compares a file with the output. |
| `RuleValidator` | Validates a loaded rule in levels, and gives a `ValidationReport` with the statistics. |
| `PredicateSchemaFile` | Reads the predicate schema file for the strict schema level. |
| `RuleStatistics` | Measures a compiled rule. |
| `DiagnosticWriter`, `ValidationReportWriter` | Write diagnostics, or a whole validation report, as text, JSON or SARIF 2.1.0. |
| `SeverityPolicy` | Finds the exit code from the diagnostics, with or without strict mode. |
| `ArtifactCatalog`, `ArtifactExporter` | List the artifacts that the tools make from a rule, and export one artifact or all of them with a manifest. |
| `RuleReport` | Writes the Markdown report of a rule. |
| `ImageRenderer` | Renders a Mermaid or DOT diagram as an SVG or PNG image with an external program. |
| `RulePatcher` | Writes an edited rule back into its document, and keeps the comments and layout of the document. |
| `StubEvaluator` | Evaluates a rule with stub values in place of the predicates, and with data files. |
| `RuleDocument` | Holds one rule open for editing: compiles each change, keeps the last valid tree, undo and redo, format, open and save. See [Editing a rule](#editing-a-rule). |
| `AssignmentSession`, `AssignmentVariables` | Try a rule with values: a value for each term, data for `from(...)` references, and a policy. They give the decision, the trace with a reason for each node, and the faults. See [Trying a rule with values](#trying-a-rule-with-values). |
| `RuleTestCaseText` | Adds, replaces, removes and reads the cases of a rule test file as text, and swaps its rule. See [Editing a rule test file](#editing-a-rule-test-file). |

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

`DocumentOutline` helps a front end to find the places of a document where a rule can be. `DocumentOutline.Read(path)` and `DocumentOutline.Browse(text, format)` list the objects, arrays and strings of a JSON or YAML document in document order, as `DocumentNode` values with the normalized path, the depth, a kind and a short label. An object with a `const`, `predicate` or `op` key is a rule (`DocumentNodeKind.Rule`) and is one entry: the nodes inside it are not listed. A string is `DocumentNodeKind.Text`. Rule text has no paths, so `Read` reports a `TWT0006` error for it. `DocumentOutline.Match(nodes, pattern)` finds the entries that a path names. In a pattern, `.*` or `[*]` matches every key or index at that place, so a pattern can name none, one or many entries.

## Compiling and converting

The tool has no host registry. `RuleConverter.CreateCompiler` makes a permissive compiler: an empty registry in `CompilationMode.Lenient`. The compiler accepts every predicate name and every argument as written. A `from("source", "query")` reference needs no declared data source. Syntax errors, operator errors and limits are still diagnostics. The limits come from the [settings](#configuration-file).

`RuleConverter.Compile` compiles a loaded rule in its own format. `RuleConverter.Convert` converts it to rule text, JSON or YAML, and keeps the notes unless `includeNotes` is `false`. The converted text compiles to a tree equal to the source tree, with the same notes. A conversion does not keep the layout of the source, its spelling (operator case and aliases) or YAML `#` comments. [Rule formats](rule-formats.md) describes the conversion guarantee.

## Editing a rule

`RuleDocument` is the model of an editor, such as the [terminal UI](tui.md). It holds the text of one rule, its format, its file and the result of compiling the text.

| Member | Behavior |
| --- | --- |
| `Create(text, format, settings)` | Makes a document with no file. The default is an empty rule text. |
| `Open(path, settings)` | Reads a file with `RuleLoader` and finds its format. A file that is read but does not compile still opens, with its diagnostics. A file that cannot be read gives the diagnostics and no document. |
| `SetText(text)` | Replaces the text and compiles it. The change is one undo step and clears the redo steps. The same text changes nothing. |
| `Check(text, format)`, `SetText(checkedText)` | `Check` compiles text without changing the document and returns a `CheckedText`; any thread may call it. `SetText(checkedText)` applies that result as `SetText(text)` does, without a second compile. A front end uses the pair to compile off its UI thread. |
| `Diagnostics` | The diagnostics of the current text. Their spans point into `Text`. |
| `Definitions` | The `let` definitions and uses of the current text, read without expansion ([`RuleDefinitions`](rule-text.md#read-the-definitions-of-a-document)). It is read again after each change. |
| `Rule`, `IsRuleCurrent` | The last rule that compiled. When the current text has an error, `IsRuleCurrent` is `false` and `Rule` stays the rule of an earlier text. `Rule` is `null` until a text compiles. |
| `Undo()`, `Redo()`, `CanUndo`, `CanRedo` | Move through the history of text and format. The document keeps the last 1000 steps (`UndoLimit`). |
| `FormatText(pretty)` | Formats the text in its own format as one undo step: canonical (one line for rule text and JSON) or pretty (rule text at most 80 characters wide, JSON indented by two spaces). YAML is one node on each line in both forms. Rule text uses the operator style of the settings. The `let` definitions of the text stay. Text with an error does not change, and the result holds the diagnostics. |
| `Save(path)` | Writes the file as `OutputWriter` does. The extension selects the format (`.rule`, `.tw`, `.txt`, `.json`, `.yaml`, `.yml`); another extension keeps the format. A save in another format converts the rule first, as one undo step, and needs text that compiles. |
| `ConvertTo(format)` | Converts the text to rule text, JSON or YAML as one undo step. The tree, the notes and the `let` definitions stay (a `definitions` section and `use` nodes in JSON and YAML); the layout, the spelling and YAML comments do not. A save in another format converts the same way. Text with an error does not change. |
| `ReplaceRule(rule)` | Puts a compiled rule in place of the text, as one undo step: a structural edit or an accepted rewrite. The rule is printed in the format of the document with its notes, pretty when the text has more than one line and canonical otherwise. The printed text must compile with the limits of the settings, or the text does not change and the result holds the diagnostics. |
| `ApplyEdit(edit)` | Puts the result of a structural edit in place of the text, as one undo step. An edit with a `Document` (a rule with `let` definitions) is printed with its definitions and uses; any other edit is put in as `ReplaceRule` puts a rule. The layout and the compile check are those of `ReplaceRule`. |
| `MarkSaved()` | Makes the current text the saved text. A front end calls it after it wrote the rule somewhere else, such as into a larger document. |
| `IsDirty`, `Path`, `Changed` | Unsaved changes, the full path of the file, and the event after each change of the text, format, path or saved state. |

The document compiles with the permissive compiler of [Compiling and converting](#compiling-and-converting). It is not thread-safe: call it from one thread.

## Editing the tree

`RuleTree`, `RuleTreeEditor` and `RulePalette` are the model of a structural editor, such as the Tree pane of the [terminal UI](tui.md#structural-editing).

`RuleTree.Of(rule, settings, definitions)` lists the nodes of a compiled rule in tree order. Each `RuleTreeNode` has a `Path`, a `Depth`, a `Kind` (`Constant`, `Term`, `Operator`, `Definition`, `Use` or `Parameter`), a `Label`, an `OperandCount`, its `Literals` (the values that an edit can change, as JSON), its `Note` and its `Uses`. The path is the path of the node in the JSON tree format, the same form as the keys of `CompiledRule.Notes`: `$` is the root and `$.operands[1]` is its second operand. `TextOf(path)` gives the rule text of a node of the rule with its notes, which compiles back to the same subtree.

`definitions` is optional: the `RuleDefinitions` of the text that the rule was compiled from (`RuleDocument.Definitions`). With them, a node of the rule that a use expands to has the use in `Uses` (such as `both(a, b)`) and in front of its label, and each definition follows the rule: a `Definition` node at `$.definitions.name` labeled `let name(parameters)`, then its body, where a parameter is a `Parameter` node and a use is a `Use` node with its arguments as operands. `Definitions` gives them back.

`RuleTreeEditor` takes a compiled rule, the settings and the optional definitions. Each edit names a node by its path and returns a `RuleTreeEdit`: the edited rule, the path of the node to select next and a message, or no rule, the reason and the compiler diagnostics. With definitions, an edit changes the source of the node and keeps the definitions: `ReplaceOperator`, `ChangeLiteral` and `InsertOperand` change the node that holds the operator (the body of the definition for an expanded use), `WrapInNot`, `Negate` and `Replace` change the outermost source (the use itself at the root of an expanded use), and `Delete` and `Swap` change the operands of the parent. A node in the place of a parameter changes the argument of that use. A node with a `$.definitions` path is changed in place; the entry of a definition itself and a literal on a parameter are refused. The edited document must compile, so an edit that would break a definition is refused. `RuleTreeEdit.Document` holds the edited document in the JSON tree format with its definitions, and an expression in rule text may use the definitions.

| Edit | Behavior |
| --- | --- |
| `ReplaceOperator(path, op)` | Puts another operator in place of the operator of the node and keeps the operands. A count or range that the new operator needs starts at 1 (`max` at the number of operands); a member that it does not take is removed. |
| `WrapInNot(path)` | Wraps the node in `NOT`. |
| `Negate(path)` | Removes a `NOT`, swaps `True` and `False`, swaps `AND` and `NAND`, `OR` and `NOR`, `XOR` and `EQUIVALENT`, and wraps any other node in `NOT`. A negation of `Unknown` is refused. |
| `InsertOperand(path, expression, index)` | Inserts an operand into an operator, at the end when the index is `null`. |
| `Delete(path)` | Removes the node from its parent. An operator that needs two operands and keeps one is replaced by that operand. The root cannot be deleted. |
| `Swap(path, direction)` | Swaps the node with the next (1) or the previous (-1) operand. |
| `Replace(path, expression)` | Puts an expression in place of the node. |
| `ChangeLiteral(path, name, value)` | Changes `const` of a constant, an argument of a term (a new name adds it, an empty value removes it), or `k`, `min` or `max` of an operator. The value is JSON; text that is not JSON is a string. |

An expression is rule text, JSON or YAML; its content selects the format. An expression that does not compile is refused with its diagnostics. Every edit compiles the result with the permissive compiler and the limits of the settings, and a result that does not compile is refused: the rule never changes. The notes of the nodes that an edit keeps stay on those nodes. `RuleDocument.ApplyEdit` puts an edit into the document.

`RulePalette.Create(rule, schema, definitions)` lists the operators, the constants, the predicates of a [predicate schema file](#predicate-schema-file), the predicates of the rule (with the values of their first use as defaults) and, when `definitions` is given, each `let` definition (source `Definition`, its parameters as arguments). A `PaletteEntry` has a `Source`, a `Name`, a `Description` and its `Arguments` (`PaletteArgument`: name, `LiteralKind`, required, default, description). `RulePalette.Fill(entry, values, settings)` makes the rule text of a constant or of a call of a predicate with the values of a form, or gives the diagnostics when the call does not compile. For a definition it makes the use, `name(value, ...)`, without a compile: only the document that has the definition can compile it.

## Rewrites

`RewritePlayground` applies the rewrites of the library to a compiled rule and does not change it, so a front end can show the result before the author accepts it.

`RewritePlayground.Preview(rule, kind, settings)` applies one `RuleRewriteKind`: `Simplify` (with `SimplifyWithSteps`), `Canonicalize`, `NegationNormalForm` (`ToNnf`), `ConjunctiveNormalForm` (`ToCnf`), `DisjunctiveNormalForm` (`ToDnf`), `Compress` (`CompressToDerived`), `ExpandToPrimitives` or `OptimizeOrder`. `MaxRewriteNodeCount` of the settings caps the normal forms and the expansion. The `RewritePreview` holds the rule before and after (`After` is `null` when the rewrite gives no rule, such as for `TRE0016`), the steps of `Simplify`, the diagnostics of the rewrite, `Changed` (the tree after differs from the tree before), and the rule text and the plain text tree before and after. `RewritePlayground.NameOf(kind)` gives the name of the library method. A rewrite returns a rule with no notes. `RewritePlayground.CarryNotes(before, after, settings)` puts the notes back: a note stays when the node at its path has the same subtree after the rewrite, and the `NotedRewrite` it returns holds the rule with those notes and the number of notes it dropped. A front end puts an accepted result into the document with `RuleDocument.ReplaceRule`.

`RewritePlayground.Compare(earlier, current, settings)` gives a `RuleComparison`: `Equivalence` from `RuleEquivalence.Compare` and `Relation` (equivalent, tighter, looser, overlapping, disjoint) from `RuleImplication.Compare`. Both stop at `MaxAnalysisTerms` of the settings distinct terms across the two rules, and are then undecided.

## Trying a rule with values

`AssignmentSession` is the model of the assignment pane of the [terminal UI](tui.md#assign-values). It keeps a value for each term, the data for the `from("source", "query")` references and the policy for an `Unknown` result. It evaluates the rule of a `RuleDocument` through `StubEvaluator`, so the decision equals the decision of `truthweaver evaluate` for the same values.

| Member | Behavior |
| --- | --- |
| `Set(key, value)`, `ValueOf(key)`, `Values` | Give a term a value, read it, and list the terms that have a definite value. A key is the canonical text of the term. `Unknown` removes the value. A term with no value is `Unknown`. |
| `Cycle(key)` | Moves a term to the next value: `Unknown`, `True`, `False`, and `Unknown` again. |
| `ClearValues()`, `Load(values, variablesText)` | Remove every value, or replace the values and the variables, for example with a saved case. |
| `SetVariables(text)`, `LoadVariablesFile(path)`, `VariablesText`, `VariableDiagnostics` | Set the data. See [the variables document](#the-variables-document). |
| `Policy`, `CyclePolicy()` | The policy for the answers of `Collapse` and `Project`: `UnknownAsFalse` (the default), `UnknownAsTrue` or `UnknownIsError`. |
| `EvaluateAsync(document)` | Evaluates the last tree of the document that compiled and gives an `AssignmentOutcome`. |

The values stay when the rule changes. A value applies again when its term comes back.

An `AssignmentOutcome` has these members.

| Member | Meaning |
| --- | --- |
| `Terms` | The distinct terms of the rule, in the order of their first use, each with its value. |
| `Decision`, `IsSatisfied` | The decision, and whether it is satisfied. Only `True` is satisfied. |
| `Trace` | A tree of `TraceLine` nodes. Each node has its text, its value (`null` when short-circuiting skipped it) and a plain reason. |
| `Faults` | One line for each fault: the term and the cause. |
| `Collapsed`, `Projected` | The decision under the policy: `Collapse(policy)`, and `Project` with the value that the policy names. `Projected` is `null` for `UnknownIsError`. |
| `VariableDiagnostics` | The problems of the variables document. |
| `Evaluation` | The `StubEvaluation` behind the decision: the rule compiled over the stand-in predicates, the decision and the options. A view that marks the decision on the rule prints it from this rule. It is `null` when no rule was evaluated. |

A term with a `from(...)` argument takes the value of the term with the value that the data gives. Its key is the canonical text of that resolved term, such as `inRegion(region: "EU")`, and it is `null` while the data does not resolve the reference. A reference that does not resolve is a fault, and the term is `Unknown`.

The reason of a node says why it has its value. For `AND`, `OR` and `NOT` it also says how an `Unknown` operand acts: `False` wins in `AND`, `True` wins in `OR`, and `NOT` of `Unknown` stays `Unknown`.

### The variables document

The variables are one JSON or YAML object. Each key is a source name, and its value is the data of that source.

<!-- doctest:skip variables document, not a rule -->
```yaml
user:
  region: EU
  age: 42
```

With this document, `from("user", "$.region")` is `"EU"`. A YAML plain scalar is read as in JSON: `null`, `true` or `false`, a whole number, a decimal number, or a string. A quoted scalar is a string. A document with an error gives a `TWT0003` diagnostic without the content of the document, and the session keeps the data of the last document that was well-formed. `AssignmentVariables.Parse(text)` makes the data sources from the text.

## Editing a rule test file

`RuleTestCaseText` edits the text of a [rule test file](rule-tests.md) one case at a time. It does not depend on `TruthWeaver.Testing`. A case is a `RuleTestCaseSpec`: the name, the value of each term, the variables, and the expected result and number of faults.

| Member | Behavior |
| --- | --- |
| `NewFile(ruleText, firstCase)` | Makes the text of a file with the rule inline and one case. |
| `ReadCases(fileText, baseDirectory)` | Reads the cases. A `variableFiles` path is relative to `baseDirectory`. The `trace` expectation is not read. |
| `AddOrReplaceCase(fileText, spec)` | Adds the case at the end, or replaces the case with the same name. The rest of the file stays as it is, with its comments. |
| `RemoveCase(fileText, name)` | Removes a case. The last case stays. |
| `WithRule(fileText, ruleText)` | Replaces `rule` or `ruleFile` with an inline rule, to run the cases on another rule. Do not write the result back. |

Each operation gives `TWT0022` diagnostics when the file is not usable. The cases must be a block list, and each case must start on the line of its dash. A replaced case loses the keys that the class does not write: `trace`, `variableFiles` and comments.

## JsonLogic import

`JsonLogicImporter.Import(jsonLogic)` reads a [JsonLogic](https://jsonlogic.com) rule and returns the rule tree as compact JSON. Load the result with `RuleLoader.LoadString` in the JSON format to compile or convert it. The importer uses `System.Text.Json` only.

JsonLogic is two-valued, so a mapped rule can answer `Unknown` where JsonLogic answers `false`. The import reports each such place as a `TWT0024` warning. An operator or shape without a mapping fails the import with a `TWT0023` error for each occurrence, and the result has no value.

| JsonLogic | TruthWeaver |
| --- | --- |
| `true`, `false` | The constants `True` and `False`. |
| `null` as a condition | The constant `Unknown`, with a `TWT0024` warning. |
| `{"and": [...]}`, `{"or": [...]}` | `AND`, `OR`. One operand gives that operand. No operand is a `TWT0023` error. |
| `{"!": x}` | `NOT`. |
| `{"!!": x}` | The operand `x`. |
| `{"if": [c, a, b]}` | `If(c, a, b)`. A longer list `[c1, a1, c2, a2, e]` nests in the else position. A list with no else branch uses `Unknown` for it, with a `TWT0024` warning. |
| `{"var": "a.b"}` as a condition | The term `aB`, with a `TWT0024` warning: JsonLogic reads the value by truthiness. |
| `{"==": [{"var": "a"}, v]}`, `===` | The term `aIsEqualTo(value: v)`. With `null` for `v`: `aIsNull`. |
| `{"!=": ...}`, `!==` | `aIsNotEqualTo(value: v)`. With `null`: `aIsNotNull`. |
| `<`, `<=`, `>`, `>=` | `aIsLessThan`, `aIsLessThanOrEqualTo`, `aIsGreaterThan`, `aIsGreaterThanOrEqualTo`, each with `value`. A literal on the left flips the comparison. |
| `{"<=": [lo, {"var": "a"}, hi]}` | `aIsBetween(lower: lo, upper: hi)`. |
| `{"<": [lo, {"var": "a"}, hi]}` | `aIsGreaterThan(value: lo) AND aIsLessThan(value: hi)`. |
| `{"in": [{"var": "a"}, [..]]}` | `aIsIn(values: [..])`. |
| `{"in": [v, {"var": "a"}]}` | `aContains(value: v)`. |

A term name is the variable path in camel case, followed by the catalog member: `user.age` with `>` gives `userAgeIsGreaterThan`. The host registers a predicate with that name that selects the value. A character that a name cannot hold becomes `_`.

The importer warns about these differences:

- A `var` that is read by truthiness, in `and`, `or`, `!`, `if` or at the root.
- A missing variable. JsonLogic reads it as `null`, so the comparison is `false`. The term answers `Unknown`. The warning appears once for each variable.
- `==` and `!=`. They convert types in JsonLogic. The terms compare without conversion, like `===` and `!==`.
- A default value of `var`. The importer ignores it.
- An `if` with no else branch, and a `null` condition.

These have no mapping and fail the import: every other operator (`+`, `cat`, `map`, `reduce`, `missing`, `some` and the rest), a comparison between two variables or two literals, a comparison of a literal with an operation, a literal that is not `true`, `false` or `null` where a condition is expected, and a `var` path that is not a non-empty string.

## Writing a rule back

`RulePatcher.Patch` writes an edited rule back into the document that it came from, in the format of the document. The edit is a `CompiledRule` (from `RuleConverter.Compile`, a rewrite or the `RuleBuilder`). The patcher uses only its tree and its notes.

| Overload | Writes back |
| --- | --- |
| `Patch(LoadedRule, CompiledRule, mode, settings)` | One rule, at the path it was loaded from. |
| `Patch(SourceText, RuleFormat, edits, mode, settings)` | Each `RuleEdit` (a path, or `null` for the whole document, and a rule), in order. Each edit applies to the text that the previous edit gave, so several rules in one document patch independently. |

The result is a `DocumentPatch`: the new text, the encoding of the original, and one `RulePatchOutcome` (the path and a `RulePatchKind`) for each edit. When one edit fails, the result has no text and the document does not change.

### Patch mode

`WriteBackMode.Patch` is the default. The patcher compares the tree form of the rule in the document with the edited rule, and replaces only the values that changed. A value is an operator name, a predicate name, an argument, a constant or a note. Comments, key order, quotes, indentation and every byte outside the changed values stay. A changed string keeps its YAML style (plain, single-quoted or double-quoted) when the new value can have that style with the same meaning. Otherwise it is double-quoted.

<!-- doctest:skip a document that holds rules, not one rule -->
```yaml
rules:
  # Who may publish.
  publish:
    op: and  # all of these
    operands:
      - predicate: isActive  # checked first
      - predicate: hasRole
        args:
          role: editor
```

An edit that changes `editor` to `admin` changes only that word. The comments stay.

The patcher writes the whole rule again, in its span only, when the change does not fit the old values:

- The structure changed: a node, a key (such as a new `note`) or an operand was added or removed.
- A changed value is a YAML block scalar, an alias, an anchored value or a tagged value. A value with an anchor is shared with its aliases, so a change to it would change other values too.
- The rule is rule text: a rule text file, or a string that holds rule text. Rule text has no positions for its parts. The new text keeps the quote style or the block header of the string.

The rewrite fits the place of the rule: a YAML block mapping is indented to its column, a YAML flow mapping stays on one line in flow style, and a JSON object takes the indentation of the document. The text outside the rule stays. Comments and layout inside the rule do not stay. The outcome is `RuleRewritten`, with a `TWT0019` warning that gives the reason.

The patcher reads every result back and compiles it. When the result does not compile to the edited tree with the edited notes, the patch is a `TWT0021` error. A change to a rule that is itself anchored, so that an alias shares it, is a `TWT0021` error too. An edit that compiles to the same tree and notes as the rule in the document is `Unchanged`, and no byte changes.

### Round-trip mode

`WriteBackMode.RoundTrip` reads the whole document into a document model, puts each edited rule at its path, and serializes the whole document again. JSON is indented by two spaces. YAML comments, quotes, key layout and indentation can be lost, so this mode always gives a `TWT0020` warning. The outcome of each edit is `DocumentRewritten`.

### Writing the file

`RulePatcher.WriteFile` writes a `DocumentPatch` in the encoding of the original, with its byte order mark and its line endings. `RulePatcher.Encode` gives the same bytes. Do not write a patch with `OutputWriter`: it changes line endings to LF and removes a byte order mark, so bytes outside the rule change.

## Configuration file

`truthweaver.json` holds defaults for the tools. `ToolConfigurationFile.Find` looks for the file in a start directory and then in each parent directory, and returns the nearest one. `ToolConfigurationFile.Load` reads it.

<!-- doctest:skip a configuration file, not a rule -->
```json
{
  "$schema": "./truthweaver.schema.json",
  "operatorStyle": "symbolic",
  "constantStyle": "letters",
  "dialect": "latex",
  "theme": "dark",
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
| `theme` | `auto`, `light`, `dark`. Only the [terminal workbench](tui.md#themes-and-colors) reads it. | `auto` |
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

## Stub evaluation

`StubEvaluator.EvaluateAsync(rule, options)` compiles a loaded rule over stand-in predicates and evaluates it. The result is a `StubEvaluation`: the compiled rule, the `Decision` and the `EvaluationOptions` that were used. A rule that does not compile gives no evaluation and the compile diagnostics.

| Member of `StubEvaluationOptions` | Meaning | Default |
| --- | --- | --- |
| `Stubs` | The [stub values](#stub-values). | `StubAssignment.Empty`: every term is `Unknown`. |
| `DataSources` | The [data files](#data-files) for the `from("source", "query")` references. | None. |
| `Evaluation` | The mode, the fault budget, the timeout, the flat trace and the resolved values in the trace. | `EvaluationOptions.Default` |
| `Settings` | The compile limits and the schema file. | `ToolSettings.Default` |
| `PredicateSchema` | A [predicate schema file](#predicate-schema-file) that is already read. Otherwise the evaluator reads the file of `Settings.SchemaFile`, when one is set. | None. |

The evaluator registers one stand-in for each predicate of the rule:

- The stand-in declares every argument that the rule gives the predicate, each one optional, so one predicate can have different arguments in different terms.
- The kind of an argument comes from the predicate schema file when the file declares it. Otherwise it comes from the literal in the rule: a string, a whole number, a decimal number, a Boolean, or a list of one of them. A whole number and a decimal number for one argument make it a decimal. An argument that only `from(...)` references give, and that the schema file does not declare, is a string.
- The stand-in gives the stub value of its term, found by the canonical text of the term with the values that the predicate receives. A term with no stub value is `Unknown`, and it is not a fault.

For a term with a `from(...)` argument, the values are the values that the data source gives. With `{"region": "EU"}` as `user`, the term `inRegion(region: from("user", "$.region"))` takes the stub value of `inRegion(region: "EU")`. A reference that does not resolve, or a value that does not fit the kind of the argument, is a fault, and the term is `Unknown`.

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
| `Unusable` | 2 | The tool could not do its work: a `TWT` error about the input, the configuration, the stub values, the data files, the schema file, an output file, an artifact id, an image renderer, a write-back, a rule test file or a JsonLogic document that the importer cannot map. This code wins over `Failed`. |

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

## Artifacts and export

An artifact is one output that the tools make from a rule. `ArtifactCatalog.All` lists every artifact in a fixed order. Each artifact calls one library printer with fixed options, so its text is exactly the text of that printer. From the settings, an artifact uses only the compile limits, the analysis limit and the schema file. `ArtifactCatalog.TryFind(id)` finds an artifact by its id. `ArtifactCatalog.Equation(dialect, constants, latexWrap)` and `ArtifactCatalog.TruthTable(format)` find an artifact from printer options.

| Id | File | Text |
| --- | --- | --- |
| `rule-text` | `rule-text.rule` | `PrintRuleText()`: the word style, parentheses and the notes. |
| `rule-text-symbolic` | `rule-text-symbolic.rule` | `PrintRuleText` with `OperatorStyle.Symbolic`. |
| `rule-text-pretty` | `rule-text-pretty.rule` | `PrintRuleText` with `RuleTextOptions.Width` 80. |
| `json`, `yaml` | `json.json`, `yaml.yaml` | `PrintJson()` and `PrintYaml()`, with the notes. |
| `equation-<dialect>-<constants>` | `.txt`, `.tex`, `.mml` or `.typ` | `PrintEquation` in a dialect (`unicode`, `latex`, `asciimath`, `cstyle`, `mathml`, `typst`, `ascii`) with a constant style (`letters`, `word`, `logicsymbols`). |
| `equation-latex-doubledollar-<constants>` | `.tex` | The LaTeX equation between `$$` delimiters (`LatexWrapMode.DoubleDollar`). |
| `equation-latex-mathjax-<constants>` | `.md` | The LaTeX equation in the `` $`...`$ `` form for GitHub and MathJax (`LatexWrapMode.MathJaxSafe`). |
| `tree`, `mermaid`, `dot`, `html` | `tree.txt`, `mermaid.mmd`, `dot.dot`, `html.html` | `PrintPlainText()`, `PrintMermaid()`, `PrintDot()` and `PrintHtml()`. |
| `truth-table-<format>` | `.md`, `.csv`, `.json`, `.html`, `.tex` | `PrintTruthTable` in a [truth-table](truth-tables.md) format: `markdown`, `csv`, `json`, `html`, `latex`. |
| `statistics`, `statistics-text` | `statistics.json`, `statistics-text.txt` | `ValidationReportWriter.FormatStatistics` as JSON or text. |
| `diagnostics`, `diagnostics-json`, `diagnostics-sarif` | `.txt`, `.json`, `.sarif` | The diagnostics of every [validation level](#validation), as `DiagnosticWriter` writes them. |
| `report` | `report.md` | The [Markdown report](#report). |

An evaluation result (a decision) is not an artifact, because it needs stub values and a serializable decision record.

`ArtifactExporter.Render(rule, id)` gives the text of one artifact. `ArtifactExporter.Export(rule, artifact, writer)` writes it to a `TextWriter`, such as standard output, and `ArtifactExporter.Export(rule, artifact, path)` writes it to a file. Both write through `OutputWriter`, so the output has LF line endings and one final newline.

- A rule that does not compile gives no artifact, and the result has the compile diagnostics. The diagnostics artifacts are the exception: they show why the rule does not compile.
- A truth table over the cap of 8 terms is refused with a `TRE0035` error. With `ExportOptions.AllowLargerTruthTable`, the table is printed up to 12 terms with a `TRE0034` warning. `ExportOptions.TruthTableTermLimit` sets the most terms a table can have, with no ceiling (you own the 3^n cost). A number allows a table above 8 terms by itself, up to that number, and a number below 8 lowers the cap. A number below 1 throws `ArgumentOutOfRangeException`. `AllowLargerTruthTable` stays as the switch for the default limit of 12.
- An id that is not in the catalog is a `TWT0018` error that lists the ids.

### Export of every artifact

`ArtifactExporter.ExportAll(rule, folder, options)` writes every artifact to a folder, one file each, and then writes `manifest.json`. The rule is compiled and validated one time. A truth table over the cap (or over `TruthTableTermLimit`) is left out with a `TWT0015` warning that names the term limit of the export, and the manifest lists it as skipped, so a large rule still exports. A rule that does not compile writes nothing. The export does not delete other files of the folder.

<!-- doctest:skip a manifest, not a rule -->
```json
{
  "source": {
    "format": "text",
    "hash": "sha256:590bb31f50f9feefafe055eccf71bc92439dd5d51a70c81db7886cdff905adf3"
  },
  "artifacts": [
    {
      "id": "equation-latex-mathjax-letters",
      "file": "equation-latex-mathjax-letters.md",
      "format": "markdown",
      "options": {
        "constants": "letters",
        "dialect": "latex",
        "latexWrap": "mathjax"
      }
    }
  ],
  "skipped": []
}
```

- `source` has the format of the rule, the `path` of an embedded rule, and `hash`: SHA-256 of the rule text with its line endings made LF, in UTF-8 (`ArtifactExporter.HashOf`).
- `artifacts` has each file in catalog order: the id, the file name, the format and the printer options. The example shows one item.
- `skipped` has each artifact that was left out, with the reason.

The manifest has no absolute path and no time, so two exports of the same rule give identical folders. `ArtifactExporter.RenderAll(rule, options)` makes the same files without writing them, with `manifest.json` last. A front end compares them with a folder through `OutputWriter.Check` for a `--check` option.

### Images

The tools have no renderer of their own. `ImageRenderer.Render(diagram, language, path, options)` runs an external program:

| `DiagramLanguage` | Program | Command |
| --- | --- | --- |
| `Dot` | Graphviz `dot` | `dot -T<format> -o <output> <input>` |
| `Mermaid` | Mermaid CLI `mmdc` | `mmdc -e <format> -o <output> -i <input>` |

`ImageRenderOptions.Format` is `Svg` (the default) or `Png`. The program must be on the `PATH`, or on `ImageRenderOptions.SearchPath`. On Windows, the renderer looks for a `.exe`, `.cmd` or `.bat` file. The input is a temporary file. The program has `ImageRenderOptions.Timeout` (one minute by default) to finish.

- A program that is not on the search path is a `TWT0016` error. The message names the program and tells where to get it: Graphviz from `https://graphviz.org/download/`, and the Mermaid CLI from `npm install -g @mermaid-js/mermaid-cli`.
- A program that stops with an exit code other than 0, does not finish in time, or writes no file is a `TWT0017` error with the exit code and the error output of the program.

With `ExportOptions.Images`, `ExportAll` also renders `mermaid.mmd` and `dot.dot` as `mermaid.svg` and `dot.svg` (or `.png`). The manifest lists each image that was written, with the ids `mermaid-svg` and `dot-svg`, the `renderer` and the `source` file. A missing renderer is an error, but the text artifacts and the manifest are still written. The image bytes come from the external program, so they are only as deterministic as that program.

## Report

`RuleReport.Format(rule, options)` writes a Markdown report of a rule. It is also the `report` artifact. The report has these sections:

1. The source: the file name (not its directory), the path of an embedded rule, the format and the hash.
2. **Rule**: the rule text, broken to a width of 80 characters, in a `text` code block.
3. **Equation**: the LaTeX equation in the `` $`...`$ `` form, which GitHub draws with MathJax.
4. **Diagram**: the Mermaid flowchart in a `mermaid` code block, which GitHub draws.
5. **Truth table**: the Strong K3 table in Markdown. A rule over the cap gets a note in place of the table, with the term count and the row count. `ExportOptions.AllowLargerTruthTable` or `ExportOptions.TruthTableTermLimit` puts the table in and adds the `TRE0034` warning.
6. **Statistics**: a table of the [statistics](#statistics).
7. **Analysis**: the findings of the analysis level, each with its severity, code, tree path and message.

The report holds no time, so it is the same on each run. A rule that does not compile has no report; the result has the compile diagnostics.

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
| `TWT0015` | `ArtifactSkipped` | Warning | An export of every artifact left out an artifact that the rule cannot give. |
| `TWT0016` | `RendererNotFound` | Error | The program that renders a diagram as an image is not on the search path. |
| `TWT0017` | `RenderFailed` | Error | The program that renders a diagram as an image failed. |
| `TWT0018` | `UnknownArtifact` | Error | An artifact id is not in the artifact catalog. |
| `TWT0019` | `RuleRewritten` | Warning | A patch wrote one rule again in full; comments and layout inside the rule are not kept. |
| `TWT0020` | `RoundTripWriteBack` | Warning | A round-trip write-back serialized the whole document again; YAML comments and layout can be lost. |
| `TWT0021` | `WriteBackFailed` | Error | An edited rule cannot be written back into its document. |
| `TWT0022` | `InvalidRuleTestFile` | Error | A rule test file is not usable. |
| `TWT0023` | `UnsupportedJsonLogic` | Error | A JsonLogic document uses an operator or a shape that the importer cannot map. |
| `TWT0024` | `JsonLogicBehaviorDiffers` | Warning | An imported JsonLogic construct can give a different answer in TruthWeaver. |

## Packaging

`TruthWeaver.Tool.Core` has `IsPackable` set to `false`, so `dotnet pack` makes no package for it. The command line and [terminal UI](tui.md) tools carry the assembly in their own tool packages. The project is in `TruthWeaver.slnx`, so the CI build, test and analysis steps include it and its test project, `TruthWeaver.Tool.Core.Tests`.

The library references `TruthWeaver`, `TruthWeaver.Yaml` and `TruthWeaver.DataSources.Json`, and no other package. It takes no command-line or terminal UI library. The architecture tests check both rules.
