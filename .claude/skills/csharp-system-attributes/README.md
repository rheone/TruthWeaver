# C# System Attributes

Proactive guidance on which `System.*`/BCL attribute to reach for while writing or reviewing C#
code: debugger presentation, API lifecycle, nullable-flow analysis, caller info, performance,
custom attribute or enum authoring, embedded-language string hints, and trimming/Native AOT
annotations, organized by the situation you're in rather than by C# version. Does not cover
P/Invoke or marshaling attributes, or attributes specific to ASP.NET Core, EF Core,
`System.Text.Json`, or test frameworks.

## When to reach for it

- A type is noisy or unhelpful to inspect in the debugger and needs `DebuggerDisplay`
- Deprecating a member, gating debug-only code, tidying IntelliSense visibility, or shipping a
  pre-stable preview API
- Writing a `TryXxx` method or throw-helper and want the nullable analyzer to understand it
- Writing a `CallerMemberName`/`CallerArgumentExpression` parameter for logging or guard clauses
- Authoring a custom attribute class or a `[Flags]` enum
- Writing a parameter/property that holds a regex, JSON, or other embedded-language string
- Writing reflection-based code that needs to survive trimming or Native AOT publishing

## Using it

This skill is model-invoked: it fires automatically when you're writing a throw-helper, a
`TryParse`-style method, a custom attribute, a flags enum, or deciding which attribute belongs on a
type or member.

## What it covers

| Topic | Reference |
| --- | --- |
| Debugger presentation attributes | [references/debugging-diagnostics.md](references/debugging-diagnostics.md) |
| API lifecycle attributes (`Obsolete`, `Conditional`, `EditorBrowsable`, `Experimental`) | [references/api-lifecycle-contracts.md](references/api-lifecycle-contracts.md) |
| Nullable-flow analysis attributes | [references/nullable-flow-analysis.md](references/nullable-flow-analysis.md) |
| Caller info attributes | [references/caller-info.md](references/caller-info.md) |
| Performance attributes (`MethodImpl`, `SkipLocalsInit`) | [references/performance.md](references/performance.md) |
| Custom attribute and enum authoring (`AttributeUsage`, `Flags`) | [references/attribute-authoring-meta.md](references/attribute-authoring-meta.md) |
| Analyzer/trim suppression attributes | [references/analyzer-suppression.md](references/analyzer-suppression.md) |
| Embedded-language string hints (`StringSyntax`) | [references/string-syntax.md](references/string-syntax.md) |
| Trimming & Native AOT annotations (`DynamicallyAccessedMembers`, `RequiresUnreferencedCode`, `RequiresDynamicCode`, `RequiresAssemblyFiles`) | [references/trimming-and-aot.md](references/trimming-and-aot.md) |
| Testing attribute-driven behavior | [references/testing.md](references/testing.md) |

## Example prompts

- "Add a `DebuggerDisplay` to this type so it's readable while debugging."
- "Make this `TryGetValue` method's nullable annotations tell the compiler `value` is non-null when it returns true."
- "Should I mark this old method `Obsolete` or just hide it with `EditorBrowsable`?"
- "Add `StringSyntax` to this method so the regex pattern gets highlighted in the IDE."
- "This reflection-based factory method breaks under Native AOT — what attribute tells the trimmer what it needs?"
