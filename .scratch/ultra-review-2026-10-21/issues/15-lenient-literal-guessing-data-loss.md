# 15 - Lenient literal guessing silently changes or drops argument values (data loss on tool conversion)

Status: needs-triage
Severity: medium
Lens: F3 Compilation
Verdict: CONFIRMED
Blocked by: none

## Finding F3.3: Lenient literal guessing silently changes or drops argument values (data loss on tool conversion)

Location: `src/TruthWeaver/Compilation/LiteralConversion.cs:80` (CONFIRMED, medium)

Guess, which builds the TermIdentity of an unregistered predicate in Lenient mode, ignores the TryParse results. A number without '.' that does not fit in a long (exponent form or too large) becomes Int64 0. A decimal that fails to parse becomes 0. A mixed-kind array is coerced: a later element of a different kind is turned into a string, and when OfArray then rejects the mixed array, the catch at 164-171 returns an EMPTY string array. docs/tool-core.md:105-107 says the Lenient tool compiler 'accepts every argument as written' and that conversion keeps the tree. Printers only see the compiled TermIdentity, so the original value is lost. Different terms can also collapse to the same identity, which misleads the analyzer.

### Failure scenario

The tool converts JSON `{"predicate":"limit","args":{"max":1e5}}` to rule text as `limit(max: 0)`. `{"args":{"id":99999999999999999999}}` becomes `id: 0`. `levels: [1, 2.5]` becomes `levels: []`. `tags: ["a", 1]` becomes `["a", "1"]`. `foo(n: 1e5) AND NOT foo(n: 0)` is analyzed as `t AND NOT t`, which gives a spurious contradiction warning.

### Expected behavior

In Lenient mode, keep the literal exactly as written: keep the text of a number that has no exact numeric kind, keep each element's kind, or report a diagnostic. Never fall back silently to 0 or to an empty array.

### Verification

I traced the code and did not run it. LiteralConversion.Guess (src/TruthWeaver/Compilation/LiteralConversion.cs:80-89) discards the results of decimal.TryParse and long.TryParse, so a failed parse leaves the default 0. A number text with no '.' that is not a valid long (JSON raw text "1e5" or "99999999999999999999") becomes OfInt64(0). Raw number text really does reach Guess: JsonNodeCursor.cs:125 uses element.GetRawText(), YamlNodeCursor.cs:145 and DslParser.cs:907 pass the text through, and RuleNodeCompiler.cs:169 calls Guess for every argument of an unregistered predicate in Lenient mode to build the TermIdentity. GuessArray (lines 149-172) coerces any element whose kind differs from the first element to OfString(elements[i].Text ?? ""). For [1, 2.5], the first element is Int64 and the second becomes the string "2.5". LiteralValue.OfArray (src/TruthWeaver.Abstractions/LiteralValue.cs:126-139) throws ArgumentException on the kind mismatch. The catch at lines 168-171 then returns an empty StringArray. For ["a", 1], the 1 becomes the string "1" and the kind changes silently. Nested arrays have no Text, so they become "". docs/tool-core.md:105 says the lenient compiler "accepts every argument as written", which these paths contradict. The collapse of different terms into one identity follows directly. Both foo(n: 1e5) and foo(n: 0) get Int64 0, so the identities are equal and the analyzer sees the same term. I did not trace the exact printer output of the converter or the analyzer's contradiction warning. Impact is limited to unregistered predicates in Lenient mode, which is the tool path. Registered predicates use TryConvert, which reports a diagnostic. Real silent data loss and false analyzer results happen only for unusual literals: exponent form, a number beyond long range, or mixed-kind arrays.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
