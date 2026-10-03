# ADR-0005: Strong K3 language surface

## Status

Accepted. Supersedes the operator-set, alias, `IMPLIES`, `XOR`/`XNOR`
and "word operators only" decisions in
[ADR-0003](0003-rule-syntax-and-serialization.md). Source requirements: `.scratch/2026-10-02-TODO.md`; planning
spec: `.scratch/k3-conformance/spec.md`.

## Context

[ADR-0001](0001-kleene-failure-model.md) made Strong Kleene (K3) the engine's
internal logic. ADR-0003 then deliberately kept the *authoring surface* small:
no `IMPLIES`, no symbol aliases, binary-only `XOR`/`XNOR`, and `ExactlyOne` as
the n-ary "exactly one" operator. The library is now being positioned as a
general-purpose Strong K3 expression engine, and the reference specifications
(`.tmp/`) define a complete K3 language: a primitive kernel, derived operators,
cardinality aliases, K3 value operations (`COALESCE`, `If`), inspection and
boundary functions, and alternate notations. The reasons ADR-0003 declined
those conveniences (two spellings, rule authors getting truth tables wrong)
are outweighed by the goal of conforming to a published K3 operator set, and
the aliases are cheap once the canonical form stays single.

## Decision

1. **Every expression, including every predicate result, is a `TruthValue`**
   (`True`, `False`, `Unknown`). `Unknown` is never implicitly converted to
   `True` or `False`; conversion to a two-valued result happens only at an
   explicit boundary (`COALESCE` with a constant inside a rule, or `Decision.Project` /
   `Decision.Collapse` on the result; decisions 12 and 14).
   `False < Unknown < True` is an implementation aid for truth functions and
   cardinality bounds, not a numeric ordering of truth.
2. **Operators are accepted in several notations but have one canonical form.**
   Named operators are case-insensitive on input; canonical output is upper
   camel/upper case (`AND`, `OR`, `AtLeast`). Symbol notation (`&&`, `||`,
   `!`, and the logic symbols `∧ ∨ ¬ ⊕ → ↔`, plus the input-only aliases
   `⊼ ⊽ ⊻ ⇒ ⇔` for NAND, NOR, XOR, IMPLIES and EQUIVALENT) is accepted on
   input and maps to the canonical named operator. The canonical printer and persisted DSL text
   remain word-only. `True`/`False`/`Unknown` literals are case-insensitive,
   and `Unknown` becomes a valid literal.
3. **Primitive kernel and derived operators.** Primitives: `NOT`, `AND`, `OR`,
   `AtLeast`, `AtMost`, `Exactly`, `COALESCE`. Derived: `IMPLIES` (`¬A ∨ B`,
   Strong Kleene material implication), `EQUIVALENT` (alias `IFF`), `XOR`,
   `NAND`, `NOR`, `ANY`, `ALL`, `NONE`, `BETWEEN`. Inspection: `IsTrue`,
   `IsFalse`, `IsUnknown`, `IsKnown`. Conditional: `If` / `? :`. Project and
   Collapse are methods on `Decision`, not rule constructs (decisions 12 and 14).
3a. **Derived operators remain first-class AST nodes** with their own
   evaluation, label/description and printing, rather than being desugared at
   parse time. Their primitive definitions are used by the optional
   expand-to-primitives transform, by the analyzer, and as the conformance
   oracle in tests. This preserves the author's operator on round-trip.
4. **XOR family.** `XOR(a, b)` is binary. `PARITY(a, b, ...)` is n-ary **parity**
   (odd number of `True`); any `Unknown` operand yields `Unknown`
   (`.tmp/xor.md`). `ExactlyOne` is retained and is a different operation
   (exactly one `True`, evaluated with the cardinality interval semantics), as
   is `Exactly(1, ...)`. ADR-0003's concern that n-ary XOR is ambiguous is
   resolved by the distinct name `PARITY`.

   *Amended 2026-10-03 (k3-followups 06); the operator was originally named
   `NXOR`.* `NXOR` conventionally means negated XOR (`XNOR`), which is exactly
   `EQUIVALENT` here and the opposite of what n-ary parity does, so the name
   was misleading. It is renamed `PARITY` in every layer (DSL, JSON/YAML `op`
   `parity`, schema, `RuleBuilder.Parity`, `ParityExpression`, labels, printers,
   rewrites and analyzer) and the `NXOR` spelling is removed, not kept as an
   alias. Rule text, JSON or YAML that still writes `NXOR` is rejected
   (`SyntaxError` in the DSL, `MalformedTree` in JSON and YAML) with a "did you
   mean `PARITY`" suggestion. The word stays reserved so a predicate cannot
   take it. Semantics are unchanged. Occurrences of `PARITY` in the other
   decisions below were `NXOR` when written.
5. **`EQUIVALENT` replaces `XNOR` as the canonical biconditional.** `IFF` and
   `XNOR` are accepted on input as aliases producing the same node. JSON/YAML
   canonical op name is `equivalent`; `xnor` and `iff` are accepted on read.
   Persisted rules written with `xnor` continue to compile.
6. **Cardinality uses the `[definitely true, possibly true]` interval.**
   `ANY` = `AtLeast(1, ...)`, `ALL` = `AtLeast(n, ...)`, `NONE` = `AtMost(0, ...)`,
   `BETWEEN(min, max, ...)` = `AND(AtLeast(min, ...), AtMost(max, ...))`.
   This supersedes CONTEXT.md's "there are no `All`/`None` operators".

## Amendments (2026-10-02 grilling, round 2)

7. **`XOR` with more than two operands remains a compile error**
   (`InfixArityViolation`); the diagnostic message hints at `PARITY` for parity.
8. **Precedence.** `NOT` > `AND` > `OR` is unchanged. Every other infix
   operator (`XOR`, `EQUIVALENT`, `NAND`, `NOR`, `IMPLIES`, `??`) must not be
   mixed with another infix operator at the same nesting level without
   parentheses (compile error, extending the existing `XOR`/`XNOR` rule).
   Function-call forms (`PARITY(...)`, `ANY(...)`, `If(...)`) have no precedence.
9. **Delimiters.** `()`, `[]`, `{}` are interchangeable grouping; the AST does
   not retain which was written. Printing normalizes to parentheses by
   default, with an optional deterministic depth-cycling renderer.
   Implemented (k3-conformance 20): the lexer has `{`/`}` tokens and every
   grouping site accepts any of the three pairs, but the argument list of a
   function-call operator or term (`ANY(...)`, `Role(name: "x")`) is still
   `(` only, because the decision says "grouping" and the reference material
   only shows delimiters around sub-expressions. A closer must match its
   opener: a wrong closer is a `SyntaxError` at that closer ("Expected ')' to
   close '(' at offset 4 but found ']'."), end of input is reported at the
   opener ("Unclosed '(' ..."), and a closer with nothing open is reported as
   "Unexpected closing ..." at the closer.
   Implemented (k3-conformance 21): `CompiledRule.PrintRuleText(GroupingStyle)`
   with `GroupingStyle.Parentheses` (identical to `CanonicalText`, the default
   and persisted form) and `GroupingStyle.DepthCycling`. The cycle is by group
   depth: outermost group `(`, then `[`, then `{`, then repeating (the
   reference example `A AND (B OR [C AND {D OR (E AND [F OR G])}])`). Only
   groups the printer wraps count as depth; function-call argument lists stay
   `(` and do not deepen it. The reference material's "option to convert all
   delimiter pairs to parens" is the default, so it needs no flag.
   Whitespace normalization is implemented (k3-conformance 22) as the
   text-level `RuleText.NormalizeWhitespace`, which works on text as written
   (no compile, no registry, keeps operators, case and delimiters) and is
   idempotent; the canonical printer already prints single spaces around
   infix operators and after commas, so printed text needs nothing more.
10. **Expression mutation** (primitive/NAND/NOR expansion, compression,
    simplification, canonicalization, whitespace normalization) all ship in
    this effort. Every rewrite must be K3-sound, verified exhaustively against
    the truth-table oracle; classical laws that fail in K3 (for example
    `A OR NOT A = True`) are not applied.

    Implemented in k3-conformance 23 (primitive expansion):
    `CompiledRule<TContext>.ExpandToPrimitives()` returns a new rule (the original
    is immutable and untouched)
    whose tree holds only the kernel (`NOT`, `AND`, `OR`, `AtLeast`, `AtMost`,
    `Exactly`, `COALESCE`), constants and terms. Definitions, each verified
    exhaustively against the oracle (and a random-rule property test over every
    assignment): `IMPLIES` = `OR(NOT a, b)`; `XOR` = `OR(AND(a, NOT b), AND(NOT a,
    b))`; `EQUIVALENT` = `OR(AND(a, b), AND(NOT a, NOT b))`; `NAND`/`NOR` = `NOT`
    of `AND`/`OR`; `ExactlyOne` = `Exactly(1, ...)`; `ANY`/`ALL`/`NONE` =
    `AtLeast(1)`/`AtLeast(n)`/`AtMost(0)`; `BETWEEN` = `AND(AtLeast(min),
    AtMost(max))` with a vacuous bound (`min = 0` or `max = n`) dropped, since the
    compiler rejects those thresholds as constants; `GreaterThan(k)` =
    `AtLeast(k + 1)` and `LessThan(k)` = `AtMost(k - 1)` (the compiler's valid
    ranges map exactly onto the targets' valid ranges); `If` = the multiplexer
    plus consensus term of decision 13.
    **`PARITY` (parity)** is `OR(Exactly(1, ...), Exactly(3, ...), ...)` over every
    odd count rather than a fold of the `XOR` expansion: with no `Unknown`
    operand the interval is a single count and the disjunction is `True` iff it is
    odd; with an `Unknown` operand the interval has two or more consecutive
    counts, so every matching `Exactly(k)` is `Unknown` and at least one odd count
    lies inside it, which gives `Unknown`. That is exactly parity's "Unknown if
    any operand is Unknown", and the size is linear (a fold repeats its
    accumulator twice per step, growing exponentially). **The inspections need no
    semantic boundary:** `COALESCE` is the one primitive that can observe
    `Unknown`, so `IsTrue(x)` = `COALESCE(x, False)`, `IsFalse(x)` =
    `COALESCE(NOT x, False)`, `IsUnknown(x)` = `AND(COALESCE(x, True), COALESCE(NOT
    x, True))` and `IsKnown(x)` = `OR(COALESCE(x, False), COALESCE(NOT x, False))`.
    Operands a definition repeats are one shared, already-expanded node (a DAG in
    memory; the printed text repeats them), so a deeply nested expansion can
    exceed the default compile node limit when recompiled.

    Implemented in k3-conformance 24 (universal gates):
    `CompiledRule<TContext>.ExpandToNand()` and `ExpandToNor()` expand to the
    kernel and then rewrite it with a single gate: `NOT a` = `a GATE a`; for NAND,
    `a AND b` = `(a NAND b) NAND (a NAND b)` and `a OR b` = `(a NAND a) NAND (b NAND
    b)`, and for NOR the duals; longer chains fold left. `AtLeast(k)` is the
    disjunction over every k-subset of the subset's conjunction (a monotone
    formula, so exact under the interval semantics), `AtMost(k)` = `NOT
    AtLeast(k + 1)`, `Exactly(k)` = `AtLeast(k) AND AtMost(k)` with a vacuous side
    dropped; the subset count is `C(n, k)`. **`COALESCE` is a documented semantic
    boundary:** every `NAND`/`NOR`/`AND`/`OR`/`NOT` circuit is monotone in the
    information order and `COALESCE` is not (`COALESCE(Unknown, True)` = `True` but
    `COALESCE(False, True)` = `False`), so it has no gate-only form and stays in
    place with its operands rewritten, as do the inspections that
    expand to it. A rule without them is purely `NAND` (or `NOR`).

    Implemented in k3-conformance 25 (compression):
    `CompiledRule<TContext>.CompressToDerived()` is the opt-in inverse of
    expansion. It matches primitive shapes top-down and rewrites them to derived
    operators: `OR(NOT a, b)` to `IMPLIES`; `NOT(AND(a, b))` and `OR(NOT a, NOT b)`
    to `NAND`; `NOT(OR(a, b))` and `AND(NOT a, NOT b)` to `NOR`; the exact
    `XOR`, `EQUIVALENT`, `If` and `PARITY` shapes the expander emits; the
    threshold-to-alias rows (`AtLeast(1)` `ANY`, `AtLeast(n)` `ALL`, `AtMost(0)`
    `NONE`, `Exactly(1)` `ExactlyOne`, `NOT AtLeast(k)` `AtMost(k - 1)`);
    `AND(AtLeast(m), AtMost(M))` over the same operands to `BETWEEN`; and the
    `COALESCE` forms to `IsFalse` (`COALESCE(NOT x, False)`) and, as two-sided
    pairs, to `IsUnknown` and `IsKnown`. Each rewrite never adds nodes, so
    the result is never larger than the input; passes repeat until stable, so
    compression is idempotent. It recovers an equivalent derived form, not
    necessarily the original; a `COALESCE` with a constant that matches no
    inspection pattern is left as written (it is already its shortest form). Shared operands are preserved.

    Implemented in k3-conformance 26 (canonicalisation):
    `CompiledRule<TContext>.Canonicalize()` applies an ordered, K3-sound rule set
    bottom-up until stable: (1) exact aliases collapse (`ANY`/`AtLeast(1)` to `OR`,
    `ALL`/`AtLeast(n)` to `AND`, `GreaterThan(k)` to `AtLeast(k + 1)`, `LessThan(k)`
    to `AtMost(k - 1)`, `ExactlyOne` to `Exactly(1)`); (2) `NOT NOT x` to `x`; (3)
    nested `AND`/`OR`/`COALESCE` flatten; (4) operands of the commutative
    operators sort by canonical text (ordinal); (5) repeated `AND`/`OR` operands
    are removed. `IMPLIES`, `NAND`, `NOR`, `NONE` and the inspections keep
    their spelling (rewriting them to primitives would grow the tree, and the
    canonical form is never larger than its input); `COALESCE`, `IMPLIES` and `If`
    keep operand order. No complement law and no constant folding. It is
    idempotent and deterministic, and, because it reorders and deduplicates
    operands, it does not preserve evaluation order, short-circuiting or which
    faults are reported, only the value; this is stated in the API remarks.

    Implemented in k3-conformance 27 (simplification):
    `CompiledRule<TContext>.Simplify()` canonicalises, then repeats a bottom-up
    rewrite sweep plus canonicalisation until nothing changes, and returns the
    input unchanged if the result were ever larger. Rules: constant folding;
    identity/annihilator laws for `AND`/`OR` (an `Unknown` operand is kept);
    absorption `a AND (a OR b) = a` (a lattice law that holds in K3); `NOT` of a
    constant, `NOT NAND`, `NOT NOR`, `NOT IsKnown`/`IsUnknown`, threshold flipping,
    and De Morgan only where it removes nodes; `COALESCE`/inspections
    of constants and of operands that can never be `Unknown`; `If` with a constant
    condition or equal branches; threshold operands that are `True`/`False`
    eliminated by shifting `k`, out-of-range thresholds folded; and any other
    derived operator with a constant operand expanded one level
    (`PrimitiveExpander.ExpandTop`), simplified, and kept only if no larger.
    Classical-only laws are never used: excluded middle, non-contradiction,
    `a IMPLIES a`, `a EQUIVALENT a`, `a XOR a`, complement absorption, and an
    `If` with an `Unknown` condition. The analyzer's dual-rail findings are not
    used (they are diagnostics for authors; every rewrite here is local and
    structural). Like canonicalisation it preserves the value, not evaluation
    order, short-circuiting or which faults are reported.
11. **Validation messages are structured**: code, message, span (or
    JSON/YAML path), optional "did you mean" suggestion, and an
    expected-vs-found pair, with a plain-text rendering.

    Implemented in k3-conformance 28 (DSL). `Diagnostic` keeps its positional
    members and gains optional, init-only `Path`, `Expected`, `Found` and
    `Suggestion` (a `DiagnosticSuggestion` of kind `Replacement` for "did you
    mean" or `Hint` for advice), set through optional parameters on
    `Diagnostic.Error/Warning/Info`; existing message text is unchanged, so the
    structure is additive. `SourceSpan.GetLocation(source)` derives the 1-based
    line and column (`SourceLocation`). `DiagnosticFormatter.Format` (one or
    many) and `CompilationResult.FormatDiagnostics(source)` render the plain
    text: a header (`BRE0001 error at line 1, column 3: ...`), the source line
    with a caret underline, then `Expected:`, `Found:` and `Did you mean:` /
    `Hint:` lines. Suggestions use an internal, deterministic
    optimal-string-alignment distance (case-insensitive, cut-off 1/2/3 edits for
    words of up to 4/8/more characters, ties to the ordinally first candidate)
    over the DSL vocabulary and the registry's predicate names. The shared
    `InfixArityViolation` code (`BRE0006`) is kept for the five binary operators;
    its `Expected`/`Found` carry the operand counts and the `Suggestion` names
    `PARITY`/`ExactlyOne` for `XOR` and nesting for the others. The
    no-mixing "add parentheses" advice is now also a `Hint` suggestion that
    quotes the operand wrapped in parentheses where there is one.

    Implemented for JSON and YAML in k3-conformance 29. The same `Diagnostic`
    carries a `Path` alongside `Span`: `$` is the document root, `.name` a key,
    `[n]` a 0-based sequence item, `['key']` for a key that is not a plain
    identifier; JSON and YAML share the syntax. Raw parse nodes (and arguments)
    carry the path (an internal `Path` init property), and the compiler copies it
    onto its own diagnostics, so a validation error found after parsing is
    located the same way. A wrong field is located at the field (`.k`,
    `.min`, `.max`, `.const`, `.op`, `.predicate`, `.args.name`), a
    wrong operand count at `.operands`, and a missing key at the node that should
    have held it. `Unknown operator` is checked before the operands are read and
    is answered with the nearest tree-format op name (including the read-only
    `xnor`/`iff` aliases); an
    unknown predicate in a tree is only ever answered with a registered predicate
    name, never a DSL operator word. YAML diagnostics also carry the `Span` of the
    offending node (from YamlDotNet's marks); JSON diagnostics have none, because
    `JsonElement` keeps no positions, except invalid-syntax diagnostics, which use
    the reader's line and byte position. For invalid syntax the path is the
    innermost container still open when the reader stopped. `FormatDiagnostics`
    prints `at $.path` (plus ` (line L, column C)` when there is a span).

12. **Project is a method on the result, not part of the rule.**
    *Amended 2026-10-03 (k3-followups 05); this replaces the original decision,
    which made `Project(expr, unknown)` an in-tree node (`ProjectExpression`) that
    could appear anywhere in a rule.* A rule always yields its raw three-valued
    result, and `Project` is no longer an operator in the DSL, JSON, YAML, the
    builder or the schema. Inside a rule, `COALESCE(x, True)` and `COALESCE(x,
    False)` do the same job and remain rule-level operators; on the result, the
    application calls `Decision.Project(bool unknownAs)`, which keeps `True` and
    `False` and replaces `Unknown` with the chosen definite value. The parameter is a
    `bool` so an `Unknown` replacement cannot be requested. It is a pure function
    of `Decision.Result`: it never changes the decision, never adds or hides a
    `Fault` (a faulting predicate is still `Unknown` plus a `Fault`) and leaves
    `Decision.IsSatisfied` fail-closed. It equals `COALESCE(rule, value)` for
    every input. **Removed from the rule language:** the DSL `Project(expr, True|False)`
    function, the JSON/YAML `project` node and its `unknownAs` field,
    `RuleBuilder.Project`, `ProjectExpression`, `NodeShape.UnknownAs`,
    `rule-tree.schema.json`'s `projectOperatorNode`, the analyzer, evaluator,
    printer and `RuleDiff` handling, and the `Project(True)` / `Project(False)`
    description label. The expansion and compression rewrites no longer produce or
    recognise a project node: `ExpandToPrimitives` has nothing to expand and
    `CompressToDerived` leaves `COALESCE(x, True|False)` as written, the shortest
    form (only `COALESCE(NOT x, False)` still compresses, to `IsFalse`), and
    `Simplify` treats a `COALESCE` with a definite operand as definite. A rule that
    still declares a `Project` is rejected, wherever it appears and in any letter
    case, with an error whose message and hint point to `COALESCE` and
    `Decision.Project`: `SyntaxError` (`BRE0001`) spanning the whole call in DSL
    text, and `MalformedTree` (`BRE0014`) at the node's path in JSON and YAML.
    `Project` stays a reserved word so a predicate cannot shadow it.
13. **JSON/YAML node shapes** for `If`, inspection, boundaries and the
    `Unknown` literal follow the existing `{"op": ..., "operands": [...]}`
    pattern (literal: `{"op": "unknown"}`) and are recorded here when
    implemented. Implemented so far (k3-conformance 09): `IMPLIES` is
    `{"op": "implies", "operands": [antecedent, consequent]}` (exactly two
    operands, compile-time checked like `XOR`; op name case-insensitive on
    read) in both JSON and YAML (`op: implies`), and `rule-tree.schema.json`
    lists `implies` among the operator ops. The DSL accepts `IMPLIES` and `→`;
    the canonical printer writes `(a IMPLIES b)`; the symbolic tree-printer
    style renders `→`, while the C-style has no spelling for it and keeps
    `IMPLIES`.

    Implemented in k3-conformance 10: `EQUIVALENT` is
    `{"op": "equivalent", "operands": [left, right]}` (exactly two operands,
    compile-time checked with the shared infix arity code); `xnor` and `iff`
    are read-only aliases in both formats (the shared op-name table has a
    separate read table) and the printers always write `equivalent`;
    `rule-tree.schema.json` lists `equivalent`, `iff` and `xnor`. The DSL
    accepts `EQUIVALENT`, `IFF`, `XNOR` and `↔` (all reserved words, any
    case) and the canonical printer writes `(a EQUIVALENT b)`; the tree
    printers use `EQUIVALENT` / `↔` / `==` for the word / symbolic / C-style
    operator styles. **Public API break (pre-1.0):** the AST record
    `XnorExpression` is renamed `EquivalentExpression` (a record cannot be
    type-aliased), its `NodeShape` op-name is `Equivalent`, and the
    description/evaluated-node label is `EQUIVALENT` instead of `XNOR`.
    `RuleBuilder.Xnor` is kept as a forwarding member of the new
    `RuleBuilder.Equivalent`.

    Implemented in k3-conformance 11: `NAND` and `NOR` are strictly binary
    (the spec's operator table says binary, and a chain is ambiguous for a
    non-associative operator), first-class `NandExpression` /
    `NorExpression` nodes with `{"op": "nand" | "nor", "operands": [left,
    right]}` in JSON and YAML (case-insensitive on read, exactly two operands,
    compile-time checked with the shared infix arity code and a parentheses
    hint). The DSL accepts `NAND`, `NOR`, `↑` and `↓` (reserved words, any
    case) as infix operators subject to the no-mixing rule; the canonical
    printer writes `(a NAND b)` / `(a NOR b)`; the symbolic tree-printer style
    renders `↑` / `↓`, while the C-style has no spelling and keeps the words.
    Evaluation and the analyzer rail are the negated primitive
    (`NOT (a AND b)`, `NOT (a OR b)`); both operands are always evaluated.
    `rule-tree.schema.json` lists `nand` and `nor`. `RuleBuilder.Nand` and
    `RuleBuilder.Nor` are new.

    Implemented in k3-conformance 12: `PARITY` is a first-class
    `ParityExpression` function-call node (no precedence, so it needs no
    parentheses next to infix operators) with `{"op": "parity", "operands":
    [...]}` in JSON and YAML (case-insensitive on read). It takes **two or more**
    operands (fewer is `MalformedTree`, like `AND`/`OR`/`ExactlyOne`), and the
    DSL spelling is `PARITY(a, b, ...)` (reserved word, any case). It is
    `Unknown` whenever any operand is `Unknown`, otherwise `True` for an odd
    number of `True` operands; evaluation folds binary XOR and the analyzer rail
    is the same fold of the XOR rail. The canonical printer writes
    `PARITY(a, b, ...)`; every tree-printer style keeps the word (no symbol or
    C-family spelling). The `XOR` arity message (still `InfixArityViolation`) now
    names `PARITY` and `ExactlyOne`. `ExactlyOne` is unchanged and differs from
    `PARITY` from three operands on. `rule-tree.schema.json` lists `parity`.
    `RuleBuilder.Parity` is new.

    Implemented in k3-conformance 13: `ANY`, `ALL` and `NONE` are first-class
    `AnyExpression` / `AllExpression` / `NoneExpression` function-call nodes (no
    precedence) with `{"op": "any" | "all" | "none", "operands": [...]}` in JSON
    and YAML (case-insensitive on read) and the DSL spellings `ANY(...)`,
    `ALL(...)`, `NONE(...)` (reserved words, any case). They take **two or more**
    operands, the same minimum as `AND`/`OR`/`ExactlyOne`/`PARITY` (fewer is
    `MalformedTree`); the threshold family's one-operand allowance is not
    inherited because a single-operand `ANY`/`ALL`/`NONE` is just the operand
    or its negation. Semantics are the cardinality interval over the
    definitely-true / possibly-true counts: `ANY` = `AtLeast(1, ...)`, `ALL` =
    `AtLeast(n, ...)`, `NONE` = `AtMost(0, ...)` (evaluation reuses the
    threshold evaluator; the analyzer rail reuses `AtLeast`, with `NONE` as its
    negation). The canonical printer writes `ANY(a, b, ...)` etc.; every
    tree-printer style keeps the word (no symbol or C-family spelling) and the
    evaluated/description label is `ANY`/`ALL`/`NONE`. `rule-tree.schema.json`
    lists `any`, `all` and `none`. `RuleBuilder.Any`, `All` and `None` are new.

    Implemented in k3-conformance 14: `BETWEEN(min, max, op1, op2, ...)` is a
    first-class `BetweenExpression(Min, Max, Operands)` function-call node (no
    precedence): the first two arguments are integer bounds, parsed like the
    threshold family's `k` (a missing or non-integer bound is a `SyntaxError`
    naming the minimum or maximum), then the operands. It is
    `AND(AtLeast(min, ...), AtMost(max, ...))` over the definitely-true /
    possibly-true interval (the evaluator ANDs the two threshold results; the
    analyzer rail is `AtLeast(min)` AND NOT `AtLeast(max + 1)`). Operand
    minimum: **two or more**, as `ANY`/`ALL` (`MalformedTree`). Bound range:
    `0 <= min <= max <= n` for `n` operands, and the whole range `0..n` is
    rejected because the node would be the constant `True`, the same
    structural-constant rationale as the threshold family (all of these are
    `InvalidThresholdValue`, with a message naming `min=`, `max=` and the
    allowed range). JSON/YAML: `{"op": "between", "min": 1, "max": 2,
    "operands": [...]}` (case-insensitive op, integer `min`/`max` required
    else `MalformedTree`); `rule-tree.schema.json` has a `betweenOperatorNode`.
    `NodeShape` gained an optional `Max` (its `K` carries `min`). The
    canonical printer writes `BETWEEN(1, 2, a, b, c)`; the evaluated and
    description label is `BETWEEN(min, max)`, kept as a word in every
    `OperatorStyle`. `RuleBuilder.Between(min, max, operands)` is new.

    Implemented in k3-conformance 15: `COALESCE(a, b, ...)` and the infix `??`
    build one `CoalesceExpression(Operands)`: the first operand that is not
    `Unknown` (`True`/`False` pass through; `Unknown` only if all are).
    `??` is an infix operator under decision 8: it cannot share a level with
    `AND`/`OR` or another infix operator without parentheses. **Chains are
    accepted**: `a ?? b ?? c` is one three-operand node, because coalescing is
    associative (unlike the binary-only `IMPLIES`/`NAND`/`NOR`, whose chains
    stay errors); its operands are `NOT`-level expressions. Only the `??`
    token is infix; the word `COALESCE` is a function call only (a lone `?` is
    a lexical error). Two or more operands are required (`MalformedTree`).
    Evaluation is left to right and stops at the first non-`Unknown` operand
    (skipped operands appear as `NotEvaluated` in the evaluated tree and trace,
    like `AND`/`OR`; `EvaluationMode.Exhaustive` evaluates all). The analyzer
    rail folds from the right: with `(D, P)` the definite/possible rails of
    `x`, `COALESCE(x, y)` is `(D_x OR (P_x AND D_y), P_x AND (D_x OR P_y))`.
    The canonical printer writes the function-call form `COALESCE(a, b)`; the
    tree printers spell the label `COALESCE` / `??` / `??` for the word /
    symbolic / C-style styles. JSON/YAML op `coalesce`; `RuleBuilder.Coalesce`
    is new.

    Implemented in k3-conformance 16: `If(condition, whenTrue, whenFalse)` and
    the ternary `condition ? whenTrue : whenFalse` build one
    `IfExpression(Condition, WhenTrue, WhenFalse)`. **Semantics:** a `True`
    condition yields `whenTrue`, a `False` one `whenFalse`; an `Unknown`
    condition does not guess a branch, so the result is the branch value only
    when both branches are the same definite value, else `Unknown`
    (`.tmp/Strong Kleene K3 Logic.md` section 25). The primitive definition,
    used by the analyzer rail and the test oracle, is therefore the
    multiplexer plus its consensus term: `(c AND t) OR (NOT c AND f) OR
    (t AND f)`. The bare multiplexer `(c AND t) OR (NOT c AND f)` was
    rejected because it yields `Unknown` for `If(Unknown, True, True)`, which
    contradicts the "does not guess a branch" intent and the reference
    specification; the consensus term never changes the result of a definite
    condition. **Evaluation** skips the branch a definite condition does not
    need (recorded as `NotEvaluated`, like `AND`/`OR`/`COALESCE`); an `Unknown`
    condition evaluates both; `EvaluationMode.Exhaustive` evaluates both. The
    analyzer rail is the primitive definition over the dual rails, so
    `If(a, b OR True, c OR True)` is a tautology even for an `Unknown` `a`.
    **Syntax:** `If` is a reserved function-call word (any case) taking
    exactly three operands (`MalformedTree` otherwise). The lone `?` and `:`
    form the ternary, the lowest-precedence construct, accepted wherever a full
    expression is (the root, parentheses, call arguments). Under decision 8 the
    condition and each branch must each be a single operand or a parenthesized
    group: a bare `AND`/`OR` chain, a bare infix expression (`XOR`, `??`, ...)
    or an unparenthesized nested ternary in any of the three positions is
    `AmbiguousOperatorMixing`. The canonical printer writes the function-call
    form `If(a, b, c)`; tree printers and the evaluated/description label are
    `If` in every `OperatorStyle` (no symbolic or C-style spelling). JSON/YAML:
    `{"op": "if", "operands": [condition, whenTrue, whenFalse]}` (op name
    case-insensitive on read; `rule-tree.schema.json` lists `if`).
    `RuleBuilder.If` is new.

    Implemented in k3-conformance 17: `IsTrue(x)`, `IsFalse(x)`, `IsUnknown(x)`
    and `IsKnown(x)` are **one** `InspectionExpression(Kind, Operand)` node with
    an `InspectionKind` (the smallest design consistent with the `NodeShape`
    seam: like the threshold family they differ only in what they test, and
    `NodeShape.OpName` is the kind's name). They are function calls (reserved
    words, any case) taking exactly one operand (`MalformedTree` otherwise) and
    always yield a definite `True` or `False`: `IsTrue` is "is `True`",
    `IsFalse` is "is `False`", `IsUnknown` is "is `Unknown`", `IsKnown` is "is
    not `Unknown`". The operand's own faults are recorded as usual but the
    inspection adds none, and a definite result never collapses the rest of
    the enclosing rule. The operand is always evaluated. The analyzer rail has
    equal definite and possible rails: with `(D, P)` the rails of `x`, `IsTrue`
    is `D`, `IsFalse` is `NOT P`, `IsUnknown` is `P AND NOT D`, `IsKnown` is `D
    OR NOT P`, so `IsUnknown(a) OR IsKnown(a)` is a genuine tautology. The
    canonical printer writes `IsTrue(a)` etc.; the evaluated/description label
    is the same word in every `OperatorStyle`. JSON/YAML: `{"op": "isTrue" |
    "isFalse" | "isUnknown" | "isKnown", "operands": [x]}` (case-insensitive on
    read, one operand checked by the compiler; the schema lists them in the
    unary node). `RuleBuilder.IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` are new.
14. **Collapse is a method on the result, not part of the rule.**
    *Amended 2026-10-03 (k3-followups 04); this replaces the original decision,
    which made `Collapse(expr, policy)` a rule-language function accepted only as a
    rule's outermost expression and recorded on the compiled rule.* A rule always
    yields its raw three-valued result: `Decision.Result` is never altered by a
    collapse. The application chooses how an `Unknown` becomes a two-valued answer at
    the call site with `Decision.Collapse(CollapsePolicy)`. Policies:
    `UnknownAsFalse`, `UnknownAsTrue`, `UnknownIsError`. `Unknown` is a normal K3
    value, not a failure: `UnknownIsError` yields an explicit "rejected:
    unresolved" outcome and never a `Fault` or exception; `Decision.Faults` is
    reserved for real predicate exceptions, timeouts and cancellation.
    `UnknownRequiresResolution` is out of scope. The rationale is the same split the
    SQL `WHERE`/`CHECK` and XACML PDP/PEP precedents make: the decision point
    reports what it knows, and the enforcement point decides what an unknown means for
    the action it guards. Putting the policy in the rule would let rule text silently
    change `Result` and weaken the fail-closed `IsSatisfied`.

    **Public API** (in `TruthWeaver.Abstractions`): the `CollapsePolicy` enum, the
    `CollapseOutcome` enum (`False`, `True`, `RejectedUnresolved`) and
    `Decision.Collapse(CollapsePolicy)`, a pure function of `Decision.Result`
    (`True`/`False` map to themselves and only `Unknown` depends on the policy). It
    never records a fault, never changes the decision, and `RejectedUnresolved` is a
    normal outcome: nothing is thrown and `Decision.Faults` is untouched (a faulting
    predicate still records its fault, which is how "not known" and "something
    broke" stay distinguishable). `Decision.IsSatisfied` is `Result == True`,
    fail-closed, whatever policy a caller applies. There is no `Decision.Outcome`.
    **Removed from the rule language:** the DSL `Collapse(expr, policy)` function, the
    JSON/YAML `collapse` node and its `policy` field, `RuleBuilder.Collapse`,
    `CompiledRule.CollapsePolicy`, the collapse root of `Describe()` and of the
    evaluated tree, `rule-tree.schema.json`'s `collapseOperatorNode`, the printers'
    and `RuleDiff`'s collapse handling and the `NestedCollapse` diagnostic
    (`BRE0016`, retired and not reused). A rule that still declares one is rejected,
    wherever it appears and in any letter case, with an error whose message and hint
    point to `Decision.Collapse`: `SyntaxError` (`BRE0001`) spanning the whole call in
    DSL text, and `MalformedTree` (`BRE0014`) at the node's path in JSON and YAML.
    `Collapse` stays a reserved word so a predicate cannot shadow it.
15. **Predicates return `TruthValue`.** `IPredicate` and every predicate
    delegate return `TruthValue` (breaking change, pre-1.0). A returned
    `Unknown` records no `Fault`; an exception, timeout or cancellation still
    becomes `Unknown` plus a `Fault` (ADR-0001).
16. **`Unknown` is a first-class constant and substitution value.**
    Constants are `TruthValue`s end to end. Lenient-mode and failed-node
    substitutions use `Unknown`, not `false`. `True`, `False`, `Unknown` and
    every operator name are case-insensitive; the canonical printer writes
    `True`, `False`, `Unknown` and upper-case operators. Serialized form
    (implemented in k3-conformance 03): JSON `{"const": true|false}` is kept,
    and `Unknown` is `{"const": "unknown"}` (a string constant in any letter
    case is also accepted for all three values); YAML is `const: unknown`.
17. **The analyzer is K3-aware.** A dual-rail BDD tracking "definitely true"
    and "possibly true" replaces classical two-valued analysis, so
    `A AND NOT A` is a K3 contradiction only when it is, and `A OR NOT A` is
    not a tautology. Implemented in k3-conformance 06 (the interim relabel
    from ticket 05 is superseded). Each term contributes two independent BDD
    variables (is `True`; is `Unknown`), so every variable setting is a valid
    K3 state. A sub-expression is reported as a tautology (`BRE0012`) when its
    definitely-true rail is constant true and as a contradiction (`BRE0013`)
    when its possibly-true rail is constant false. The `Structural*` constant
    names and codes are kept for stability. Each later operator slice extends
    the analyzer with its own rail definition.

## Amendments (2026-10-03, k3-followups 15)

These record the final decisions of the k3-followups effort and the audit
([spec audit](../../.scratch/k3-conformance/spec-audit.md) sections C and D).
Where they differ from the original wording above they take precedence.

18. **Only the connectives are Strong Kleene (K3); `COALESCE` and the
    inspections are external operators.** Decision 3 calls `COALESCE` part of the
    "primitive kernel". It is a kernel operator in the sense that it is the one
    primitive that can observe `Unknown`, but it is not a K3 connective. Neither
    are `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`. The K3 connectives are
    `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, `PARITY`,
    the cardinality operators (`AtLeast`, `AtMost`, `Exactly`, `ExactlyOne`,
    the threshold family, `ANY`, `ALL`, `NONE`, `BETWEEN`) and `If`. The
    distinction is the **information order** (`Unknown` below `True` and
    `False`, which are incomparable), which sits beside the truth order
    `False < Unknown < True` of decision 1. Every connective is monotone in the
    information order (refining an `Unknown` input never changes a definite
    output); the external operators are not (`COALESCE(Unknown, False)` is
    `False`, `COALESCE(True, False)` is `True`). The precedents are SQL
    (`COALESCE`, `IS [NOT] TRUE/FALSE/UNKNOWN`) and Bochvar's external
    connectives. Consequences: the "no tautologies" theorem and the
    expressiveness of `NAND`/`NOR` (decision 10, k3-conformance 24) hold for the
    connectives and do not extend to the external operators, so
    `IsKnown(a) OR IsUnknown(a)` is a genuine tautology (decision 13,
    k3-conformance 17) and `COALESCE` stays outside the universal gates. The
    language as a whole is Strong Kleene (K3) plus external operators. Prose
    uses "Strong Kleene (K3)", not "K3" and "Strong K3" as two systems.
19. **`Project` and `Collapse` are TruthWeaver terms and are methods on the
    result.** Neither name appears in the K3 literature (and "projection" means
    column selection in relational algebra). Decisions 12 and 14 (amended
    2026-10-03) are final: `Decision.Project(bool unknownAs)` and
    `Decision.Collapse(CollapsePolicy)` are pure functions of `Decision.Result`,
    which is always the raw three-valued value, and `IsSatisfied` stays
    fail-closed.
20. **`PARITY` is the final name** for n-ary parity (decision 4, amended
    2026-10-03). `NXOR` conventionally means negated `XOR`, which is
    `EQUIVALENT` here, the opposite of parity.
21. **`If` rationale.** Decision 13's consensus definition is the *strongest
    extension* of the classical conditional: `If(c, t, f)` is definite exactly
    when every `True`/`False` resolution of the `Unknown` inputs gives the same
    answer, which holds for all 27 `(c, t, f)` triples. This is the
    metastability-containing multiplexer result (Friedrichs, Függer and Lenzen,
    "Metastability-Containing Circuits", IEEE Trans. Computers 2018, section 3,
    which adds the `t AND f` term to the standard multiplexer) and the
    reason the classical consensus-removal rewrite is invalid in K3. The
    alternatives differ: the bare multiplexer and McCarthy's conditional give
    `If(Unknown, True, True)` = `Unknown`, and SQL searched `CASE` sends an
    `Unknown` condition to `ELSE` (differing at four triples). `If(IsTrue(c), t,
    f)` is the SQL `CASE` equivalent. The semantics are pinned by an exhaustive
    27-triple test.
22. **`BETWEEN` needs `min <= max`.** Decision 6's `AND(AtLeast(min),
    AtMost(max))` equals the strongest extension of the cardinality range only
    for `min <= max`; for an empty range the composition gives `Unknown` where
    every completion is `False`. The compiler already enforces
    `0 <= min <= max <= n` (decision 13, k3-conformance 14) and the compressor
    only builds `BETWEEN` when the bounds are ordered.

## Open decisions

None.

## Consequences

- ADR-0003's "closed operator set" and "no aliases" statements no longer hold;
  its remaining decisions (named arguments, literal-only arguments, pipeline,
  precedence of `NOT > AND > OR`) stand.
- Existing public API names (`XNOR`, `RuleBuilder.Xnor`) need aliases or
  deprecation, tracked in the planning tickets.
- The equivalency table in CONTEXT.md becomes a description of derived-operator
  definitions rather than a list of non-existent operators.
