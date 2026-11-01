# Rule text

How to write a rule as text (the DSL), how to print it, and how to tidy it. The grammar is here. The meaning of each operator is in the [Strong Kleene (K3) reference](strong-k3/README.md). Back to the [README](../README.md).

## Where each rule is stated

This page does not repeat the K3 reference. These topics are in the specification:

| Topic | Page |
| --- | --- |
| Words and symbols for each operator | [Spellings](strong-k3/specification/syntax.md#spellings) |
| The three forms: prefix, infix and call | [Forms](strong-k3/specification/syntax.md#forms) |
| Operand counts and the errors for a wrong count | [Operand counts](strong-k3/specification/syntax.md#operand-counts) |
| Precedence and grouping | [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping) |
| Which operators need parentheses next to each other | [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule) |
| `Project`, `Collapse` and `NXOR` | [Operations outside the rule language](strong-k3/specification/syntax.md#operations-outside-the-rule-language) |
| Every operator, its category and its operand counts | [Operations](strong-k3/specification/operations.md) |
| Diagnostic codes | [Diagnostics](strong-k3/specification/diagnostics.md) |

## Grammar

The whole DSL in EBNF. `{ x }` is zero or more, `[ x ]` is optional and `|` is a choice. A term name must be a valid identifier - a letter or `_` first, then letters, digits or `_` only - and it cannot be a reserved word. The reserved words are the operator and constant keywords: `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IFF`, `XNOR`, `IMPLIES`, `NAND`, `NOR`, `TRUE`, `FALSE`, `UNKNOWN`, `T`, `F`, `U`, `PARITY`, `NXOR`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `IF`, `ISTRUE`, `ISFALSE`, `ISUNKNOWN`, `ISKNOWN`, `PROJECT`, `COLLAPSE`, `EXACTLYONE`, `ATLEAST`, `ATMOST`, `GREATERTHAN`, `LESSTHAN`, `EXACTLY`, `LET`, `FORALL` and `EXISTS`, in any case. `PredicateRegistryBuilder<TContext>` throws an `ArgumentException` that names the word when you register a predicate or collection with a name that breaks either rule, and the source generator reports `TWG008` for a name that is not a valid identifier and `TWG007` for one that is reserved. A `let` definition's own name follows the same rule: see [Definitions and parameters](#definitions-and-parameters). `IN` is a contextual keyword: it is reserved only at the head of a `FORALL`/`EXISTS` quantifier, so a predicate named `In` elsewhere is unaffected; see [Quantifiers](#quantifiers).

<!-- doctest:skip grammar notation, not a rule -->
```ebnf
rule        = expression ;

expression  = or_expr [ "?" or_expr ":" or_expr ] ;          (* ternary = If *)
or_expr     = and_expr { ( "OR" | "||" | "∨" ) and_expr } ;
and_expr    = infix_expr { ( "AND" | "&&" | "∧" ) infix_expr } ;
infix_expr  = not_expr [ infix_op not_expr ]                 (* at most one *)
            | not_expr { "??" not_expr } ;                   (* COALESCE chain *)
infix_op    = "XOR" | "⊕" | "⊻" | "EQUIVALENT" | "IFF" | "XNOR" | "↔" | "⇔"
            | "IMPLIES" | "→" | "⇒" | "NAND" | "↑" | "⊼" | "NOR" | "↓" | "⊽" ;
not_expr    = ( "NOT" | "!" | "¬" ) not_expr | primary ;

primary     = "(" expression ")" | "[" expression "]" | "{" expression "}"
            | constant | call | quantifier | term ;
constant    = "True" | "False" | "Unknown" | "T" | "F" | "U" ;
call        = list_op "(" expression { "," expression } ")"
            | threshold "(" integer "," expression { "," expression } ")"
            | "BETWEEN" "(" integer "," integer "," expression { "," expression } ")"
            | "If" "(" expression "," expression "," expression ")"
            | inspection "(" expression ")" ;
list_op     = "PARITY" | "ANY" | "ALL" | "NONE" | "COALESCE" | "ExactlyOne" ;
threshold   = "AtLeast" | "AtMost" | "GreaterThan" | "LessThan" | "Exactly" ;
inspection  = "IsTrue" | "IsFalse" | "IsUnknown" | "IsKnown" ;
quantifier  = ( "FORALL" | "EXISTS" ) "(" identifier "IN" identifier "," expression ")" ;

term        = identifier [ "(" [ argument { "," argument } ] ")" ] ;
argument    = identifier ":" ( literal | variable ) ;
variable    = "from" "(" string "," string ")" ;              (* a whole argument value only, never inside an array *)
literal     = string | number | "true" | "false" | "[" [ literal { "," literal } ] "]" ;
```

`rule` is written here as a single `expression` for brevity; the full grammar a document actually parses is zero or
more `let` declarations followed by one `expression`, the [definitions and parameters](#definitions-and-parameters)
section below.

Some rules are context rules, not syntax, so the grammar does not show them:

- **No implicit mixing.** An `infix_op` expression, a `??` chain or a ternary cannot sit next to `AND`, `OR`, another infix operator or a nested ternary at the same level without parentheses. The compiler reports `AmbiguousOperatorMixing`. See [The mixing rule](strong-k3/specification/syntax.md#the-mixing-rule).
- **Checks after parsing.** The compiler checks operand counts, threshold bounds and argument schemas after it parses the rule. A failure is a diagnostic, not a syntax error.
- **Comments.** A `//` comment runs to the end of its line, and a `/* */` comment can span lines. A comment can go anywhere whitespace can, and the parser keeps it as the note of a node. See [Notes](rule-formats.md#notes).
- **Rejected spellings.** `NXOR`, `Project` and `Collapse` are not in the language. The compiler rejects them in rule text, JSON and YAML, and the diagnostic names the replacement. See [Operations outside the rule language](strong-k3/specification/syntax.md#operations-outside-the-rule-language).

## Definitions and parameters

A rule text document may name a reusable sub-expression once and use it, optionally with arguments, as many times
as the rule needs. Declare it with `let`, before the rule's own value:

<!-- doctest:skip grammar notation, not a rule -->
```ebnf
document    = { definition } rule ;
definition  = "let" identifier [ "(" identifier { "," identifier } ")" ] "=" expression ";" ;
```

`let name = expression;` declares a definition with no parameters; a use of it is the bare name, wherever a term or
a constant could appear. `let name(p1, p2, ...) = expression;` declares one with parameters; a use applies the name
to its arguments like a call, `name(arg1, arg2, ...)`, each argument itself a term, a constant, or any other
sub-expression (the same things an `AND`/`OR` operand can be). Inside the definition's own body, each parameter is
used the same way: a bare name wherever a term, constant or sub-expression could appear. A parameter cannot be
a reserved word, such as `T`, `True`, `not`, `and` or `any` (the comparison ignores case): the compiler rejects it with
`DefinitionNameCollision`.

<!-- doctest:rule definitions -->
```text
let allApprovals(reviewer1, reviewer2) = reviewer1 AND reviewer2;
allApprovals(approvedByAlice, approvedByBob) AND NOT approvedByCarol
```

Expansion happens at compile time: every use is replaced by the definition's body, with its parameters substituted,
before the rule reaches the evaluator, the analyzer, a rewrite or a printer. None of those needs a definitions-aware
case, because the tree they see never contains one: it holds only the operators this page already describes.
`PrintRuleText`, `PrintJson`, `PrintYaml` and `CanonicalText` all print the already-expanded rule, exactly as if
every use had been written out by hand:

<!-- doctest:ruletext definitions -->
```text
(approvedByAlice AND approvedByBob) AND NOT approvedByCarol
```

A definition is document-scoped sugar, not a new kind of node, so it follows a few rules of its own:

- **No loops, no recursion.** A definition cannot reference itself, directly or through a chain of other
  definitions, at any depth. The compiler detects a cycle before it would recurse unboundedly.
- **Unique, non-reserved names.** A definition's name must be unique (case-insensitively) within its document. It
  cannot reuse `let` or any other reserved word, `T`/`F`/`U`, or the name of a predicate registered with the
  compiler.
- **Unique parameter names.** A definition's own parameter list cannot name the same parameter twice
  (case-insensitively), such as `let both(x, X) = x AND X;`. Binding a use's arguments by name would otherwise let
  the later parameter silently overwrite the earlier one, dropping an argument with no diagnostic.
- **Order does not matter.** A use may name a definition declared earlier or later in the same document.
- **Expansion counts toward the normal limits.** Once a use is expanded, its nodes are ordinary tree nodes: they
  count toward `CompilerOptions.MaxNodeCount` and `MaxDepth` exactly as if written out by hand.
- **A bounded chain of definitions.** `CompilerOptions.MaxDefinitionDepth` (default 8) bounds how many definitions
  deep a use may expand through (a use inside definition A naming B, whose body names C, and so on), independent
  of `MaxDepth` and the cycle check.
- **An unused definition is an opt-in lint**, `LintRules.UnusedDefinition`, off by default.

Each rule is a distinct compile diagnostic, listed with the others in [Diagnostics](strong-k3/specification/diagnostics.md):
a self- or mutual reference (`DefinitionCycleDetected`), a name collision, including a parameter that is a reserved word (`DefinitionNameCollision`), a repeated
parameter name (`DuplicateDefinitionParameter`), a chain deeper than `MaxDefinitionDepth`
(`MaxDefinitionDepthExceeded`), a use with the wrong number of arguments (`DefinitionArgumentCountMismatch`), a use
of a name that was never declared (`UnknownDefinition`), and the unused-definition lint (`UnusedDefinition`). A
diagnostic's span always points at the use; one actually inside a definition's own body additionally names the
definition. `DuplicateDefinitionParameter` is the exception: nothing about the definition's own declaration is a
use, so its span always points at the declaration itself.

### Read the definitions of a document

A compiled rule has no definitions, so the printers above cannot print the `let` statements back. To get the
definitions and uses of a document, read its source with `RuleDefinitions` (namespace `TruthWeaver.Compilation`).
Reading only parses the text: it does not need a predicate registry and it does not expand the uses.

| Member | What it gives |
| --- | --- |
| `RuleDefinitions.ReadText(text, options?)`, `RuleDefinitions.ReadJson(json)` | The definitions and uses of rule text or JSON. `YamlRuleDefinitions.ReadYaml(yaml)` reads YAML, and `YamlRuleDefinitions.ReadAs(source, format)` reads any of the three formats (package `TruthWeaver.Yaml`). Reading is bounded the same way a compile is: an optional `CompilerOptions` (`CompilerOptions.Default` when omitted) caps the nodes the source-mapping walk below produces, so a document designed to make `let` expansion blow up reports `MaxNodeCountExceeded` instead of hanging or exhausting memory. Pass the same `CompilerOptions` the document was compiled with, including a non-default `MaxDefinitionDepth`, so `SourcesOf` reaches the same depth a real compile expanded to, instead of disagreeing with it. |
| `Succeeded`, `Diagnostics` | `Succeeded` is `false` when the text has a syntax error. Reading does not throw for an authoring error: the diagnostics tell what is wrong. |
| `Definitions` | Each `DefinitionDeclaration`: the name, the parameters, the path (`$.definitions.name`), and the spans of the declaration and its name. |
| `Uses` | Each `DefinitionUse`: the name, the number of arguments, the definition whose body holds it (`null` in the rule), its path, its spans, and `ExpandedPaths`, the paths of its expansions in the compiled rule. |
| `SourcesOf(path)` | The source paths of a node of the compiled rule, outermost first: the use, then the body node it expands to. A node in the place of a parameter gives the argument of the use. |
| `PrintJson()`, `PrintYaml()` | The document in the tree format without expansion: a `definitions` section and a `use` node for each use. It compiles to the same rule as the source. `PrintYaml` is in `TruthWeaver.Yaml`. |

Reading reports syntax errors only. Compile the text to find an unknown definition, a cycle, a name collision or a
wrong number of arguments. All paths use the form of the [tree format](rule-formats.md#definitions-and-parameters),
the same form as `CompiledRule.Notes`.

```csharp
RuleDefinitions read = RuleDefinitions.ReadText(
    "let both(x, y) = x AND y;\nboth(isOwner, isEditor) OR isAdmin");

DefinitionDeclaration both = read.Definitions[0]; // both(x, y), at $.definitions.both
DefinitionUse use = read.Uses[0];                // both, 2 arguments, expands at $.operands[0]
EquatableArray<string> sources = read.SourcesOf("$.operands[0].operands[1]"); // the argument isEditor
```

The [terminal UI](tui.md#definitions-in-the-tree-pane) uses this type to show the definitions in its Tree pane and
to keep them when you edit the tree.

## Quantifiers

`FORALL(variable IN collection, body)` and `EXISTS(variable IN collection, body)` range over a **collection
binding** the host registered on the predicate registry. They are a function-call form with no
precedence, like `PARITY`, `ANY` and `If`: no parentheses are needed beside an infix operator, and `body` is a full
expression. `FORALL` and `EXISTS` are reserved words; `IN` is reserved only here, at a quantifier's head, so a
predicate named `In` elsewhere is unaffected. `variable` is a plain identifier that labels the trace (`line[2]`) and
is part of tree equality and canonical text; `collection` names a collection the compiler resolves against its
registry, a namespace of its own, separate from predicate names. A head with `IN` left out (`FORALL(line lines, ...)`) or with another
token in its place (`FORALL(line INN lines, ...)`) is one `SyntaxError` (`TRE0001`) that names the token found; the
parser still reads `lines` as the collection, so the rest of the rule gives no further error.

<!-- doctest:rule forall -->
```text
FORALL(line IN lines, inStock)
```

The meaning of `FORALL`/`EXISTS` (the generalized `AND`/`OR` over a collection's elements, the empty- and
`null`-collection cases, the runtime and compile-time limits, and the trace shape) is in
[CONTEXT.md's Quantifiers section](../CONTEXT.md#quantifiers), not repeated here. Nesting one quantifier inside
another is allowed, up to `CompilerOptions.MaxQuantifierDepth`:

<!-- doctest:rule nested-quantifier -->
```text
EXISTS(line IN lines, FORALL(detail IN details, inStock))
```

A body term may read the element itself in two ways, and may mix both in the same body: a typed element
predicate such as `inStock` above, registered on the collection's own element registry, and an untyped
`from("line", "...")` read, as if the element were a data source of its own. The second way needs the host to
opt the collection in: `AddCollection`'s optional `elementSource` parameter projects one element to an
`IDataSource`. A collection registered without it supports only the typed path, and a body that still writes
`from("line", ...)` against it gets the same undeclared-data-source diagnostic [data sources](data-sources.md)
already gives any other name nothing declared.

<!-- doctest:rule forall-element-source -->
```text
FORALL(line IN lines, qtyAbove(n: from("line", "$.qty")))
```

A `let` definition may be used inside a quantifier's body. The use expands in the body, so the definition's terms
resolve in that collection's element registry, not in the outer registry. A use outside the body is checked against the
outer registry, so the same definition can compile in one place and give an unknown-predicate diagnostic in the other.
The quantifier variable is a label only and no term can read it, so a definition cannot refer to it.

<!-- doctest:rule forall-definition -->
```text
let inStockLine = inStock;
FORALL(line IN lines, inStockLine)
```

## Case

Operator words and the constants `True`, `False` and `Unknown` are case-insensitive on input, in every format. Rule text also reads the letters `T`, `F` and `U` (any case) as the constants, so `T AND f` and `True AND False` compile to the same tree. JSON and YAML read the words and the letters too, so `{"const": "U"}` and `const: u` are `Unknown`. `and`, `And` and `AND` compile to the same node.

The printer writes each operator word in one fixed spelling. The rule is:

- Connectives and the call words `PARITY`, `ANY`, `ALL`, `NONE`, `COALESCE` and `BETWEEN` are upper case: `NOT`, `AND`, `OR`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`, `COALESCE` and `BETWEEN`.
- The other call words are upper camel case: `ExactlyOne`, `AtLeast`, `AtMost`, `GreaterThan`, `LessThan`, `Exactly`, `If`, `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`.
- The constants are `True`, `False` and `Unknown`. `ConstantStyle.Letters` writes them as `T`, `F` and `U`.

`PrintRuleText` and `CanonicalText` write the word form of an operator by default. The printed spelling is the one that the [grammar](#grammar) uses for each word. See [Operator style](#operator-style) for the one other spelling `PrintRuleText` supports.

<!-- doctest:rule casing -->
```text
parity(isBanned, lovesPineapple, isDineIn) and isknown(isTakeout)
```

The compiled rule prints as `PARITY(isBanned, lovesPineapple, isDineIn) AND IsKnown(isTakeout)`.

Predicate names and argument names are case-insensitive too. `hasCrust(Crust: "thin")` and `hasCrust(crust: "thin")` compile to the same term, and the printer writes the name as the predicate's schema spells it. Argument values are case-sensitive.

The [Spellings](strong-k3/specification/syntax.md#spellings) table lists the accepted words and symbols.

## Grouping delimiters

`()`, `[]` and `{}` group a sub-expression and mean the same. The tree does not record which delimiter you wrote. `CanonicalText` always uses parentheses. For the rule, see [Precedence and grouping](strong-k3/specification/syntax.md#precedence-and-grouping).

These details apply to the parser:

- Each closer must match its opener. `(a AND b]` is an error.
- A function call keeps `(` as its own argument-list syntax. Only a grouped sub-expression can use `[` or `{`.
- Brackets and braces inside a quoted string are ordinary text.

A delimiter mistake is a `SyntaxError` diagnostic with the exact span:

| Mistake | Example | Message (span) |
| ------- | ------- | -------------- |
| Mismatched closer | `a AND (b OR c]` | `Expected ')' to close '(' at offset 6 but found ']'.` (the `]`) |
| Unclosed group | `a AND (b OR c` | `Unclosed '(' at offset 6: expected ')' before the end of the rule.` (the `(`) |
| Closer with no opener | `a AND b)` | `Unexpected closing ')' with no matching opener.` (the `)`) |

To print a rule with delimiters that change with the nesting depth, pass a `GroupingStyle` to `CompiledRule.PrintRuleText`. Without an argument it prints the parentheses style:

```csharp
CompiledRule<MyContext> rule = compiler.Compile("a AND (b OR (c AND (d OR (e AND (f OR g)))))").GetRuleOrThrow();

rule.CanonicalText;                                  // a AND (b OR (c AND (d OR (e AND (f OR g)))))  (parentheses only)
rule.PrintRuleText();                                    // same as CanonicalText (the rule has no notes)
rule.PrintRuleText(GroupingStyle.Parentheses);           // the same, written out
rule.PrintRuleText(GroupingStyle.DepthCycling);          // a AND (b OR [c AND {d OR (e AND [f OR g])}])
```

`DepthCycling` is opt-in and deterministic. The delimiter depends only on how many groups enclose it, and it cycles through `(`, `[` and `{`. It helps people read a deeply nested rule. The output always parses back to a tree equal to the original, so `CanonicalText` stays the form to persist. Function-call argument lists keep `(`.

## Operator style

Pass an `OperatorStyle` to `PrintRuleText` to choose how `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND` and `NOR` are spelled:

- `Word` (the default) writes the word form, exactly as `CanonicalText` does.
- `Symbolic` writes the mathematical symbol the DSL parser itself reads back for that operator (`∧ ∨ ¬ ⊕ ↔ → ↑ ↓`), so the result still compiles to a tree equal to the original.
- `CStyle` is rejected: the parser does not read `^` or `==` (see [Spellings](strong-k3/specification/syntax.md#spellings)), so there is no round-trippable C-style rule text. `PrintRuleText` throws `ArgumentException`; `TryPrintRuleText` returns `false` and sets its `out` text to `null` instead.

`COALESCE` and `If` keep their word/function-call spelling in every style: the DSL reads `??` only as an infix operator, with no call syntax for it, so a function-call COALESCE has no symbol to switch to.

<!-- doctest:rule opstyle -->
```text
approvedByAlice AND (approvedByBob OR NOT approvedByCarol)
```

<!-- doctest:ruletext opstyle -->
```text
approvedByAlice AND (approvedByBob OR NOT approvedByCarol)
```

<!-- doctest:ruletext opstyle symbolic -->
```text
approvedByAlice ∧ (approvedByBob ∨ ¬approvedByCarol)
```

```csharp
rule.PrintRuleText();                                         // approvedByAlice AND (approvedByBob OR NOT approvedByCarol)
rule.PrintRuleText(operatorStyle: OperatorStyle.Symbolic);     // approvedByAlice ∧ (approvedByBob ∨ ¬approvedByCarol)
rule.PrintRuleText(operatorStyle: OperatorStyle.CStyle);       // throws ArgumentException

bool ok = rule.TryPrintRuleText(GroupingStyle.Parentheses, OperatorStyle.CStyle, out string? text);  // false, text is null
```

## Constant style

`PrintRuleText` and `TryPrintRuleText` take an optional `ConstantFormat` to choose how `True`, `False` and `Unknown` are spelled. The parser reads a constant as the word or as the letter, in any letter case. So rule text accepts only a format that the parser reads back to the same constants:

- `ConstantStyle.Word` (the default) writes `True`, `False` and `Unknown`.
- `ConstantStyle.Letters` writes `T`, `F` and `U`.
- An override is accepted when it is a letter-case variant of the constant's own word or letter, for example `TrueText = "TRUE"` or `TrueText = "t"`.
- `LogicSymbols` is rejected. The parser does not read `⊤` or `⊥`.
- Any other override is rejected, for example `"1"` for `True`, or `"F"` for `True`.

The names `T`, `F` and `U` are reserved, in any case. A predicate cannot have one of these names, and a term with one of these names is a compile error that says the name is reserved.

`PrintRuleText` throws `ArgumentException` with a message that names the style or the override. `TryPrintRuleText` returns `false` and sets its `out` text to `null` instead.

```csharp
rule.PrintRuleText(constants: new ConstantFormat { TrueText = "TRUE" });                   // TRUE AND Unknown
rule.PrintRuleText(constants: new ConstantFormat { Style = ConstantStyle.Letters });       // T AND U
rule.PrintRuleText(constants: new ConstantFormat { Style = ConstantStyle.LogicSymbols });  // throws ArgumentException

bool ok = rule.TryPrintRuleText(
    GroupingStyle.Parentheses,
    OperatorStyle.Word,
    new ConstantFormat { Style = ConstantStyle.LogicSymbols },
    out string? text);                                                                     // false, text is null
```

To print `⊤` and `⊥`, use [`PrintEquation`](equations.md#constants) or a tree printer. They are read views, not rule text. See [tree printers](rulebuilder.md#rendering-a-rule-as-a-diagram).

## Whitespace

Whitespace between tokens never matters, so you can lay out rule text freely. `CanonicalText` prints one space around each infix operator and after each comma, with no leading or trailing whitespace, whatever spacing the rule had when you wrote it.

To tidy text as written, use `RuleText.NormalizeWhitespace`. It keeps your operators, letter case and delimiters. It does not compile the text and does not need a predicate registry.

```csharp
RuleText.NormalizeWhitespace("  a&&b ||\n  !c  ");          // "a && b || !c"
RuleText.NormalizeWhitespace("ANY( a ,b,	c )");            // "ANY(a, b, c)"
RuleText.NormalizeWhitespace("named( value :\"x  y\" )");   // "named(value: \"x  y\")" (string contents untouched)
```

The result is the same for any input spacing, and a second call changes nothing. The result compiles to a tree equal to the tree of the input. A comment stays as written, with one space on each side like a token. A `//` comment is followed by a line feed, because it runs to the end of its line. The prefix `!` and `¬` touch their operand. The argument list of a call or term touches its name. Nothing pads the inside of `()`, `[]` or `{}`. The function keeps characters that the DSL does not recognize in place, so the text of a rule that does not compile yet is never lost.

## Width-aware pretty printing

`CompiledRule.PrintRuleText(RuleTextOptions)` and its Try-first sibling `TryPrintRuleText(RuleTextOptions, out string?)` take one options object instead of the `grouping`/`operatorStyle`/`constants` parameters. It adds one more setting, `Width`:

- `Width = null` (the default) prints one line, the same text as the other `PrintRuleText` overloads.
- `Width = N` breaks a group across lines when its one-line form would be longer than `N` characters at its current indent. `IndentSize` (default 2) is the number of spaces added per nesting level.

The breaking rule, applied independently at every group, from the outermost in:

- If the group's one-line form fits in `Width`, it prints on one line, exactly as `Width = null` would print it.
- Otherwise, it breaks. An `AND`/`OR` chain or a binary connective (`XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`) breaks after each operator, one operand per line; a call form (`AtLeast(2, a, b, c)`, `BETWEEN(min, max, ...)`, `ANY(...)` and the rest) breaks one argument per line. Each broken operand or argument is indented one more `IndentSize` than its parent, and is itself checked against `Width` the same way, so only the groups that need to break do.
- A single token (a term name, a constant) is never split. A width smaller than the longest token still prints that token whole, on its own line; it does not loop or throw.

The result always compiles back to a tree equal to the original: breaking is only a presentation choice, the same way `GroupingStyle.DepthCycling` is.

<!-- doctest:rule width -->
```text
approvedByAlice AND (approvedByBob OR NOT approvedByCarol)
```

<!-- doctest:ruletext width width=20 -->
```text
approvedByAlice AND
(
  approvedByBob OR
  NOT approvedByCarol
)
```

```csharp
rule.PrintRuleText(RuleTextOptions.Default);                           // one line (Width is null)
rule.PrintRuleText(RuleTextOptions.Default with { Width = 20 });       // approvedByAlice AND\n(\n  approvedByBob OR\n  NOT approvedByCarol\n)
```

`PrintRuleText(writer, ...)` and `PrintRuleText(writer, options)` write the same text to a `TextWriter`. See [Write a printer to a TextWriter](examples.md#write-a-printer-to-a-textwriter).

## Editor support

[`editors/vscode`](../editors/vscode/README.md) has a TextMate grammar and a VS Code language configuration for rule text. The grammar colors operators in word, symbolic and C-style forms, call forms, constants, terms and their arguments, `from(...)` variable references, strings, numbers, comments and brackets. It colors text and does not check it: the compiler is still the judge of a rule. The files are not published, and the folder README explains how to load them in VS Code. A test in `dotnet test` runs the grammar over every rule text block of this page and of [Examples](examples.md), so a documented rule always gets colors.

## Printer spelling of the ternary

`If(c, t, f)` and `c ? t : f` compile to the same node. The `CStyle` operator style of the tree printers labels the node `?:`. The other styles label it `If`. `PrintRuleText` never renders the ternary form (see [Operator style](#operator-style)). [`PrintEquation`](equations.md) with `EquationDialect.CStyle` renders `If` as the ternary `c ? t : f` instead of a call; the other dialects keep the call form.
