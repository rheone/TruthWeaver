# What would change a decision

`RuleCounterfactual` answers the question "what would make this rule `True`?" for the current facts. It returns the smallest sets of term changes that give the rule a target value. For a denied decision, the result is the "what to fix" list for a user interface. Back to the [README](../README.md).

The search is exact: it checks every value of every term, as the [rule equivalence](rewriting-rules.md#rule-equivalence) check does. It calls no predicate.

## Find the changes

Give the rule and the current facts. The facts are a dictionary from the printed term (such as `hasRole(role: "admin")`) to its value, or a `Decision` of the rule:

```csharp
CounterfactualResult result = RuleCounterfactual.FindChanges(rule, decision);

if (result.TryGetChangeSets(out EquatableArray<ChangeSet> changeSets))
{
    foreach (ChangeSet set in changeSets)
    {
        // For example: "a: False -> True" and "b: (not read) -> True".
        Console.WriteLine(string.Join(", ", set.Changes.Select(c => $"{c.Term}: {c.From?.ToString() ?? "(not read)"} -> {c.To}")));
    }
}
```

`TryGetChangeSets` returns `true` only when `Outcome` is `Found`.

A decision gives the value of each term that it evaluated. A term that faulted is `Unknown`, as the decision recorded it. A decision without a trace tree, or with a trace tree that does not match the rule (another operator, or a term with other literal arguments), throws `ArgumentException`.

## How a change works

- A change sets one term to `True`, `False` or `Unknown`. `Unknown` is a value that a change can set: `IsUnknown(a)` becomes `True` when `a` is set to `Unknown`.
- Every term that a change set does not name keeps its fact.
- A term without a fact can have any value. Such a term is missing from the dictionary, or the decision did not evaluate it (short-circuiting skipped it). A change set that does not name the term gives the target value for each of its three values. When the target depends on the term, the change set names it, and `TermChange.From` is `null`.
- A fixed term is never changed. Give the fixed terms in `CounterfactualOptions.FixedTerms`, for facts that the user cannot change.
- Each change set is minimal: no smaller part of it gives the target value. The sets come fewest changes first.

Terms are opaque and independent, as for [rule equivalence](rewriting-rules.md#rule-equivalence). The search cannot know that two different predicates are related, so a change set can name values that cannot occur together in the application.

A key of the facts dictionary or a fixed term that is not a term of the rule throws `ArgumentException`, so a misspelled fixed term cannot let the search change the real one.

## Options

| `CounterfactualOptions` member | Default | Content |
| --- | --- | --- |
| `Target` | `True` | The value the rule must take. `False` answers "what would deny this decision?". |
| `FixedTerms` | `null` | The printed terms that the search must not change. |
| `MaxChangeSets` | 10 (`RuleCounterfactual.DefaultMaxChangeSets`) | The most change sets to return, at least 1. |
| `MaxAnalysisTerms` | 20 (`CompilerOptions.MaxAnalysisTerms`) | The most distinct terms the search decides. |

## The result

| `Outcome` | Meaning | `ChangeSets` |
| --- | --- | --- |
| `Found` | At least one set of allowed changes gives the target value. | The minimal change sets, fewest changes first. |
| `NoChangeNeeded` | The facts already give the target value. | Empty. |
| `NonePossible` | No set of changes to the terms that are not fixed gives the target value. | Empty. |
| `Undecided` | The rule has more distinct terms than `MaxAnalysisTerms`. `Reason` says why. | Empty. |

A `ChangeSet` has `Changes`, one `TermChange` (`Term`, `From`, `To`) for each changed term in the order of the first occurrence of the term in the rule, and `Count`.

`IsComplete` is `true` when `ChangeSets` holds every minimal change set. It is `false` when the search stopped at `MaxChangeSets`, or at its work limit of 100,000 search steps. The sets returned are then still the smallest, in order, and a `Found` result always holds at least one set.

## Example

The rule `(b AND c) OR a` with every term `False` has two minimal change sets:

| Rank | Changes |
| --- | --- |
| 1 | `a`: `False` to `True` |
| 2 | `b`: `False` to `True`, `c`: `False` to `True` |

With `a` fixed, only the second set is returned. For the rule `a AND b` with `a` fixed at `False`, the outcome is `NonePossible`.
