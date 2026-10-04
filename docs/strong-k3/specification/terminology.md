# Terminology

The terms of the Strong Kleene (K3) reference, each with one meaning. Back to the [specification index](README.md).

The reference writes "Strong Kleene (K3)". It never writes "K3" and "Strong K3" as two different systems.

## Classifying an Operation

| Term | Meaning |
| --- | --- |
| **Operation** | Anything with its own document: an operator, a cardinality function, a function or a result transformation. |
| **Primary category** | The one grouping that an Operation belongs to, and the directory that holds its document. The categories are Gates / Operators, Derived Logical Operations, Cardinality Functions, Functions and Result Transformations. Category is the topic. It is independent of Kind. |
| **Kind** | `Primitive` or `Derived`. Every Operation document states it. Kind is independent of category: the cardinality and function categories hold both. |
| **Primitive** | An Operation that has no definition in terms of other Operations. There are seven: `NOT`, `AND`, `OR`, `AtLeast`, `AtMost`, `Exactly` and `COALESCE`. `Collapse` is also recorded as Primitive, because nothing in the rule language expresses it. |
| **Derived** | An Operation that has a definition in terms of primitives. A derived Operation stays a node of its own. It keeps its own evaluation, printing and round trip. |
| **Strong Kleene connective** | An Operation that is monotone in the [information order](values.md#information-order). The list is in [semantics](semantics.md#strong-kleene-connectives-and-external-operators). |
| **External operator** | An Operation that is not monotone in the information order, so it is not a Strong Kleene connective. These are `COALESCE` and the inspections `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`. Every such document says so. |
| **Gate** | The logical concept: `NOT`, `AND` and `OR` as Strong Kleene truth functions. The Gates / Operators category holds them. An **Operator** is the programmatic form of a gate: the DSL word, the tree node, the JSON `op` value or the `RuleBuilder` member. The engine and its API say Operator. |

## Forms of an Operation

An Operation can be written in more than one way. The reference separates two senses of "form".

| Term | Meaning |
| --- | --- |
| **Canonical form** | For a derived Operation, its definition in terms of primitives, written as a function-call expression such as `OR(NOT(a), b)` for `IMPLIES`. A canonical form is listed only where one is established. It holds for every assignment of `T`, `F` and `U` and, for variadic Operations, for every operand count and valid parameter. Where none is established, the document says so. It is not the output of the engine's `Canonicalize()` rewrite, which reorders and flattens a rule, and it is not the canonical text that the printer writes. |
| **Public form** | The spelling that a rule author or API consumer uses to name an Operation: the printed word (`AND`, `AtLeast`, `If`), the JSON and YAML `op` value (`and`, `atLeast`) and the `RuleBuilder` member. Every other accepted spelling, such as the symbol `&&` or the word `IFF`, is an alias. It compiles to the same node. A removed name is not an alias. |

Canonical form is how an Operation is defined. Public form is how it is named. A derived Operation keeps its public form on a round trip, although it has a canonical form.

## Evaluation terms

| Term | Meaning |
| --- | --- |
| **Truth order** | $\mathsf{F} < \mathsf{U} < \mathsf{T}$. An implementation aid. See [values](values.md#truth-order). |
| **Information order** | $\mathsf{U}$ below $\mathsf{T}$ and $\mathsf{F}$, which are incomparable. See [values](values.md#information-order). |
| **Strongest extension** | The three-valued function that is definite exactly when every classical resolution of the `Unknown` inputs agrees. See [semantics](semantics.md#truth-functional-evaluation-and-the-strongest-extension). |
| **Definitely true count, possibly true count** | For a cardinality Operation over $n$ operands, $d$ is the number of `True` operands and $p$ is the number of `True` or `Unknown` operands. See [semantics](semantics.md#cardinality-uses-an-interval). |
| **Fault** | A predicate that fails to answer during one evaluation: an exception, a timeout or a cancellation. The expression sees `Unknown`, and the decision records the fault. A predicate that returns `Unknown` records no fault. |

## Engine terms

| Term | Meaning |
| --- | --- |
| **Expression** | The immutable tree of operators over terms and constants. Every expression evaluates to exactly one value. |
| **Predicate** and **term** | A predicate is a registered, reusable function. A term is a predicate bound to concrete arguments. A term is a leaf of the tree. The two are not interchangeable. |
| **Decision** | The result of evaluating an expression: a value, any faults and, optionally, a trace. |
| **Result transformation** | A method on a `Decision`, outside the rule language. `Decision.Project(unknownAs)` replaces `Unknown` with a chosen definite value. `Decision.Collapse(policy)` turns the value into a `CollapseOutcome`. `Project` and `Collapse` are TruthWeaver terms, not Strong Kleene (K3) terms. In relational algebra, "projection" means selecting columns. Both are pure and never record a fault. |
| **Rewrite** | An opt-in, value-preserving transform of a compiled rule that returns a new rule: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize` and `Simplify`. |

## Related

- [values](values.md), [semantics](semantics.md) and [notation](notation.md).
- [operations](operations.md) lists every Operation with its category, Kind and canonical form.
