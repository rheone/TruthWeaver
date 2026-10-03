# Cardinality Functions

Operations over the count of true operands. `AtLeast`, `AtMost` and `Exactly` are primitive; the rest are derived from them. Back to the [reference](../README.md).

| Operation | Kind | Summary |
| --- | --- | --- |
| AtLeast | Primitive | At least `k` operands are true (pending) |
| AtMost | Primitive | At most `k` operands are true (pending) |
| Exactly | Primitive | Exactly `k` operands are true (pending) |
| ExactlyOne | Derived | Exactly one operand is true (pending) |
| GreaterThan | Derived | More than `k` operands are true (pending) |
| LessThan | Derived | Fewer than `k` operands are true (pending) |
| ANY | Derived | At least one operand is true (pending) |
| ALL | Derived | Every operand is true (pending) |
| NONE | Derived | No operand is true (pending) |
| BETWEEN | Derived | The true count lies within `min` and `max` (pending) |
