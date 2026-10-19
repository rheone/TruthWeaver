# TruthWeaver.Tool.Tui

`truthweaver-tui`: a terminal workbench to edit a rule in rule text, JSON or YAML with live diagnostics.

## Purpose

The workbench compiles the rule again on each change. It shows the diagnostics and the canonical text of the rule next to
the editor, and it keeps one undo and redo history. You can format the rule, give each term a value and read the decision,
the trace and the reason for every node. Like the command line, it compiles every rule permissively and needs no host
application. It is a thin front end over [TruthWeaver.Tool.Core](../TruthWeaver.Tool.Core/README.md). The full reference
is in [Terminal UI](../../docs/tui.md).

## Use

The package is not on NuGet. Pack it from the repository and install it from the folder. The command is `truthweaver-tui`.

<!-- doctest:skip shell command, not a rule -->
```shell
dotnet pack src/TruthWeaver.Tool.Tui --configuration Release --output ./packages
dotnet tool install --global TruthWeaver.Tool.Tui --add-source ./packages --prerelease
```

The tool runs on .NET 11 in a terminal that supports ANSI escape sequences, such as Windows Terminal.

Open a rule file. The extension, then the content, selects the format. Use `--format text|json|yaml` to set it. With no
file, the workbench starts with an empty rule. A `-` reads the rule from standard input.

<!-- doctest:skip shell command, not a rule -->
```shell
truthweaver-tui rule.txt
```

The window has the rule editor on the left, and the canonical text of the rule on the right. The bottom shows the
diagnostics, with their severity, code, line and column. A region can hold more than one pane, and Ctrl+PageDown shows the
next one. The status line shows the file name, the format, a `*` for unsaved changes, the number of errors and warnings,
and the last message.

When the text has an error, the workbench keeps the last tree that compiled and shows it in the canonical pane.

### Keys

The menu bar holds every command, in the menus File, Edit, Rule, Analyze, Export, View and Help, and each entry shows its
key. F1 or F10 opens the menu bar, and Alt with the underlined letter opens one menu. The hint line at the bottom shows the
most common keys, and Shift+F1 shows the key reference with every key by menu.

| Key | Does |
| --- | --- |
| F1, F10 | Opens the menu bar. |
| Shift+F1 | Shows or hides the key reference. |
| Ctrl+O | Opens a rule file. |
| Ctrl+S | Saves. For a rule inside a JSON or YAML document, it patches the rule into the document. |
| F12 | Saves to a new path. The extension selects the format. |
| Ctrl+Z, Ctrl+Y | Undo and redo. |
| F8, Shift+F8 | Formats the rule: canonical or pretty. |
| F4 | Opens the Convert dialog: canonical rule text, formatted rule text, JSON or YAML. |
| Shift+F4 | Converts the rule to the next format: rule text, JSON, YAML. |
| F5 | Opens the export dialog. |
| F7, Shift+F7 | Validates every level, and turns strict validation on or off. |
| F9 | Imports a rule from the clipboard or a file. |
| F2, Shift+F2 | Opens a rule inside a JSON or YAML document, and switches to another rule of that document. |
| Ctrl+F12 (or Alt+O) | Saves the open rule into its document with the round-trip write-back. |
| F6, Shift+F6 | Moves the focus to the next or previous region. |
| Ctrl+PageDown (or Ctrl+J), Ctrl+PageUp (or Ctrl+L) | Shows the next or previous pane of the region. |
| F11 | Maximizes the region with the focus, or shows every region again. |
| Alt+Left (or Ctrl+I), Alt+Right (or Ctrl+W), Alt+Up (or Ctrl+T), Alt+Down (or Alt+Y) | Moves the lines between the regions. |
| Ctrl+R, Ctrl+G | Chooses a rewrite, and compares the rule with the last saved or accepted version. |
| Ctrl+F9 (or Alt+B) | Loads a second rule from a file or the clipboard to compare with the open rule. |
| Ctrl+F2 (or Alt+U), Ctrl+F3 (or Alt+P), Ctrl+F4 (or Alt+D) | In the Assign pane: sets every term to `Unknown`, moves the policy for `Unknown`, loads a variables file. |
| Ctrl+F5 (or Alt+S), Ctrl+F7 (or Alt+N) | Saves the assignment as a rule test case, and runs the rule test file. |
| Ctrl+Q | Quits. It asks first when the rule has unsaved changes. |

Some panes have their own keys, such as Space, T, F and U in the term list of the Assign pane. [Terminal UI](../../docs/tui.md)
lists them, including the second key of a command whose primary key some terminals swallow.

## Folders

Each folder has the namespace `TruthWeaver.Tool.Tui.<Folder>`. A build warning (IDE0130) shows a namespace that does not
match its folder. `Program` and `Results` stay at the root.

| Folder | Holds |
| --- | --- |
| `Commands` | The key commands of the workbench, one list for each feature. |
| `Controllers` | The logic behind a pane: compare, counterfactual, embedded rule, export, import, rewrite, structure, validation and view. |
| `Hosting` | The start of the application, and the dialogs, prompts and clipboard of the terminal. |
| `Panes` | The panes of the regions, the pane interface and the layout of the regions. |
| `Predicates` | The ready-made predicates that the palette offers as templates. |
| `Sessions` | The state and the text lines that the panes share: values, rule tests, imports, exports and coverage. |
| `Theme` | The colors, the theme choice and the editor marks. |
| `Views` | The Terminal.Gui views of the panes and the rule renderer. |
| `Workbench` | The workbench state, its window, the lists of panes and commands, and the preferences. |

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue` and `Decision`.
- [TruthWeaver](../TruthWeaver/README.md) to compile, print, rewrite and evaluate the rules.
- [TruthWeaver.Yaml](../TruthWeaver.Yaml/README.md) to read and write YAML rules.
- [TruthWeaver.Tool.Core](../TruthWeaver.Tool.Core/README.md) for the logic that the workbench shares with the command line.
- [TruthWeaver.Testing](../TruthWeaver.Testing/README.md) for the stand-in predicates that give the stub values.
- [TruthWeaver.Predicates](../TruthWeaver.Predicates/README.md) for the ready-made predicates that the palette offers as templates.

## Key types

| Type | Role |
| --- | --- |
| `TuiStartup` | Reads the arguments and the configuration, and starts the application. |
| `Workbench` | Holds the open document, the layout, the assignment and the status line. |
| `WorkbenchWindow` | The Terminal.Gui window: the regions, the panes and the keys. |
| `IWorkbenchPane` | One pane of a region. Add an instance to `WorkbenchPanes.Default`. |
| `WorkbenchCommand` | One command: its key, its menu group, its title, its description and its action. Add it to `WorkbenchCommands.Default`. |
| `WorkbenchMenus` | Builds the menu bar from the commands and the choices, one menu for each group. |

## Notes

The workbench uses [Terminal.Gui](https://github.com/tui-cs/Terminal.Gui) and its `Terminal.Gui.Editor` package for the text
editors. No other project of the repository references them, and an architecture test checks this.
