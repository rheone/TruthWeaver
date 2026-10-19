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

The window has a menu bar on the first row, three regions, a status line and a hint line.

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

The status line shows the file name, the format, a `*` for unsaved changes, the number of errors and warnings, the value of the term under the editor cursor or of the selected term of the Tree pane, whichever you moved last (see [Structural editing](#structural-editing)), and the last message.

The hint line below it shows the most common keys: `F1 Menus`, `Ctrl+O Open`, `Ctrl+S Save`, `Ctrl+Z Undo`, `F6 Region`, `F5 Export`, `F7 Validate` and `Ctrl+Q Quit`. The menu bar holds every command. See [Menus and keys](#menus-and-keys).

A region can hold more than one pane and shows one at a time. Its border shows the title of the pane and, for more than one pane, its position, such as `Canonical (1/2)`.

The sizes of the regions, as you move the dividers, and the theme, as you switch it, come back at the next start. See [Preferences file](#preferences-file).

## Editing

The rule compiles again after you stop typing. The compile waits 300 ms after the last key and runs on a background task, so a large rule never blocks the editor. While it waits or runs, the status line shows `checking...` and the diagnostics, the panes and the last valid tree still show the previous result. A new key restarts the wait and drops the result of the older text, so an old result never replaces newer text. An action that needs the current text, such as save, format, undo, export or a structural edit, compiles the typed text first.

- The editor colors the characters of each diagnostic span and underlines them: red for an error, yellow for a warning, cyan for information. With `NO_COLOR` the underline stays, an error is bold, and information is faint.
- The editor also colors each kind of rule text in its own color: predicates, operators, variables, constants and comments (the description text). See [Syntax colors](#syntax-colors). A diagnostic mark wins over a syntax color where the two overlap. The colors follow the compile: they update when the compile after a pause in typing finishes, never on a single key, and they also show for a rule that has errors.
- When the text has an error, the workbench keeps the last tree that compiled. The Canonical pane shows that tree under the line `Last valid tree (the rule text has errors):`, and the status line says `showing the last valid tree`. When no text compiled yet, the pane says `No valid tree yet: the rule text has errors.`
- In the Diagnostics pane, Enter on a diagnostic moves the editor cursor to the start of its span.
- An empty rule, or a rule with only white space, shows a hint in the Diagnostics pane in place of the `TRE0001` error: type a rule, or press Shift+F9 to choose a sample rule. F10 opens the menus. As soon as the rule has text, its real diagnostics show again.

Undo and redo work on one history for the document. Ctrl+Z undoes and Ctrl+Y redoes; the Edit menu shows both keys. Each change is one step, whether it comes from the editor or from a pane: the typing between two pauses, a structural edit of the Tree pane (insert, delete, replace, wrap, swap), an accepted rewrite, formatting, a conversion with F4 or Shift+F4, the conversion that a save in another format does, and a sample rule. Undo gives back the text and the format together. Opening a file, importing and opening a rule inside a document start a new document with a new history. The workbench keeps the last 1000 steps.

## Sample rules

The File menu has a **Samples** submenu, and Shift+F9 opens the same list in a dialog. Each sample has a name and a one line description, and its rule text carries notes (comments) that explain the logic. Choose a sample to put it in place of the rule. Unsaved changes need your consent first, as for Ctrl+O. The sample is one undo step, so Ctrl+Z gives back the rule you had, in its format. A sample is rule text: when the open rule is JSON or YAML, the editor changes to rule text.

| Sample | Shows |
| --- | --- |
| Predicates with arguments | Named arguments of text, number and boolean values. |
| AND, OR and NOT | The basic connectives and how `Unknown` passes through. |
| XOR and EQUIVALENT | Exclusive or, and two values that must agree. |
| IMPLIES | The implication, False only for True then False. |
| NAND and NOR | Negated AND and negated OR. |
| PARITY | An odd number of True operands. |
| AtLeast, AtMost and Exactly | Counting True operands against a number. |
| ExactlyOne | Exactly one True operand. |
| ANY, ALL and NONE | Some, every or no operand is True. |
| BETWEEN | The count of True operands lies in a range. |
| If | One of two results chosen by a condition. |
| IsTrue and IsUnknown | Questions about the value itself. |
| COALESCE | The first operand that is not `Unknown`. |
| let definitions | A named sub-expression with parameters. |
| Quantifiers | `FORALL` and `EXISTS` over a collection. It reports `TRE0042` on purpose. |
| from(...) variables | An argument read from a data source at run time. |
| Notes | Line and block comments as notes of nodes. |

The workbench compiles with an empty registry, so it has no collection to range over. A rule with `FORALL` or `EXISTS` reports `UnknownCollection` (`TRE0042`) here. The Quantifiers sample shows this diagnostic on purpose: its notes explain that a host must declare the collection. Every other sample compiles. See [Quantifiers](rule-text.md#quantifiers) for the syntax.

## Generate a rule

Rule > Generate rule (Shift+F5) replaces the rule with a semi-random rule that compiles, so you can try the panes without writing a rule. Three questions come one after the other: the seed (blank is a random seed), the depth (1 to 5, default 3) and the operator families. The families dialog has one tick box for each group of the Insert menu: Connectives (ticked at first), Cardinality, Inspection (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`) and Other (`If`, `COALESCE`). Each box stands for the operators that the same group of the Insert menu lists. Tick any set of them; OK with no box ticked keeps the dialog open. The terms are calls of predicates from the catalog with sample arguments, so the rule reads like a real rule.

The same seed, depth and set of families give the same rule text. The status line shows the seed, so you can repeat or share a rule. Some generated rules do not compile; the action then tries again, up to 50 times, and says so on the status line if none compiles (try another seed). It never changes the rule in that case. Unsaved changes need your consent first, as for Ctrl+O. The generated rule is one undo step, so Ctrl+Z gives back the rule you had. A generated rule is rule text: when the open rule is JSON or YAML, the editor changes to rule text.

## Insert menu

The Insert menu puts rule text into the editor at the cursor. If text is selected, the insert replaces the selection. The cursor goes after the inserted text, and the insert is one step in the undo history (Ctrl+Z). No entry has a key; open the menu with Alt+I.

| Entry | Inserts |
| --- | --- |
| Predicate... | A call of a predicate with a placeholder for each argument. A search list shows the predicates of the rule, of the schema file and of the catalog, each with its description. Type words to narrow the list. |
| Operators | A submenu for each group: Connectives (`NOT`, `AND`, `OR`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `PARITY`), Cardinality (`ANY`, `ALL`, `NONE`, `ExactlyOne`, `AtLeast`, `AtMost`, `Exactly`, `GreaterThan`, `LessThan`, `BETWEEN`), Inspection (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`) and Other (`If`, `COALESCE`). The operands are the placeholder terms `a`, `b` and `c`. An infix operator is in parentheses. |
| Constants | `True`, `False`, `Unknown`, and the letters `T`, `F` and `U`. |
| Variable... | A `from("source", "query")` reference. The workbench asks for the data source and the query. Put the cursor where a predicate argument value goes. |
| New definition | A `let` definition with two parameters. Put the cursor at the start of the rule, and give the definition a name and a body. |
| Use definition... | A use of one of the `let` definitions of the open rule, with its parameter names as placeholders. A list shows the definitions. |

Each entry shows its name in the color of its kind (operators, constants, predicates and variables) and its description in the description color; the name of the entry or of its submenu also says the kind. See [Syntax colors](#syntax-colors). Each operator and constant shows a one-line description next to its name. The description is the one the library gives for the operator, the same text that the Tree pane shows. The description of a predicate comes from its catalog entry.

Insert adds rule text, so the menu works only when the open rule is rule text. If the rule is JSON or YAML, no entry inserts anything, and the status line says that Insert is not available. Convert the rule to rule text with F4 to use it.

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

The title of the window shows the file name and, in front of it, a `*` while the rule has unsaved changes: `* rule.rule - TruthWeaver workbench`. The `*` appears after an edit and goes away after a save. A rule with no file yet shows `(new rule)`.

### Recent files

The File menu has an **Open recent** submenu under **Open**. It lists the last ten files that you opened, newest first, each with its number and path. A long path keeps its end. A file that opened again moves to the top, and the eleventh file pushes out the oldest. A file that you gave on the command line counts as opened.

Choose an entry to open that file. Unsaved changes need your consent first, as for Ctrl+O. A file that no longer exists shows `(missing, choose to remove)`. Choosing it takes it out of the list, shows a message on the status line, and keeps the open rule. The list is in the [preferences file](#preferences-file) and holds paths only.

## Import and convert

F9 imports a rule. A dialog asks for the source (the clipboard or a file) and the format (detect it, rule text, JSON or YAML). A file that does not compile still opens, with its diagnostics. The clipboard takes only a valid expression: text that does not compile reports its first error and the open rule stays. Unsaved changes need your consent first.

F4 opens the Convert dialog: a list of the forms to convert to, and the buttons Convert and Cancel. The list has canonical rule text, formatted (pretty) rule text, JSON and YAML. Formatted rule text wraps a long rule over several lines at 80 characters and keeps the notes, like Shift+F8 does. Shift+F4 converts to the next format in the order rule text, JSON, YAML without a dialog. A conversion is one undo step. The tree, the notes and the `let` definitions stay the same: rule text `let` statements become a `definitions` section and `use` nodes in JSON and YAML, and the other way. The status line shows a banner that says what is not kept: the layout and spelling of the old text, and the number of YAML `#` comments when you convert YAML to another format. Text that does not compile is not converted.

## Export

F5 opens the export dialog. One dialog holds all the choices, so no second dialog opens.

- **Output type.** A list of the kinds of output: rule, equation, tree and diagram, truth table, statistics and diagnostics, report, image, and every artifact to a folder. The chosen type stays highlighted.
- **Format.** A list of the formats of the chosen type, taken from the [tool core catalog](tool-core.md), so the dialog holds every artifact of the catalog. Under Rule: rule text (canonical, symbolic and formatted), JSON and YAML. Under Tree and diagram: the text tree, Mermaid, DOT and HTML. Under Statistics and diagnostics: the statistics and the diagnostics (text, JSON and SARIF). The report has its own format. The chosen format stays highlighted. For the truth table, the list shows every format of the tool: Markdown, CSV, JSON, HTML and LaTeX. For the equation, the dialog shows three dropdowns: the dialect, the LaTeX envelope (only for the LaTeX dialect) and the constant style.
- **Options.** A box allows truth tables over the cap of 8 terms, up to 12 terms. A **term limit** field sets the most terms of a truth table (empty for none). A number allows a table of that size with no ceiling, and the table grows by 3^n rows. The field must be empty or a whole number of 1 or more. Otherwise the dialog stays open on it. Both options carry a `TRE0034` warning, and both stay for the next export. A refused table says how to allow it. When both options are set, the term limit wins. The box only removes the default limit while the field is empty. A line under the field says so.
- **File or folder.** A text field with a suggested path in the folder of the open rule. It follows the chosen format until you edit it.

The buttons are **Copy**, **To File** and **Cancel**. Copy is the default action. Enter in the Output type list moves the focus to the formats (or to the dropdowns of the equation) and does not end the dialog. Enter in the Format list copies, or writes to the file when the entry cannot be copied. In the folder export, the Output type list has no formats. There, Enter moves the focus to the path field and writes nothing. Enter in the path field writes to the folder. To File writes to the path in the text field of the same dialog. A file that exists needs your confirmation. For the images and for every artifact to a folder, Copy is off and To File is the default. Cancel changes nothing.

A click selects an entry. A double click also only selects it and never ends the dialog. Tab moves between the lists, the dropdowns, the box, the field and the buttons. Alt+C, Alt+T and Alt+L press Copy, To File and Cancel.

The text of an artifact equals the output of the matching command line export. The artifacts are made from the text in the editor. If the text has errors, only the diagnostics artifacts can be made.

The command line has the same artifacts and the same truth-table options (`--allow-large-truth-table` and `--truth-table-limit`); see [export](cli.md#export). The command line also has `--output`, `--check` and the JSON result, and the dialog also has the clipboard. The dialog offers the images that `export --render` writes: Mermaid and DOT as SVG and PNG.

The dialog lists the image formats of a diagram language only when its program is found. DOT images need Graphviz (`dot`). Mermaid images need the Mermaid CLI (`mmdc`). When a program is missing, the dialog has no entries for that language. One line under the lists, always visible, names the program and where to get it. When both programs are missing, the Image type is not in the list. A missing program never leaves a dead entry. An export of all artifacts includes the images only when both programs are installed. Otherwise it says which program is missing.

## Validation panel

F7 validates the editor text on every level (compile, tree schema, limits, predicate schema, analysis and the conversion check) and reports the outcome on the status line. The Validation pane, the second pane of the bottom region, shows the result of each level, the diagnostics under their level, and the statistics of the rule. While the pane shows, it validates again after each change.

| Key in the pane | Does |
| --- | --- |
| Enter | Moves the editor cursor to the span of the diagnostic. |
| `r` | Validates again. |
| `s` | Turns strict mode on or off. |

Shift+F7 also turns strict mode on or off. In strict mode a warning fails the validation, as with the command line.

## View pane

The View pane is the second pane of the right region. Press Ctrl+PageDown (or Alt+W, for a terminal that swallows
Ctrl+PageDown; see [Terminals that swallow keys](#terminals-that-swallow-keys)) in that region to show it. It prints the last valid tree of the rule in one form at a time. Each form calls the library printer through its `TextWriter` overload, so the text equals the output of that printer for the same options.

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

Each term ends with a badge that gives its value under the [Assign pane](#assign-values), as a word: `[True]`, `[False]` or `[Unknown]`. A term that has no value there is `[Unknown]`. A term with a `from(...)` source shows the value of its resolved reference, or `[Unknown: <reason>]` while the reference has no value: the message of the fault, or `no data yet`. Operators and constants have no badge. When the selected node is a term, the status line adds its value, such as `isActive = True`. The editor does the same: put the cursor on a predicate call or inside its arguments (a `from(...)` variable too), and the status line adds the value of that term as a word. The status line shows the value for the control you moved last, the editor cursor or the Tree selection. It shows no value while the rule is being checked (`checking...` is on the status line), while the text has errors, when the open rule is JSON or YAML, or when the cursor is on an operator or a constant. The badges and the status line follow each change of a value, of the variables or of the rule.

### Inspect a part of the rule

Analyze > Inspect (Ctrl+F8 or Alt+M) describes the part of the rule at the cursor in the editor. When another pane has the focus, it describes the selected node. The text shows in a box above the status line.

In the editor, the character under the cursor decides. When that character has no description (a space, or the end of the text), the character before the cursor decides. So the key describes the word you just typed. A cursor between two words describes the word that starts under it.

The box stays open until you press Esc, click, or change the rule or a value. A change closes the box because its text would describe the old rule. Moving the pointer does not close it.

The mouse shows the same box. Rest the pointer on a part of the editor text, or on a line of the Tree pane, for half a second. The box opens at the pointer. It closes when the pointer moves to another part or when you click. After Esc closes the box, it stays closed while the pointer stays inside the same part. It opens again after the pointer moves to another part.

The key and the pointer use one function for the text, so they always say the same. Some terminals, such as some SSH and tmux set-ups, send no mouse movement. They show no hover box, but the key shows everything.

What the text says depends on the part. Values are words (`True`, `False`, `Unknown`, `skipped`), never only a color.

| Part | The text |
| --- | --- |
| Operator | The name and the number of operands, the description of the operator table, the value it returned now (`skipped` when short-circuiting never reached it), a truth table for `IMPLIES` and the three cases of `If`, and a mark for an external operator (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`, `COALESCE`). |
| Cardinality operator | The threshold (`k`, `min` or `max`) and how many operands are True, False, Unknown and skipped now. |
| Predicate or term | The description, the family and the arguments with their kinds from the predicate catalog, the schema file or the rule, what the predicate answers for a null value when it takes a null behavior, and its value now. |
| Variable | For `from("source", "path")`: the source, the path, and the resolved term or the reason there is no data yet. For a parameter of a `let`: the definition it belongs to. |
| Constant or letter | Which value `T`, `F` or `U` stands for, and its value. |
| `let` definition or use | The parameters and the body of the definition, and how many times the rule uses it. |
| Note | The full text of the comment. |
| Error or finding in the editor | The severity and code, the full message, the expected and found text, the suggestion, and the page `docs/diagnostics.md`. |
| Root of the tree | The findings of the analyzer for the whole rule, such as an operand that cannot change the result. |

In a JSON or YAML document, the editor text has no inspect text, except for errors. Use the Tree pane instead. A name that appears more than once as an operator (two `AND`) shows the description only, because the editor cannot tell which one you mean. Select the node in the Tree pane to see its value.

### Step through an evaluation

Analyze > Next evaluation step (Shift+F12 or Alt+J) and Previous evaluation step (Shift+F11 or Alt+K) walk through the evaluation of the rule under the values of the Assign pane, one node at a time. The steps use the trace of the evaluation, in evaluation order: the operands of an operator first, then the operator. The first press starts at the first step. Each step selects its node in the Tree pane and the status line says what happened, such as `Step 2 of 3: b = skipped`.

The line of each node that a step has reached ends with `=> ` and the value as a word: `True`, `False` or `Unknown`. A node that short-circuiting never evaluated shows `=> skipped`; the operands under a skipped node have no steps. A term that faulted adds the message of the fault to its step, such as `Step 1 of 3: isActive = Unknown (fault: ...)`. Stepping back removes the mark of the step you leave. Stepping changes no result.

Esc leaves the step mode and removes the marks. A change of a value or of the rule leaves it too, because the steps would describe the old evaluation.

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

A list dialog, with or without a search box, ends only when you choose a real entry. Enter, or a double click, on an entry chooses it. Enter on a family line such as `--- Numeric ---` moves the highlight to the first entry of that family, and the dialog stays open with the search text and the scroll position. A double click on a family line, on blank space below the rows, or on an empty search result does nothing. Each change to the search box moves the highlight to the first matching row, so the highlight never goes missing and Enter always takes the top match.

The palette lists, in this order:

- The first entry, `Type an expression`, and the second, `Search the predicate catalog`.
- The constants `True`, `False` and `Unknown`.
- The predicates of the predicate schema file of the settings (`schemaFile` in `truthweaver.json`), with their arguments and kinds.
- The predicates that the open rule uses, with the arguments of their first use.
- The ready-made predicates of the `TruthWeaver.Predicates` package, in family groups (String, Numeric, Date and time, and so on). A line with a family title is not an entry; picking it opens the list again. The workbench reads the factories of the package when it first needs the list, so a new factory appears with no change to the workbench.

Every predicate list, in the palette and in the Predicate entry of the Insert menu, writes each row in the color of its kind. The first word of the row also names the kind: `operator` and `constant` are those kinds, and `schema`, `catalog`, `rule` and `let` are predicate sources. Under the list, a line shows the description of the chosen row in the description color, and a legend line names the five colors. See [Syntax colors](#syntax-colors).

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

Ctrl+F9 (or Alt+B) loads a second rule beside the open rule and shows the Compare pane, the fifth pane of the right region. It asks where the rule comes from: the clipboard or a file. The format (rule text, JSON or YAML) is detected. The second rule is read-only: no key edits it, and loading it does not change or dirty the open document.

The pane shows:

- The canonical text of both rules.
- How the second rule relates to the open rule (`RuleImplication`): equivalent (the same assignments satisfy both), tighter (satisfied by fewer assignments), looser (satisfied by more) or unrelated (some assignments satisfy both and each has some the other has not, or none satisfies both). An example assignment follows when the relation has one: one that satisfies only the open rule, or only the second rule.
- The overlap (`RuleOverlap`): whether both rules can be `True` for the same assignment, whether one can be `True` while the other is not, and, for each pair of different values that some assignment gives, an example assignment.
- The diagnostics of the second rule when it does not compile, and no comparison.

The comparison follows the open rule: it changes when you edit the rule. Both checks stop at `maxAnalysisTerms` of the settings (20 by default) distinct terms across the two rules, and then the pane says that the result is undecided, and why. An example assignment is a candidate: terms are opaque and independent, so the application may not be able to produce it.

These keys work when the Compare pane has the focus.

| Key | Does |
| --- | --- |
| `o` | Loads another second rule, as Ctrl+F9 (or Alt+B) does. |
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

Ctrl+F2 (or Alt+U) gives every term the value `Unknown`. Ctrl+F3 (or Alt+P) moves the policy: `Unknown` counts as `False` (fail-closed, the default), as `True` (fail-open), or is an error (`Collapse` gives `RejectedUnresolved` and `Project` gives no value).

## What would make this True

The Make True pane lists the smallest changes to the term values of the [Assign pane](#assign-values) that give the rule the target value. It calls the library search [`RuleCounterfactual.FindChanges`](counterfactuals.md) with the decision of the Assign pane, so it lists the same change sets as that call, fewest changes first. Press Ctrl+PageDown (or Alt+W) in the bottom region to show the pane.

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

Ctrl+F1 (or Alt+G) switches between the light and the dark set, in every pane and dialog at once. The switch does not write the `theme` setting. The workbench keeps the choice in the [preferences file](#preferences-file), and the next start uses it in place of the setting.

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

### Syntax colors

The rule text editor, the predicate lists and the Insert menu give each kind of text its own color. Each color keeps a contrast ratio of at least 4.5 : 1 (WCAG) against all three backgrounds of its set.

| Kind | What it is | Light set | Dark set |
| --- | --- | --- | --- |
| predicate | A predicate call or a bare name, such as `isAdult(age: 18)`, and the name of a `let` definition | blue `#0040AA` | blue `#78AAFF` |
| operator | A word such as `AND`, `NOT`, `AtLeast`, `let`, `FORALL` or `IN`, or a symbol such as `&&` or `!` | purple `#7028AA` | lilac `#CD96FF` |
| variable | A `from(...)` read, a parameter of a `let` definition and the variable of a quantifier, in the head and in every use | teal `#00695F` | aqua `#50DCC8` |
| constant | `True`, `False`, `Unknown`, and the letters `T`, `F` and `U` | rose `#A5005F` | pink `#FF8CC8` |
| description | A `//` or `/* */` comment in the editor, and the description of an entry in a list or in the Insert menu | slate `#505A64` | gray-blue `#96A5B4` |

The editor reads the words, symbols, strings and comments of the text. It does not need a rule that compiles. Names that a `let` or a quantifier binds count for the whole text, not by scope. The name of an argument (`age:` in `isAdult(age: 18)`), a string and a number keep the text color.

Color is never the only mark of a kind. The legend line `predicate  operator  variable  constant  description` names the kinds in their colors at the bottom of the predicate lists and in the key reference (Shift+F1), and the lists write the kind as the first word of a row. With `NO_COLOR` the weight tells the kinds apart: an operator is bold, a predicate is italic, a variable is bold and italic, a constant is underlined and a description is faint. The underline of a constant looks like the underline of a diagnostic, but a diagnostic mark wins where they overlap, and an error is also bold.

Color is never the only mark of a value. Every place that colors a value also writes it: `True`, `False`, `Unknown` or `skipped` in the trace, and `[T]`, `[F]` or `[U]` in the term list.

### NO_COLOR

When the environment variable `NO_COLOR` is set and not empty (see [no-color.org](https://no-color.org)), the workbench draws with the colors of your terminal only. The panes and dialogs show focus and selection with reverse video, hot keys with an underline, and disabled text as faint. The text marks above carry the values, and the weights of the [syntax colors](#syntax-colors) carry the kinds. Ctrl+F1 (or Alt+G) still switches the theme, but the screen does not change.

## Preferences file

The workbench keeps three choices between runs: the theme, after you switched it with Ctrl+F1 (or Alt+G), the sizes of the regions, after you moved a divider, and the recent files. They are in one JSON file of the user, outside every project:

| System | Path |
| --- | --- |
| Windows | `%APPDATA%\TruthWeaver\workbench.json` |
| Linux and macOS | `$XDG_CONFIG_HOME/TruthWeaver/workbench.json`, or `~/.config/TruthWeaver/workbench.json` when `XDG_CONFIG_HOME` is not set |

```json
{
  "theme": "dark",
  "layout": { "editorWidthPercent": 60, "topHeightPercent": 70 },
  "recentFiles": ["/home/ana/rules/publish.rule", "/home/ana/rules/policy.yaml"]
}
```

- **Content.** The file holds `theme` (`light` or `dark`, never `auto`) and `layout` (the width of the editor region and the height of the top regions, in percent). `recentFiles` lists up to ten full paths, newest first. The file holds no rule text, and no path except the recent files. The maximised region is not kept.
- **Theme.** A `theme` in the file wins over the `theme` setting of `truthweaver.json`. Without a switch, the file has no `theme`, and the setting applies. To use the setting again, delete the `theme` line or the file.
- **Recent files.** The file is written each time you open a file or remove a missing one from the list. A path that is repeated or is not text is dropped when the file is read, and so is every path after the tenth.
- **Layout.** The file is written when you move a divider. Use the Alt+arrow keys or their second keys (Alt+Z, Alt+C, Ctrl+T, Alt+Y; see [Terminals that swallow keys](#terminals-that-swallow-keys)). A mouse drag writes the file when it ends. The next start uses the sizes.
- **Problems.** A missing file, a file that is not valid JSON, or a file that is not an object gives the defaults and no message. A value that is not valid, such as `auto` or a size outside 15 to 85, is ignored and the other values stay. A file that cannot be written is ignored; the choice then lasts for the session only.

## Variables

A term with a `from("source", "query")` argument takes its value from data. The Variables pane is an editor for that data. The text is one JSON or YAML object. Each key is a source name, and its value is the data of that source:

<!-- doctest:skip variables document, not a rule -->
```json
{ "user": { "region": "EU" } }
```

Every edit applies at once. Ctrl+F4 (or Alt+D) loads a JSON or YAML file into the editor. A text with an error keeps the last data that was well-formed, and the note under the editor shows the problem.

A reference with no data is a fault. The term is `Unknown`, the result line counts the fault, and the term list shows `(no data yet)`. When the data resolves the reference, the term is listed with its value as for any other term. Its key is the term with the value from the data, such as `inRegion(region: "EU")`, as for a [stub-value file](tool-core.md#stub-values).

## Rule tests

The Tests pane saves an assignment as a case of a rule test file, runs the file, and loads a failing case back for debugging. The file has the format of [Rule tests](rule-tests.md), so `truthweaver test` runs it.

The pane has a field for the path of the file, a field for the name of the case, and the list of the cases.

- **Save.** Ctrl+F5 (or Alt+S) saves the current assignment as the case with the name in the name field. The case holds the values of the terms of the open rule, the variables when the rule has a `from(...)` reference, and the result and the number of faults that the decision has now. A new file gets the open rule as its inline `rule`. An existing file keeps its own rule, its comments and its other cases. A case with the same name is replaced; the replacement loses the `trace` expectation and the `variableFiles` of the old case. The rule text must compile. The cases must be a block list.
- **Open.** Enter in the path field reads the cases of an existing file.
- **Run.** Ctrl+F7 (or Alt+N) runs the cases of the file on the open rule, not on the rule in the file. Each case shows as `PASS` or `FAIL`. A failed case shows the result and the number of faults that it expects, and the ones it got. The line under the list gives the first reason for the selected case. The rule text must compile.
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

Ctrl+F12 (or Alt+O) writes with the round-trip write-back: it reads the whole document and writes it again. A dialog warns first that YAML comments and the layout of the document may be lost. The file is written only after you agree.

The document file is never written without a confirmation. The file keeps its encoding, its byte order mark and its line endings. A rule whose text has errors is not written.

## Menus and keys

The menu bar on the first row holds every command of the workbench, in one menu for each type of operation. Each entry shows its key on the right. The key runs the command without the menu.

| To | Do |
| --- | --- |
| Open the menu bar | Press F1 or F10. The File menu opens. |
| Open one menu | Press Alt and the underlined letter: Alt+F (File), Alt+E (Edit), Alt+I (Insert), Alt+R (Rule), Alt+A (Analyze), Alt+X (Export), Alt+V (View) or Alt+H (Help). Or click the title of the menu. |
| Move in the menus | Up and Down move to an entry. Left and Right move to the next menu, and Right opens a submenu. |
| Run an entry | Press Enter, or click the entry. |
| Close the menus | Press Esc. The focus goes back to the pane that had it. |
| See every key | Press Shift+F1. The key reference lists every key by menu, and scrolls with the arrows and the wheel. Shift+F1 or Esc closes it. |

Two settings have a small, fixed set of values. Each is a submenu with one entry for each value, and `(*)` marks the current value. Choose a value to set it in one step.

| Submenu | Values |
| --- | --- |
| Rule > Unknown policy | Unknown counts as False, Unknown counts as True, Unknown is an error. Ctrl+F3 (or Alt+P) moves to the next one. |
| View > Theme | Light and dark. Ctrl+F1 (or Alt+G) switches between them. |

### Terminals that swallow keys

Some terminals take a key before it reaches the workbench, or send none of the bytes for it. Windows Terminal, by default, binds Alt+Left, Alt+Right, Alt+Up and Alt+Down to its own tab and pane actions. An SSH session depends on the client and the server agreeing on which keys to forward. Some set-ups drop Ctrl+PageUp, Ctrl+PageDown or the Ctrl+F-keys. tmux reads many key chords itself, for its own prefix and pane commands, before it passes the rest to the program inside. Some Ctrl and function-key combinations never reach that program unless the tmux configuration passes them through.

Every command on Alt+Left, Alt+Right, Alt+Up, Alt+Down, Ctrl+PageUp, Ctrl+PageDown, a Ctrl+F-key, Shift+F11 or Shift+F12 has a second key. The second key is built from Ctrl or Alt and a letter, and it still reaches the workbench in such a terminal. You cannot configure the two keys. Both run the command. Both show on the command's entry in the menu and in the key reference (Shift+F1), as `Ctrl+F1 or Alt+G`. The tables below give both keys for every command that has one.

### File

| Key | Does |
| --- | --- |
| Ctrl+O | Opens a rule file. |
| File > Open recent | Lists the last ten files that you opened. Choose one to open it. |
| Ctrl+S | Saves. For a rule inside a document, it patches the rule into the document. |
| F12 | Saves to a new path. The extension selects the format. |
| F2 | Opens a rule inside a JSON or YAML document. |
| Shift+F2 | Switches to another rule of the open document. |
| Ctrl+F12 or Alt+O | Saves the open rule into its document with the round-trip write-back. |
| F9 | Imports a rule from the clipboard or a file. |
| Shift+F9 | Opens the list of sample rules. File > Samples has the same samples. |
| Ctrl+Q | Quits. Esc, the quit key of Terminal.Gui, also quits when no menu is open. Both ask first when the rule has unsaved changes. |

### Edit

| Key | Does |
| --- | --- |
| Ctrl+Z | Undo. |
| Ctrl+Y | Redo. |
| F8 | Formats the rule: canonical. |
| Shift+F8 | Formats the rule: pretty. |

### Rule

| Key | Does |
| --- | --- |
| F4 | Opens the Convert dialog: canonical rule text, formatted rule text, JSON or YAML. |
| Shift+F4 | Converts the rule to the next format: rule text, JSON, YAML. |
| Shift+F5 | Opens the Generate rule dialogs: seed, depth and operator families (tick boxes). |
| Ctrl+F2 or Alt+U | Gives every term the value `Unknown`. |
| Ctrl+F3 or Alt+P | Moves the policy for `Unknown` to the next one. |
| Ctrl+F4 or Alt+D | Loads a variables file (JSON or YAML). |
| Ctrl+R | Chooses a rewrite and shows the result in the Rewrite pane. |
| (menu only) | Unknown policy: sets the policy for `Unknown`. |

### Analyze

| Key | Does |
| --- | --- |
| F7 | Validates every level. |
| Shift+F7 | Turns strict validation on or off. |
| Ctrl+F5 or Alt+S | Saves the assignment as a case of the rule test file. |
| Ctrl+F7 or Alt+N | Runs the rule test file on the open rule. |
| Shift+F12 or Alt+J, Shift+F11 or Alt+K | Goes to the next or previous step of the evaluation in the Tree pane. Esc leaves the step mode. |
| Ctrl+F8 or Alt+M | Describes the part of the rule at the cursor or the selected node, in a box. Esc closes it. The mouse shows the same box on hover. |
| Ctrl+G | Compares the rule with the last saved or accepted version. |
| Ctrl+F9 or Alt+B | Loads a second rule from a file or the clipboard and shows how it relates to the open rule in the Compare pane. |

### Export

| Key | Does |
| --- | --- |
| F5 | Opens the export dialog. |

### View

| Key | Does |
| --- | --- |
| F6, Shift+F6 | Moves the focus to the next or previous region. |
| Ctrl+PageDown or Alt+W, Ctrl+PageUp or Alt+Q | Shows the next or previous pane of the region. |
| F11 | Maximises the region with the focus, or shows every region again. |
| Alt+Left or Alt+Z, Alt+Right or Alt+C | Moves the vertical divider: makes the Rule region narrower or wider, in steps of 5 percent. |
| Alt+Up or Ctrl+T, Alt+Down or Alt+Y | Moves the horizontal divider: makes the top regions lower or higher, in steps of 5 percent. |
| Ctrl+F1 or Alt+G | Switches between the light and the dark theme for this session. |
| (menu only) | Theme: sets the light or the dark theme. |

### Help

| Key | Does |
| --- | --- |
| F1 | Opens the menu bar. F10 also opens it. |
| Shift+F1 | Shows or hides the key reference. |

### Pane keys

Some panes have their own keys. They work while the pane has the focus, and they are not in the menus.

| Key | Does |
| --- | --- |
| Space, T, F, U | In the term list of the Assign pane: moves the selected term to the next value, or sets it to `True`, `False` or `Unknown`. |
| `t`, `r`, Enter | In the Make True pane: change the target, search again, or apply the entry. See [What would make this True](#what-would-make-this-true). |
| `o`, `w`, `n`, `i`, Delete, `[`, `]`, `p`, `l`, `y`, `Y`, `v` | In the Tree pane: edit the selected node, copy or paste. See [Structural editing](#structural-editing). |
| `o`, Enter, `x` | In the Compare pane: load another rule, load the example on the line into the Assign pane, or close the second rule. See [Compare two rules](#compare-two-rules). |
| `r`, `a`, `x`, `c` | In the Rewrite pane: choose a rewrite, accept it, discard it, or compare. See [Rewrites and compare](#rewrites-and-compare). |

The editor has the usual editing keys of the Terminal.Gui editor view, such as Ctrl+C, Ctrl+X and Ctrl+V for the clipboard and Ctrl+A to select all.

The mouse also works, and the key reference says so. A click on a menu title opens the menu and a click on an entry runs it. A click on a pane gives the focus to its region, a click on a line of the Tree pane selects that node, and a click in the editor moves the cursor. The wheel scrolls the pane under the pointer. Resting the pointer on a part of the rule shows its description. A right click opens a context menu. See [Context menus](#context-menus). The mouse is never the only way: every action has a key above. A terminal that does not report the mouse works with the keys.

A drag on a divider resizes the regions:

- **Dividers.** The vertical divider is between the Rule region and the right region. The horizontal divider is above the bottom region and runs across the whole window. Each divider is two border lines side by side, and a press on either line grabs it.
- **Drag.** The divider follows the pointer while you hold the left button, in steps of 1 percent and between 15 and 85 percent of the window. When you release the button, the sizes are saved as for the Alt+arrow keys. See [Preferences file](#preferences-file).
- **No pointer moves.** Some terminals report only the press and the release. In such a terminal, press on a divider and release where you want it: the divider moves to the release position.
- **Maximised region.** While a region is maximised (F11), there are no dividers to drag.

### Context menus

A right click in the editor, the Tree pane, the term list of the Assign pane or the Diagnostics pane opens a menu of the common actions of that pane. Each entry shows its shortcut, and the entry does what the shortcut does. In the Tree pane and the term list, the click first selects the line under the pointer, so the menu acts on that line. Esc closes the menu.

| Pane | Entries (shortcut) |
| --- | --- |
| Editor | Undo (Ctrl+Z), Redo (Ctrl+Y), Cut (Ctrl+X), Copy (Ctrl+C), Paste (Ctrl+V), Select all (Ctrl+A), Format: canonical (F8), Format: pretty (Shift+F8), Save (Ctrl+S) |
| Tree | Replace operator (`o`), Wrap in NOT (`w`), Negate (`n`), Insert operand (`i`), Delete node (Delete), Copy node (`y`), Copy rule (`Y`), Paste (`v`) |
| Term list | Set True (`T`), Set False (`F`), Set Unknown (`U`), Next value (Space), All terms Unknown (Ctrl+F2 or Alt+U), Load variables (Ctrl+F4 or Alt+D) |
| Diagnostics | Go to location (Enter), Validate (F7), Format: canonical (F8) |

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
- A command is a `WorkbenchCommand`: a key (or `Key.Empty` for a command that only the menu runs), a `CommandGroup` (the menu that holds it), a short title for the menu, a description for the key reference and an action on the window. Set `Hint` to show it on the hint line. Set `SecondKey`, built from Ctrl or Alt and a letter, when the primary key is one that a terminal is known to swallow (see [Terminals that swallow keys](#terminals-that-swallow-keys)); either key then runs the command, and both show in the menu and the key reference. Add the command to `WorkbenchCommands.Default`, or to the list of its feature in the `Commands` folder. `CommandKeyClashTests` fails when a key, primary or second, is also bound by the editor, a list or a text field, or is not distinct from every other key. A test fails when a command names no defined group or is not in exactly one menu.
- A setting with a small, fixed set of values is a `WorkbenchChoice`: a group, a title, the values, and the functions that read and set the current one. Add it to `WorkbenchCommands.Choices`. Its menu shows it as a submenu after the commands, with `(*)` on the current value.
- `WorkbenchMenus` builds the menu bar: one menu for each `CommandGroup` value with at least one entry, in the order of the enum (File, Edit, Insert, Rule, Analyze, Export, View, Help). A group with no entry has no menu until an entry names it. The Insert menu also builds its Operators and Constants submenus from `InsertItems`.

The state that the panes share has no view: `AssignmentSession` in the tool core holds the values, the variables and the policy (see [Trying a rule with values](tool-core.md#trying-a-rule-with-values)), `Workbench.Outcome` is its result for the open document, and `Workbench.NotifyAssignmentChanged()` tells the panes after a change. `RuleTestSession` holds the rule test file of the workbench, and `RuleTestCaseText` in the tool core edits the text of that file. `RuleDocument` in the tool core holds the text, the compiled rule, the diagnostics and the history (see [Editing a rule](tool-core.md#editing-a-rule)), and `Workbench` holds the document, the layout and the status line. `Workbench.SelectedNode` is the path of the selected node of the tree, and `Workbench.SelectedNodeChanged` tells the panes after `Workbench.SelectNode(path)`. `Workbench.ReplaceRule(rule, message)` puts an edited rule into the document as one undo step; `RuleTreeEditor` and `RulePalette` in the tool core make the edits (see [Editing the tree](tool-core.md#editing-the-tree)). `RewritePlayground` in the tool core applies a rewrite without a change to the rule and compares two versions (see [Rewrites](tool-core.md#rewrites)). `WorkbenchWindow.ShowPane(title)` shows a pane and gives it the focus. The tests in `tests/TruthWeaver.Tool.Tui.Tests` drive the window with keys without a terminal, and run the application loop once with the ANSI driver of Terminal.Gui.
