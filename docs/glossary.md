# Glossary

The vocabulary of TruthWeaver, with the terms that are new or easy to confuse. Each entry gives one meaning and links to the page that defines the term in full. The reference for Strong Kleene (K3) logic is in [docs/strong-k3](strong-k3/README.md). The engine vocabulary, with the reasons behind each term, is in [CONTEXT.md](../CONTEXT.md).

## A to C

| Term | Meaning |
| --- | --- |
| **BDD analyzer** | The compiler pass that finds sub-expressions that are `True` for every assignment of `True`, `False` and `Unknown` to their terms, or `False` for every assignment. `a OR NOT a` is not reported, because it is `Unknown` when `a` is. See [diagnostics](strong-k3/specification/diagnostics.md#code-table). |
| **Canonical form** | For a derived Operation, its definition in terms of primitives, such as `OR(NOT(a), b)` for `IMPLIES`. It is not canonical text. See [terminology](strong-k3/specification/terminology.md#forms-of-an-operation). |
| **Canonical text** | The one spelling of a compiled rule that the printer writes. It is the form to store. It is not a canonical form. |
| **Category** | The one group that an Operation belongs to in the K3 reference, such as Gates / Operators or Cardinality Functions. See [terminology](strong-k3/specification/terminology.md#classifying-an-operation). |
| **Collapse** | A method on a `Decision`, not part of a rule. `Decision.Collapse(policy)` turns the result into a `CollapseOutcome`. `CollapsePolicy` is `UnknownAsFalse`, `UnknownAsTrue` or `UnknownIsError`. `CollapseOutcome` is `True`, `False` or `RejectedUnresolved`. See [Collapse](strong-k3/result-transformations/collapse.md). |
| **CompilationMode** | `Strict`, the default, makes an unregistered predicate a compile error. `Lenient` compiles it to a term that is always `Unknown`. |
| **CompilationResult** | What `Compile`, `CompileJson` and `CompileYaml` return: a `CompiledRule`, which is `null` when compilation fails, and every diagnostic. |
| **CompiledRule** | The immutable, thread-safe result of a successful compile. It can be cached, shared and evaluated many times. |
| **CompilerOptions** | The compile-time limits (tree depth, node count, analyzer term count, rewrite size) and the `CompilationMode`. |

## D to I

| Term | Meaning |
| --- | --- |
| **Data source** | A named supplier of values, declared when the compiler is built and supplied at evaluation. A variable reference reads from it. See [data sources](data-sources.md). |
| **Decision** | The result of one evaluation: a `TruthValue`, the faults that the evaluation absorbed and, if requested, a trace. `IsSatisfied` is `true` only for `True`. See [evaluation](strong-k3/specification/evaluation.md). |
| **Derived** | An Operation that has a definition in terms of primitives. It stays a node of its own. See [terminology](strong-k3/specification/terminology.md#classifying-an-operation). |
| **Diagnostic** | One compile-time problem or finding: a code, a severity, a message, and a span or path. Only the severity Error blocks compilation. See [diagnostics](strong-k3/specification/diagnostics.md). |
| **Evaluation mode** | `ShortCircuit`, the default, stops an Operation when its result is settled. `Exhaustive` runs every reachable term. The mode never changes the result. See [evaluation](strong-k3/specification/evaluation.md#evaluation-modes). |
| **EvaluationOptions** | The per-call options: `Mode`, `Timeout`, `FaultBudget` and `IncludeResolvedValues`. |
| **Expression** | The immutable tree of Operations over terms and constants. A `CompiledRule` wraps one. |
| **External operator** | An Operation that can observe `Unknown` and answer something definite, so it is not a Strong Kleene connective. These are `COALESCE` and the four inspections. See [semantics](strong-k3/specification/semantics.md#strong-kleene-connectives-and-external-operators). |
| **Fault** | A record that a predicate failed to answer during one evaluation, because it threw, timed out or was cancelled. The expression sees `Unknown`. A predicate that returns `Unknown` records no fault. See [evaluation](strong-k3/specification/evaluation.md#predicates-and-faults). |
| **Gate** | `NOT`, `AND` or `OR` as a Strong Kleene truth function. An Operator is the programmatic form of a gate. See [terminology](strong-k3/specification/terminology.md#classifying-an-operation). |
| **Information order** | `Unknown` is below `True` and `False`, which are not comparable. Strong Kleene connectives are monotone in this order. See [values](strong-k3/specification/values.md#information-order). |
| **Inspection** | `IsTrue`, `IsFalse`, `IsUnknown` or `IsKnown`. An inspection tests the state of its operand and always answers `True` or `False`. See [Functions](strong-k3/functions/README.md). |

## K to P

| Term | Meaning |
| --- | --- |
| **Kind** | `Primitive` or `Derived`. Every Operation document states it. |
| **Kleene logic** | Three-valued logic with `True`, `False` and `Unknown`. A predicate fault becomes `Unknown`, and it is never a thrown exception or a silent `false`. See [values](strong-k3/specification/values.md). |
| **Memoization** | Within one evaluation, a term identity runs at most once, however many places in the tree use it. It does not carry over to the next evaluation. |
| **Operation** | Anything with its own document in the K3 reference: an operator, a cardinality function, a function or a result transformation. See [operations](strong-k3/specification/operations.md). |
| **Operator** | One way to combine terms and sub-expressions, such as `AND` or `If`. The engine and its API say Operator. |
| **Outline** | The view of a compiled rule as a tree of labels and descriptions, from `CompiledRule.Outline()`. |
| **PARITY** | The n-ary operator that is `True` when an odd number of operands are `True`. It is not `ExactlyOne`. The two differ from three operands. See [PARITY](strong-k3/derived/parity.md). |
| **Predicate** | A registered, reusable function that answers `True`, `False` or `Unknown`. A predicate is the function, not one call to it. See [terminology](strong-k3/specification/terminology.md#engine-terms). |
| **PredicateRegistry** | Where predicates are registered by name together with their `PredicateSchema`. |
| **PredicateSchema** | The registered name, label and description of a predicate and the declarations of its arguments. |
| **Primitive** | An Operation with no definition in terms of other Operations: `NOT`, `AND`, `OR`, `AtLeast`, `AtMost`, `Exactly` and `COALESCE`. |
| **Project** | A method on a `Decision`, not part of a rule. `Decision.Project(unknownAs)` keeps `True` and `False` and replaces `Unknown` with `True` or `False`. In relational algebra, "projection" means selecting columns. See [Project](strong-k3/result-transformations/project.md). |
| **Public form** | The spelling that a rule author uses to name an Operation: the printed word, the JSON and YAML `op` value and the `RuleBuilder` member. See [terminology](strong-k3/specification/terminology.md#forms-of-an-operation). |

## R to Z

| Term | Meaning |
| --- | --- |
| **Rewrite** | An opt-in transform of a compiled rule that returns a new rule with the same value: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize` or `Simplify`. |
| **Rule** | A named unit of persistence: metadata and one expression. Compiling it gives a `CompiledRule`. |
| **RuleBuilder** | A fluent API that assembles a rule tree from application logic. It compiles through the same pipeline as JSON. |
| **RuleDiff** | A structural diff between two compiled rules: the nodes that were added, removed or changed. |
| **Short-circuit** | `AND` stops at the first `False`, `OR` at the first `True` and `COALESCE` at the first operand that is not `Unknown`. Skipped operands are recorded as `NotEvaluated`. See [evaluation](strong-k3/specification/evaluation.md#evaluation-modes). |
| **Strong Kleene connective** | An Operation that is monotone in the information order. Every rule operator of the K3 reference is one, except `COALESCE` and the four inspections. See [semantics](strong-k3/specification/semantics.md#strong-kleene-connectives-and-external-operators). |
| **Strongest extension** | The three-valued function that is definite exactly when every classical resolution of the `Unknown` inputs agrees. See [semantics](strong-k3/specification/semantics.md#truth-functional-evaluation-and-the-strongest-extension). |
| **Term** | A predicate bound to concrete arguments, such as `hasTopping(topping: "greenOlives")`. A term is a leaf of the tree and the unit of memoization. |
| **Term identity** | What makes two term references the same variable: the registered predicate name and the arguments sorted by name and compared by exact, case-sensitive value. |
| **Threshold family** | `AtLeast`, `AtMost`, `Exactly`, `GreaterThan` and `LessThan`. Each compares the count of true operands with an integer `k`. See [Cardinality Functions](strong-k3/cardinality/README.md). |
| **Trace** | An ordered record of every node that an evaluation visited or skipped. It explains why a rule gave its result. |
| **Truth order** | `False < Unknown < True`. It states the connectives as minimum, maximum and reversal. It is not a numeric scale. See [values](strong-k3/specification/values.md#truth-order). |
| **TruthValue** | The result type: `True`, `False` or `Unknown`. It is never `bool?`. |
| **Unknown** | A value in its own right: not established as true or as false. It is not an error and not `null`. See [values](strong-k3/specification/values.md). |
| **Variable reference** | An argument written as `from("source", "query")`. The value comes from a named data source at evaluation. See [data sources](data-sources.md). |
