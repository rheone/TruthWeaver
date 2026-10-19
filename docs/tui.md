# Terminal UI

`truthweaver-tui` is the terminal workbench of TruthWeaver. It edits one rule in rule text, JSON or YAML, compiles it on every change, and shows the diagnostics and the canonical text of the rule next to the editor. You can give each term a value and see the decision, the trace and the reason for every node. Back to the [README](../README.md).

The workbench is a thin front end over the [tool core](tool-core.md), like the [command line](cli.md). It compiles every rule permissively, with no host registry, so any predicate name and any argument compile.

## Install

The package `TruthWeaver.Tool.Tui` is a .NET tool. It is not on NuGet. To install it, pack it from the repository and install it from the folder:

```shell
dotnet pack src/TruthWeaver.Tool.Tui --configuration Release --output ./packages
dotnet tool install --global TruthWeaver.Tool.Tui --add-source ./packages --prerelease
```

The command is `truthweaver-tui`. The tool runs on .NET 11 in a terminal that supports ANSI escape sequences, such as Windows Terminal or the usual terminals of Linux and macOS.

## Start

```shell
truthweaver-tui [rule-file | -] [--format text|json|yaml]
```

| Argument or option | Meaning |
| --- | --- |
| `rule-file` | The rule to open. The extension, then the content, selects the format, as [format detection](tool-core.md#format-detection) describes. With no file, the workbench starts with an empty rule text. |
| `-` | Reads the rule from standard input. The workbench opens it even when it does not compile, so the diagnostics show. The terminal library must still read keys from the console; this is not checked on every platform. |
| `--format text\|json\|yaml` | The format of the file or of standard input. Without it, the extension and then the content decide. |
| `-h`, `--help` | Writes the usage and exits. |
| `--version` | Writes the version and exits. |

The settings (operator style, theme and limits) come from the nearest `truthweaver.json` in the directory of the rule file or of the current directory, or in a parent directory, as for the [command line](cli.md#configuration).

| Exit code | Meaning |
| --- | --- |
| 0 | The workbench closed, or the help or version was written. |
| 2 | An unknown option, more than one file, a file that cannot be read, or a configuration error. The message is on standard error. |

## Layout

The window has three regions and a status line.

| Region | Pane | Shows |
| --- | --- | --- |
| Left | Rule | The rule text editor. |
| Right | Canonical | The canonical rule text of the tree. |
| Right | View | The rule in other forms. See [View pane](#view-pane). |
| Right | Tree | The nodes of the rule, for structural edits. See [Structural editing](#structural-editing). |
| Right | Rewrite | A rewrite before you accept it, and the comparison with an earlier version. See [Rewrites and compare](#rewrites-and-compare). |
| Right | Compare | A second rule, read-only, and how it relates to the open rule. See [Compare two rules](#compare-two-rules). |
| Bottom | Diagnostics | One line for each diagnostic: severity, code, line, column and message. |
| Bottom | Validation | The result of every validation level. See [Validation panel](#validation-panel). |
| Bottom | Assign | The terms with their values, the decision and the trace. See [Assign values](#assign-values). |
| Bottom | Make True | The smallest changes to the values that give the rule the target value. See [What would make this True](#what-would-make-this-true). |
| Bottom | Variables | The data for `from(...)` references. See [Variables](#variables). |
| Bottom | Tests | The rule test file and its cases. See [Rule tests](#rule-tests). |

The status line shows the file name, the format, a `*` for unsaved changes, the number of errors and warnings, the value of the selected term of the Tree pane (see [Structural editing](#structural-editing)), the last message, and `F1 keys`.

A region can hold more than one pane and shows one at a time. Its border shows the title of the pane and, for more than one pane, its position, such as `Canonical (1/2)`.

The sizes of the regions, as you move the dividers, and the theme, as you switch it, come back at the next start. See [Preferences file](#preferences-file).

## Editing

The rule compiles again after you stop typing. The compile waits 300 ms after the last key and runs on a background task, so a large rule never blocks the editor. While it waits or runs, the status line shows `checking...` and the diagnostics, the panes and the last valid tree still show the previous result. A new key restarts the wait and drops the result of the older text, so an old result never replaces newer text. An action that needs the current text, such as save, format, undo, export or a structural edit, compiles the typed text first.

- The editor colors the characters of each diagnostic span and underlines them: red for an error, yellow for a warning, cyan for information. With `NO_COLOR` the underline stays, an error is bold, and information is faint.
- When the text has an error, the workbench keeps the last tree that compiled. The Canonical pane shows that tree under the line `Last valid tree (the rule text has errors):`, and the status line says `showing the last valid tree`. When no text compiled yet, the pane says `No valid tree yet: the rule text has errors.`
- In the Diagnostics pane, Enter on a diagnostic moves the editor cursor to the start of its span.

Undo and redo work on one history for the document. Each change is one step: the typing between two pauses, formatting, and the conversion that a save in another format does. The workbench keeps the last 1000 steps.

## Format

F8 formats the rule in its own format in the canonical form. Shift+F8 formats it in the pretty form.

| Format | Canonical | Pretty |
| --- | --- | --- |
| Rule text | One line, in the operator style of the settings. | Lines of at most 80 characters. |
| JSON | One line. | Indented by two spaces. |
| YAML | One node on each line. | One node on each line. |

Formatting keeps the notes of the rule and its `let` definitions. It needs text that compiles. When the text has an error, the text does not change and the status line says why.

## Open and save

Ctrl+O opens a rule file in place of the current rule. F12 saves to a new path. Ctrl+S saves to the current path, or asks for a path when the rule has no file yet.

The extension of the save path selects the format: `.rule`, `.tw` or `.txt` for rule text, `.json` for JSON, `.yaml` or `.yml` for YAML. Another extension keeps the current format. A save in another format first converts the rule, with the same tree and the same notes, and the editor then shows the converted text. A conversion needs text that compiles.

The file is UTF-8 with no byte order mark and LF line endings, and it ends with one line feed.

When the rule has unsaved changes, opening another file and quitting ask first.

## Import and convert

F9 imports a rule. A dialog asks for the source (the clipboard or a file) and the format (detect it, rule text, JSON or YAML). A file that does not compile still opens, with its diagnostics. The clipboard takes only a valid expression: text that does not compile reports its first error and the open rule stays. Unsaved changes need your consent first.

F4 converts the open rule to the next format in the order rule text, JSON, YAML. Shift+F4 goes the other way. The conversion is one undo step. The tree, the notes and the `let` definitions stay the same: rule text `let` statements become a `definitions` section and `use` nodes in JSON and YAML, and the other way. The status line shows a banner that says what is not kept: the layout and spelling of the old text, and the number of YAML `#` comments when you convert YAML to another format. Text that does not compile is not converted.

## Export

F5 opens the export menu. It lists:

- All artifacts to a folder, with `manifest.json`.
- A switch for truth tables over the cap of 8 terms. A rule with more terms gets no truth table until you allow it, up to 12 terms, and the table then carries a `TRE0034` warning. A refused table says how to allow it.
- Every artifact of the [tool core catalog](tool-core.md): rule text in each operator style, JSON, YAML, each equation dialect and constant style (with the double dollar and MathJax envelopes), the plain text tree, Mermaid, DOT, HTML, the truth table in each format, the statistics, the diagnostics (text, JSON, SARIF) and the Markdown report.
- The Mermaid and DOT diagrams as SVG or PNG images.

For an artifact you choose the clipboard or a file. The text equals the output of the matching command line export. The artifacts are made from the text in the editor. If the text has errors, only the diagnostics artifacts can be made.

The images need Graphviz (`dot`) for DOT and the Mermaid CLI (`mmdc`) for Mermaid. A missing program is a message on the status line that names the program and where to get it. No dialog opens. An export of all artifacts includes the images only when both programs are installed, and otherwise says which program is missing.

## Validation panel

F7 validates the editor text on every level (compile, tree schema, limits, predicate schema, analysis and the conversion check) and reports the outcome on the status line. The Validation pane, the second pane of the bottom region, shows the result of each level, the diagnostics under their level, and the statistics of the rule. While the pane shows, it validates again after each change.

| Key in the pane | Does |
| --- | --- |
| Enter | Moves the editor cursor to the span of the diagnostic. |
| `r` | Validates again. |
| `s` | Turns strict mode on or off. |

Shift+F7 also turns strict mode on or off. In strict mode a warning fails the validation, as with the command line.

## View pane

The View pane is the second pane of the right region. Press Ctrl+PageDown in that region to show it. It prints the last valid tree of the rule in one form at a time. Each form calls the library printer through its `TextWriter` overload, so the text equals the output of that printer for the same options.

| Form | Printer |
| --- | --- |
| Tree | The plain text tree. |
| Rule text | The rule text in the operator style that you select. |
| Equation | The equation in one of the dialects: Unicode, LaTeX, AsciiMath, C-style, MathML, Typst or ASCII. |
| Mermaid | The Mermaid flowchart text. |
| DOT | The Graphviz DOT text. |
| HTML | The HTML tree. |
| JSON | The JSON tree format, with the notes. |
| YAML | The YAML tree format, with the notes. |

When the editor text has an error, the pane prints the last valid tree under the line `Last valid tree (the rule text has errors):`. When no text compiled yet, it says so.

A view that the printer refuses shows the reason in place of the text. For example, rule text in the C-style operators has no form that reads back, so the pane shows `Cannot show this view:` and the message of the printer. Copy and save do nothing for such a view, and the status line says why.

These keys work when the View pane has the focus.

| Key | Does |
| --- | --- |
| `v`, `V` | Shows the next or previous form. |
| `d` | Shows the equation in the next dialect. For LaTeX it goes through the three envelopes (none, double dollar, MathJax-safe) first. |
| `o` | Uses the next operator style: the printer default, word, symbolic, C-style. It applies to rule text, Mermaid, DOT and HTML. |
| `c` | Uses the next constant style: the printer default, letters, words, logic symbols. |
| `e` | Asks for the text of True, False and Unknown. An empty answer clears the override. The printer refuses a text that it cannot write, and the old texts stay. |
| `l` | Turns the simple variables on or off. It applies to equations and HTML. |
| `m` | Turns the marks of the current decision on or off. |
| `y` | Copies the text of the view to the clipboard. |
| `f` | Saves the text of the view to a file. The file ends with one line feed and has LF line endings. |

The tree, Mermaid, DOT and HTML views mark the decision for the values of the [Assign pane](#assign-values): the result of each node. A change of a value shows at once. The first line of the pane says `decision: none yet` while no text of the rule compiled. A terminal with no clipboard support makes `y` report that the clipboard is not available. Save the view to a file instead.

## Structural editing

The Tree pane is the third pane of the right region. It lists the nodes of the rule, one on each line and indented by depth: each operator with its count or range (such as `atLeast k=2`), each term in its canonical text, and each constant. The line above the list shows the path of the selected node (`$` is the root, `$.operands[1]` its second operand), its literals and its note. Move in the list to select a node.

Each term ends with a badge that gives its value under the [Assign pane](#assign-values), as a word: `[True]`, `[False]` or `[Unknown]`. A term that has no value there is `[Unknown]`. A term with a `from(...)` source shows the value of its resolved reference, or `[Unknown: <reason>]` while the reference has no value: the message of the fault, or `no data yet`. Operators and constants have no badge. When the selected node is a term, the status line adds its value, such as `isActive = True`. The badges and the status line follow each change of a value, of the variables or of the rule.

These keys work when the Tree pane has the focus. Each one acts on the selected node.

| Key | Does |
| --- | --- |
| `o` | Replaces the operator. A list of the operators opens. The operands stay. An operator that needs a count (`atLeast`, `atMost`, `greaterThan`, `lessThan`, `exactly`) gets `k=1`, and `between` gets `min=1` and `max` equal to the number of operands. |
| `w` | Wraps the node in `NOT`. |
| `n` | Negates the node: a `NOT` gives its operand, `True` and `False` swap, and `AND` and `NAND`, `OR` and `NOR`, and `XOR` and `EQUIVALENT` swap. Each pair are negations of each other in Strong K3. Any other node is wrapped in `NOT`. `NOT Unknown` is `Unknown`, so a negation of `Unknown` is refused. |
| `i` | Inserts an operand after the last operand of the selected operator. The palette opens. |
| Delete | Deletes the node. When an operator that needs two operands (such as `AND`) keeps only one, that operand takes the place of the operator. The root cannot be deleted. |
| `[`, `]` | Swaps the node with the previous or the next operand. |
| `p` | Replaces the node with an entry of the palette. |
| `l` | Changes a literal: the value of a constant, an argument of a term, or the `k`, `min` or `max` of an operator. A list opens when the node has more than one. The value is JSON (`"EU"`, `18`, `["a", "b"]`); text that is not JSON is a string. For a term, an empty value removes the argument. |
| `y` | Copies the node as rule text, with its notes. |
| `Y` | Copies the whole rule as rule text. |
| `v` | Pastes the expression on the clipboard in place of the node. |

The compiler checks every edit before it changes anything. An edit that would make an invalid rule, such as `NOT` with two operands, is refused: the rule stays as it was, and the status line says `Not applied:` with the first error. Pasted text that is not a valid expression is refused in the same way, with `Not a valid expression:`, its first diagnostic and the number of errors.

An edit that is applied writes the rule again in the format of the editor, with its notes: in the pretty form when the text has more than one line, and in the canonical form otherwise. The layout and the spelling of the old text are not kept. The editor, the Canonical and View panes and the decision of the Assign pane change at once. The edit is one undo step: Ctrl+Z gives back the text and the tree as they were, and Ctrl+Y does the edit again.

A structural edit needs text that compiles. When the editor text has an error, the Tree pane shows the last valid tree, and an edit is refused with `Cannot edit the tree: the rule text has errors`, so the text that you typed is not lost.

### Definitions in the tree pane

When the rule has `let` definitions (or a `definitions` section in JSON or YAML), the Tree pane shows them with the rule. The compiled rule has none, because the compiler replaces each use with the body of its definition, so the pane reads them from the editor text.

- A node that a use expands to starts with the use and its arguments, such as `use both(a, b) = and`. The nodes under it are the body of the definition with the arguments in place of the parameters. A use whose definition is itself a use names both: `use one = both(a, c) = and`. The decision of a use is the decision of that node.
- After the rule, each definition shows as `let name(parameter, ...)`, with its body under it. A parameter shows as `x (parameter)`, and a use inside a body shows as `use name(argument, ...)` with its arguments under it. The nodes of a definition have no decision, because a body has no value until a use gives the arguments. Their paths start with `$.definitions`, as in the JSON tree format.

A structural edit keeps the definitions. It changes the place in the text that the node comes from:

| Selected node | `o`, `l`, `i` change | `w`, `n`, `p`, `v` change | Delete, `[`, `]` change |
| --- | --- | --- | --- |
| The node that a use expands to | The body of the definition, for every use. | The use itself, for example `NOT both(a, b)`. | The operands of the parent. |
| A node inside an expanded use | The body of the definition, for every use. | The body of the definition. | The operands in the body. |
| A node in the place of a parameter | The argument of that use. | The argument of that use. | The parameters in the body. |
| A node of a definition (under `let`) | That node of the definition. | That node of the definition. | The operands in the body. |

An edit that would break a definition is refused with `Not applied:` and the first error. For example, an operand added to a use gives the wrong number of arguments, and `NOT` in place of a body operator with two operands is not valid. A parameter has no literals, so `l` cannot make it a term, and the `let` line itself cannot be edited: select its body. Pasted or typed rule text can use the definitions of the rule. A diagnostic about a node of a definition body points at that node in the text of the definition. The edit writes the definitions back in the format of the editor. Comments on a `let` statement as a whole are not kept.

### Palette

The palette opens with a search box above the list. Type to filter the list as you type: it shows the predicates whose name, label, family or description matches, and each match stays under its family line. Case does not matter, and every word must match. Clear the box to show the whole list again. Press Tab to move from the box to the list.

The palette lists, in this order:

- The first entry, `Type an expression`, and the second, `Search the predicate catalog`.
- The constants `True`, `False` and `Unknown`.
- The predicates of the predicate schema file of the settings (`schemaFile` in `truthweaver.json`), with their arguments and kinds.
- The predicates that the open rule uses, with the arguments of their first use.
- The ready-made predicates of the `TruthWeaver.Predicates` package, in family groups (String, Numeric, Date and time, and so on). A line with a family title is not an entry; picking it opens the list again. The workbench reads the factories of the package when it first needs the list, so a new factory appears with no change to the workbench.

`Type an expression` takes rule text, JSON or YAML. A predicate is a template: after you pick it, a form asks for the value of each argument, starting from the value of its first use in the rule. A string, GUID or date takes plain text; a number, a boolean or an array takes JSON; an array of strings also takes a list separated by commas. An empty value leaves out an argument that is not required. The operators of the palette are the list that `o` shows.

`Search the predicate catalog` asks for words and lists the ready-made predicates that match, in family groups. Case does not matter. Every word must match the name, the label, the family or the start of a word of the description. A short list of common phrases also finds a predicate: `older than` finds `IsBefore`, and `empty` finds `IsEmpty`.

- The `let` definitions of the open rule, with their parameters. A definition gives a use: a form asks for each argument as rule text, and the palette puts `name(argument, ...)` in place of the node.

When a schema file is set and does not declare the predicate that you picked from the catalog, the predicate goes into the rule and the status line adds `The schema file does not declare '<name>'; the schema validation level will flag it.` The schema file is never changed.

## Rewrites and compare

Ctrl+R opens a list of the rewrites of the library and applies the one you pick to the rule. The rule in the editor does not change until you accept the result: the result shows in the Rewrite pane, the fourth pane of the right region, and Ctrl+R shows that pane.

| Rewrite | Library method |
| --- | --- |
| Simplify | `SimplifyWithSteps`, with the list of its steps. |
| Canonicalize | `Canonicalize`. |
| Negation normal form | `ToNnf`. |
| Conjunctive normal form | `ToCnf`. |
| Disjunctive normal form | `ToDnf`. |
| Compress | `CompressToDerived`. |
| Expand | `ExpandToPrimitives`. |
| Optimize order | `OptimizeOrder`. The workbench has no predicate costs, so the order stays the same. |

The Rewrite pane shows:

- The diagnostics of the rewrite, such as `TRE0033` (the rewrite stopped at its pass limit), `TRE0031` (a threshold stays as it is in a normal form) or `TRE0016` (the result has more nodes than `maxRewriteNodeCount` of the settings, so there is no result).
- The size before and after: the number of nodes, the depth and the number of distinct terms.
- The rule text before and after, and the plain text tree before and after.
- For Simplify, each step: the law, and the subtree before and after the step.

These keys work when the Rewrite pane has the focus.

| Key | Does |
| --- | --- |
| `r` | Chooses and applies a rewrite, as Ctrl+R does. |
| `a` | Accepts the result: the rule in the editor becomes the result, in the format of the editor, as one undo step. Ctrl+Z gives back the rule before the rewrite, text and tree. A note stays on a node when the rewrite leaves that node as it was; a note on a node that the rewrite changed, moved or removed is dropped, and the status line says how many. |
| `x` | Discards the result. The rule does not change. |
| `c` | Compares the rule with the last saved or accepted version, as Ctrl+G does. |

A result is accepted only for the rule that it was made from. When the rule changed after the rewrite ran, `a` is refused, and you run the rewrite again. A result that would not compile when printed in the format of the editor is refused, and the status line gives the first error. A rewrite needs text that compiles.

Ctrl+G compares the rule with the last saved or accepted version: the rule when you opened or last saved the file, or the result of the last rewrite that you accepted, whichever came last. The pane shows:

- Whether the two rules are equivalent: the same value for every assignment of `True`, `False` and `Unknown` to the terms (`RuleEquivalence`). When they are not, the pane shows an assignment for which they differ.
- Whether the current rule is satisfied by the same assignments as the earlier version, by fewer (tighter) or by more (looser), with an example assignment (`RuleImplication`).
- The canonical text of both versions.

Both checks stop at `maxAnalysisTerms` of the settings (20 by default) distinct terms across the two rules. With more terms, the pane says that the comparison is undecided, and why.

## Compare two rules

Ctrl+F9 loads a second rule beside the open rule and shows the Compare pane, the fifth pane of the right region. It asks where the rule comes from: the clipboard or a file. The format (rule text, JSON or YAML) is detected. The second rule is read-only: no key edits it, and loading it does not change or dirty the open document.

The pane shows:

- The canonical text of both rules.
- How the second rule relates to the open rule (`RuleImplication`): equivalent (the same assignments satisfy both), tighter (satisfied by fewer assignments), looser (satisfied by more) or unrelated (some assignments satisfy both and each has some the other has not, or none satisfies both). An example assignment follows when the relation has one: one that satisfies only the open rule, or only the second rule.
- The overlap (`RuleOverlap`): whether both rules can be `True` for the same assignment, whether one can be `True` while the other is not, and, for each pair of different values that some assignment gives, an example assignment.
- The diagnostics of the second rule when it does not compile, and no comparison.

The comparison follows the open rule: it changes when you edit the rule. Both checks stop at `maxAnalysisTerms` of the settings (20 by default) distinct terms across the two rules, and then the pane says that the result is undecided, and why. An example assignment is a candidate: terms are opaque and independent, so the application may not be able to produce it.

These keys work when the Compare pane has the focus.

| Key | Does |
| --- | --- |
| `o` | Loads another second rule, as Ctrl+F9 does. |
| Enter | On a line with an example assignment: puts the example into the Assign pane in place of its term values. The variables stay. |
| `x` | Closes the second rule. |

## Assign values

The Assign pane tries the rule with values. It uses stub values: the workbench has no host predicates, so you give each term its value. A term with no value is `Unknown`.

The pane has four parts:

- The result line shows the result of the rule, `IsSatisfied` (only `True` is satisfied) and the number of faults.
- The policy line shows the policy for `Unknown`, and what `Collapse` and `Project` give under it.
- The term list shows the distinct terms of the rule. `[T]`, `[F]` and `[U]` are `True`, `False` and `Unknown`. A value always has a letter or a word, so no value depends on color.
- The trace tree shows each node with its value as text and in color. The line starts with `True`, `False` or `Unknown`, or with `skipped` for a node that short-circuiting skipped. The colors are green, red, yellow and gray. See [Themes and colors](#themes-and-colors). The reason line below the tree says why the selected node has its value.

In the term list, Space moves the selected term to the next value: `True`, `False`, `Unknown`. T, F and U set the value directly. Each change evaluates the rule again, and the decision and the trace change at once. The result equals `truthweaver evaluate` for the same values.

The reason lines show how `Unknown` acts. `AND` is `False` when one operand is `False`, even when another operand is `Unknown`. `OR` is `True` when one operand is `True`. `NOT` of `Unknown` is `Unknown`. When no operand settles the result, the line says that the answer is not settled.

The values stay when you edit the rule. When the text has an error, the pane evaluates the last valid tree and the result line says `(last valid tree)`.

Ctrl+F2 gives every term the value `Unknown`. Ctrl+F3 moves the policy: `Unknown` counts as `False` (fail-closed, the default), as `True` (fail-open), or is an error (`Collapse` gives `RejectedUnresolved` and `Project` gives no value).

## What would make this True

The Make True pane lists the smallest changes to the term values of the [Assign pane](#assign-values) that give the rule the target value. It calls the library search [`RuleCounterfactual.FindChanges`](counterfactuals.md) with the decision of the Assign pane, so it lists the same change sets as that call, fewest changes first. Press Ctrl+PageDown in the bottom region to show the pane.

Each entry is one line that names the terms to change, such as `b: False -> True, c: False -> True`. `(not read)` means the decision did not evaluate the term, so it has no value to change from.

| Key in the pane | Does |
| --- | --- |
| Enter | Applies the entry to the Assign pane. The values change and the decision follows at once. |
| `t` | Moves the target: `True`, `False`, `Unknown`. |
| `r` | Searches again. |

The pane searches while it shows, after each change of the rule or of a value. The search runs on a background task, so the editor stays responsive. The pane shows `Searching...` until the result arrives, and a result for older values never replaces a newer one.

The pane tells you when the list is not the whole answer:

- The decision already has the target value: `No change is needed`.
- No change of the terms gives the target value.
- The rule has more distinct terms than the search decides (20 by default): `Not searched:` and the reason.
- The search stopped at its cap of 10 sets: `Showing the 10 smallest sets; more exist.`
- The search stopped at its work limit of 100,000 steps: `Work limit reached`. The sets shown are the smallest found.

The search treats the terms as independent, so an entry can name values that cannot occur together in the application. An entry that changes a term that takes its value from a `from(...)` reference is refused, and the status line says why. When the text of the rule has an error, the pane searches the last valid tree, as the Assign pane does.

## Themes and colors

The workbench has a light and a dark color set. Each set paints its own background in every pane and dialog: the light set draws dark text on a light background, and the dark set draws light text on a dark background. The background of your terminal does not show through. The `theme` setting of `truthweaver.json` selects a set at start-up:

| `theme` | Colors |
| --- | --- |
| `auto` | The workbench asks the terminal for its background color (the xterm query OSC 11, with a time limit of 200 ms). It uses the light set for a light background. When the terminal does not answer, it uses the dark set. This is the default. |
| `light` | The set for a light background. |
| `dark` | The set for a dark background. |

Ctrl+F1 switches between the light and the dark set, in every pane and dialog at once. The switch does not write the `theme` setting. The workbench keeps the choice in the [preferences file](#preferences-file), and the next start uses it in place of the setting.

Each set has three backgrounds and one text color:

| Color | Light set | Dark set |
| --- | --- | --- |
| Pane background | white `#FFFFFF` | black `#000000` |
| Dialog and menu background | light gray `#F0F0F0` | dark gray `#202020` |
| Focus and selection background | light blue `#D6E8FF` | dark blue `#102848` |
| Text | near black `#181818` | light gray `#E6E6E6` |

The text and the value marks keep a contrast ratio of at least 4.5 : 1 (WCAG) against each of the three backgrounds of their set.

| Mark | Light set | Dark set |
| --- | --- | --- |
| `True` | green `#006E00` | green `#50FF50` |
| `False` | red `#B40000` | red `#FF5F5F` |
| `Unknown` | amber `#8C5A00` | yellow `#FFD700` |
| `skipped` | gray `#606060` | gray `#969696` |
| Editor error | red `#B40000` | red `#FF5F5F` |
| Editor warning | amber `#8C5A00` | yellow `#FFD700` |
| Editor information | blue `#005F87` | cyan `#5FD7FF` |

Color is never the only mark of a value. Every place that colors a value also writes it: `True`, `False`, `Unknown` or `skipped` in the trace, and `[T]`, `[F]` or `[U]` in the term list.

### NO_COLOR

When the environment variable `NO_COLOR` is set and not empty (see [no-color.org](https://no-color.org)), the workbench draws with the colors of your terminal only. The panes and dialogs show focus and selection with reverse video, hot keys with an underline, and disabled text as faint. The text marks above carry the values. Ctrl+F1 still switches the theme, but the screen does not change.

## Preferences file

The workbench keeps two choices between runs: the theme, after you switched it with Ctrl+F1, and the sizes of the regions, after you moved a divider. They are in one JSON file of the user, outside every project:

| System | Path |
| --- | --- |
| Windows | `%APPDATA%\TruthWeaver\workbench.json` |
| Linux and macOS | `$XDG_CONFIG_HOME/TruthWeaver/workbench.json`, or `~/.config/TruthWeaver/workbench.json` when `XDG_CONFIG_HOME` is not set |

```json
{
  "theme": "dark",
  "layout": { "editorWidthPercent": 60, "topHeightPercent": 70 }
}
```

- **Content.** The file holds `theme` (`light` or `dark`, never `auto`) and `layout` (the width of the editor region and the height of the top regions, in percent). It holds no rule text and no file path. The maximised region is not kept.
- **Theme.** A `theme` in the file wins over the `theme` setting of `truthweaver.json`. Without a switch, the file has no `theme`, and the setting applies. To use the setting again, delete the `theme` line or the file.
- **Layout.** The file is written when you move a divider, and the next start uses the sizes.
- **Problems.** A missing file, a file that is not valid JSON, or a file that is not an object gives the defaults and no message. A value that is not valid, such as `auto` or a size outside 15 to 85, is ignored and the other values stay. A file that cannot be written is ignored; the choice then lasts for the session only.

## Variables

A term with a `from("source", "query")` argument takes its value from data. The Variables pane is an editor for that data. The text is one JSON or YAML object. Each key is a source name, and its value is the data of that source:

<!-- doctest:skip variables document, not a rule -->
```json
{ "user": { "region": "EU" } }
```

Every edit applies at once. Ctrl+F4 loads a JSON or YAML file into the editor. A text with an error keeps the last data that was well-formed, and the note under the editor shows the problem.

A reference with no data is a fault. The term is `Unknown`, the result line counts the fault, and the term list shows `(no data yet)`. When the data resolves the reference, the term is listed with its value as for any other term. Its key is the term with the value from the data, such as `inRegion(region: "EU")`, as for a [stub-value file](tool-core.md#stub-values).

## Rule tests

The Tests pane saves an assignment as a case of a rule test file, runs the file, and loads a failing case back for debugging. The file has the format of [Rule tests](rule-tests.md), so `truthweaver test` runs it.

The pane has a field for the path of the file, a field for the name of the case, and the list of the cases.

- **Save.** Ctrl+F5 saves the current assignment as the case with the name in the name field. The case holds the values of the terms of the open rule, the variables when the rule has a `from(...)` reference, and the result and the number of faults that the decision has now. A new file gets the open rule as its inline `rule`. An existing file keeps its own rule, its comments and its other cases. A case with the same name is replaced; the replacement loses the `trace` expectation and the `variableFiles` of the old case. The rule text must compile. The cases must be a block list.
- **Open.** Enter in the path field reads the cases of an existing file.
- **Run.** Ctrl+F7 runs the cases of the file on the open rule, not on the rule in the file. Each case shows as `PASS` or `FAIL`. A failed case shows the result and the number of faults that it expects, and the ones it got. The line under the list gives the first reason for the selected case. The rule text must compile.
- **Debug.** Enter on a case loads its values and variables into the Assign pane and puts its name in the name field. The decision and the trace then show why the case fails. Save over the same name to change the case.
- **Remove.** Delete on a case removes it from the file. The last case of a file stays.

### Coverage over saved cases

The Tree pane shows which nodes of the rule the cases of the open rule test file reach. The workbench evaluates each case with its values and variables, and counts the decisions with [rule coverage](rule-coverage.md). The marks and the summary line are that result and nothing else.

- **Marks.** A node that no case evaluates ends with `[no case]`. A case skips a node when an earlier operand decides the result (short-circuit evaluation), so the mark finds branches that your cases never test. The mark is text, so it does not depend on color.
- **Summary.** The line under the selected node gives the share of the nodes that at least one case evaluates, such as `Coverage: 2 of 3 node(s) reached by 1 case(s) (67%).` A case that cannot be evaluated is not counted, and the line says how many.
- **No cases.** A rule with no saved cases shows `No saved cases: coverage is not measured.` and no marks. Press Enter in the path field of the Tests pane to read the cases of an existing file.
- **Updates.** The marks and the summary follow the rule and the cases. Saving, replacing or removing a case, and editing the rule, measure again.

## Rules inside JSON and YAML documents

A JSON or YAML document can hold rules among other content. F2 opens one of them in the editor. The editor then shows the rule alone, and the status line shows the file and the path of the rule, such as `policies.yaml:$.rules.publish`.

1. Press F2, then pick a file. Opening a rule text file this way reports that it has no paths.
2. Choose how to pick the rule:
   - **Browse the document tree.** The dialog lists every object, array and string of the document with its path, indented by depth. A rule in the tree format is one entry, labeled `rule (op: and)`. Enter on an entry opens it.
   - **Type a path.** The path is a [JSONPath-style path](tool-core.md#embedded-rules), such as `$.rules.publish` or `rules.publish`. A `*` segment (`rules.*`, `list[*]`) names several values.
3. A path that names nothing, or a value that is not a rule, shows the reason on the status line, and the editor keeps what it had. A path with `*` that names several values opens a list to pick one from. A path with `*` that names no rule says so.

The editor takes a string as rule text and an object or mapping as a rule in the format of the document.

Shift+F2 switches to another rule of the same document. It lists the rules in the tree format and the open rule. Unsaved changes need your consent first.

### Saving

Ctrl+S writes the rule back into the document. It changes only the rule's own span: the values that changed are replaced in place, so every byte outside the rule and every comment and key order stay as they were. The confirmation dialog says what will change before the file is written:

| Result | Meaning |
| --- | --- |
| Only the changed values are replaced | The shape of the rule did not change. Comments and layout stay, inside the rule too. |
| The rule's own text is written again | The shape changed, or the rule is a string of rule text. Comments and layout inside the rule can be lost. The rest of the file stays. A warning gives the reason. |
| Nothing to write | The rule equals the one in the file. The file is not touched, and the dialog does not show. |

If the rule in the file changed since you opened it, the dialog says so. Writing then replaces that version with yours.

F3 writes with the round-trip write-back: it reads the whole document and writes it again. A dialog warns first that YAML comments and the layout of the document may be lost. The file is written only after you agree.

The document file is never written without a confirmation. The file keeps its encoding, its byte order mark and its line endings. A rule whose text has errors is not written.

## Keys

The workbench is used with the keyboard. F1 shows this list in the window; F1 or Esc closes it.

| Key | Does |
| --- | --- |
| F1 | Shows or hides the key help. |
| Ctrl+O | Opens a rule file. |
| Ctrl+S | Saves. For a rule inside a document, it patches the rule into the document. |
| F12 | Saves to a new path. The extension selects the format. |
| Ctrl+Z | Undo. |
| Ctrl+Y | Redo. |
| F8 | Formats the rule: canonical. |
| Shift+F8 | Formats the rule: pretty. |
| F6, Shift+F6 | Moves the focus to the next or previous region. |
| Ctrl+PageDown, Ctrl+PageUp | Shows the next or previous pane of the region. |
| F11 | Maximises the region with the focus, or shows every region again. |
| Alt+Left, Alt+Right | Moves the line between the left and right regions. |
| Alt+Up, Alt+Down | Moves the line above the bottom region. |
| F4, Shift+F4 | Converts the rule to the next or previous format. |
| F5 | Opens the export menu. |
| F7 | Validates every level. |
| Shift+F7 | Turns strict validation on or off. |
| F9 | Imports a rule from the clipboard or a file. |
| F2 | Opens a rule inside a JSON or YAML document. |
| Shift+F2 | Switches to another rule of the open document. |
| F3 | Saves the open rule into its document with the round-trip write-back. |
| Ctrl+F1 | Switches between the light and the dark theme for this session. |
| Ctrl+F2 | Gives every term the value `Unknown`. |
| Ctrl+F3 | Moves the policy for `Unknown`. |
| Ctrl+F4 | Loads a variables file (JSON or YAML). |
| Space, T, F, U | In the term list of the Assign pane: moves the selected term to the next value, or sets it to `True`, `False` or `Unknown`. |
| `t`, `r`, Enter | In the Make True pane: change the target, search again, or apply the entry. See [What would make this True](#what-would-make-this-true). |
| Ctrl+F5 | Saves the assignment as a case of the rule test file. |
| Ctrl+F7 | Runs the rule test file on the open rule. |
| `o`, `w`, `n`, `i`, Delete, `[`, `]`, `p`, `l`, `y`, `Y`, `v` | In the Tree pane: edit the selected node, copy or paste. See [Structural editing](#structural-editing). |
| Ctrl+R | Chooses a rewrite and shows the result in the Rewrite pane. |
| Ctrl+G | Compares the rule with the last saved or accepted version. |
| Ctrl+F9 | Loads a second rule from a file or the clipboard and shows how it relates to the open rule in the Compare pane. |
| `o`, Enter, `x` | In the Compare pane: load another rule, load the example on the line into the Assign pane, or close the second rule. See [Compare two rules](#compare-two-rules). |
| `r`, `a`, `x`, `c` | In the Rewrite pane: choose a rewrite, accept it, discard it, or compare. See [Rewrites and compare](#rewrites-and-compare). |
| Ctrl+Q | Quits. Esc, the quit key of Terminal.Gui, also quits. Both ask first when the rule has unsaved changes. |

The editor has the usual editing keys of the Terminal.Gui editor view, such as Ctrl+C, Ctrl+X and Ctrl+V for the clipboard and Ctrl+A to select all.

The mouse also works, and F1 says so. A click on a pane gives the focus to its region, a click on a line of the Tree pane selects that node, and a click in the editor moves the cursor. The wheel scrolls the pane under the pointer. A drag on the right or bottom border of the Rule region resizes it. The mouse is never the only way: every action has a key above. A terminal that does not report the mouse works with the keys.

## Terminal.Gui

The workbench uses [Terminal.Gui](https://github.com/tui-cs/Terminal.Gui) 2.5.0 for the window, the editor, the dialogs and the keys. No other project of the repository references it, and an architecture test checks this. The three text views of the workbench (the rule editor, the variables editor and the read-only view pane) are editor views of the `Terminal.Gui.Editor` package.

| Item | Status |
| --- | --- |
| Version | 2.5.0, the newest stable release on NuGet (published 2026-09-11). Version 2.6 is only in pre-release builds. |
| License | MIT. |
| Target | A `net10.0` build, which runs on .NET 11. |
| Trimming and native AOT | The assembly sets `IsTrimmable` and `IsAotCompatible`, and the workbench builds with no trim or AOT warning. The tool package is framework-dependent and is not published as a native AOT program. |
| Packages it brings | ColorHelper, JetBrains.Annotations, Markdig, Microsoft.Extensions.Configuration (with Binder, Json and Options), Microsoft.Extensions.Logging.Abstractions, System.IO.Abstractions, TextMateSharp with TextMateSharp.Grammars (which brings Onigwrap and its native libraries), and Wcwidth. They reach only this tool. |
| Text editor | Terminal.Gui 2.5 marks its `TextView` obsolete in favor of the editor of the separate `Terminal.Gui.Editor` package, version 2.5.7 (MIT, a `net10.0` build). The workbench uses that editor and does not use `TextView`. Only this tool references the package, and an architecture test checks this. The rule editor colors diagnostics with a line transformer and leaves undo and redo to the document history. The editors show no line numbers. |

## Packaging

CI packs `TruthWeaver.Tool.Tui` with the other packages and checks the package. A release publishes it with the other packages, so a pre-release tag such as `v1.0.0-alpha.20261018T201500Z` puts the alpha of the tool on NuGet.org.

## Extending the workbench

This section is for developers of the workbench. The window gets its panes and keys from two lists in `src/TruthWeaver.Tool.Tui`, in the namespace `TruthWeaver.Tool.Tui.Workbench`. The types are in one namespace for each folder of the project, `TruthWeaver.Tool.Tui.<Folder>`; the [project README](../src/TruthWeaver.Tool.Tui/README.md) lists the folders:

- A pane implements `IWorkbenchPane`: a title, a region (`PaneSlot.Editor`, `PaneSlot.Side` or `PaneSlot.Bottom`) and `CreateView(Workbench)`. Add an instance to `WorkbenchPanes.Default`. The view follows the shared state through `Workbench.DocumentChanged`, and reads the rule from `Workbench.Document`.
- A key is a `WorkbenchCommand`: a key, a description and an action on the window. Add it to `WorkbenchCommands.Default`. The key help lists it.

The state that the panes share has no view: `AssignmentSession` in the tool core holds the values, the variables and the policy (see [Trying a rule with values](tool-core.md#trying-a-rule-with-values)), `Workbench.Outcome` is its result for the open document, and `Workbench.NotifyAssignmentChanged()` tells the panes after a change. `RuleTestSession` holds the rule test file of the workbench, and `RuleTestCaseText` in the tool core edits the text of that file. `RuleDocument` in the tool core holds the text, the compiled rule, the diagnostics and the history (see [Editing a rule](tool-core.md#editing-a-rule)), and `Workbench` holds the document, the layout and the status line. `Workbench.SelectedNode` is the path of the selected node of the tree, and `Workbench.SelectedNodeChanged` tells the panes after `Workbench.SelectNode(path)`. `Workbench.ReplaceRule(rule, message)` puts an edited rule into the document as one undo step; `RuleTreeEditor` and `RulePalette` in the tool core make the edits (see [Editing the tree](tool-core.md#editing-the-tree)). `RewritePlayground` in the tool core applies a rewrite without a change to the rule and compares two versions (see [Rewrites](tool-core.md#rewrites)). `WorkbenchWindow.ShowPane(title)` shows a pane and gives it the focus. The tests in `tests/TruthWeaver.Tool.Tui.Tests` drive the window with keys without a terminal, and run the application loop once with the ANSI driver of Terminal.Gui.
