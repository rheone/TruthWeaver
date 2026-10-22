# STE rules and AI-tell cleanup: detail and examples

Use this file when you need the reasoning behind a rule or a worked before/after. The short version lives in SKILL.md.

## Contents

1. Why two layers
2. STE rules with examples
3. Current-state writing
4. Direct statements instead of tentative ones
5. Procedures
6. Progressive disclosure examples
7. AI tells with examples
8. Final self-check

## 1. Why two layers

STE gives the text one meaning per sentence, so a beginner and a principal developer read the same thing. Voice keeps the text from sounding like a manual written by nobody. Without STE, the voice drifts into long, hedged sentences. Without voice, the text turns flat and generic, which is exactly what readers recognize as machine writing.

This skill adapts the ASD-STE100 principles (US). It does not check text against the full STE dictionary. If a customer needs formal STE compliance, use the specification and a checker.

## 2. STE rules with examples

**One meaning per sentence.**
Before: The method parses the string and, if it is valid, returns the subnet, otherwise it returns null, and logs a warning.
After: The method parses the string. If the string is valid, the method returns the subnet. If the string is not valid, the method returns null.

(The logging claim stays out unless the source confirms it. Never add or keep an unverified behavior.)

**Name the actor.**
Before: Overflow conditions will be handled by throwing.
After: The method throws `InvalidOperationException` on overflow and underflow.

**Keep articles.**
Before: Pass address to constructor.
After: Pass the address to the constructor.

**Repeat the noun when a pronoun is unclear.**
Before: The comparer takes a range and a subnet, and it converts it to bytes.
After: The comparer takes a range and a subnet. The comparer converts the subnet to bytes.

**One term per concept.**
Before: Create a subnet from two addresses. The network range holds both. The CIDR block starts at the lower address.
After: Create a subnet from two addresses. The subnet holds both addresses. The subnet starts at the lower address.

(If "CIDR block" is the established term for a different thing, keep it. Established terms stay.)

**Limit noun clusters to three nouns.**
Before: The subnet range comparison method null handling behavior
After: How the method that compares subnet ranges handles null

**Plain verbs.**
utilize → use. initiate → start. demonstrate → show. leverage → use. facilitate → help, or name the action. serves as → is. boasts → has. in order to → to.

**Modals.**
Use *must* for a requirement. Use *can* for ability. Cut *may*, *could*, and *should* unless the point is truly conditional.
Before: Addresses should be the same family.
After: Addresses *MUST* be the same family.
Before: The call could throw if the input is empty.
After: The call throws `ArgumentException` if the input is empty. (Use this only if the source confirms the behavior.)
Conditional that stays: "You can disable the hook in CI by setting `HUSKY` to `0`."

**Sentence length.**
Procedure sentences: 20 words or fewer. Descriptive sentences: 25 words or fewer. A long sentence usually holds two meanings. Split it.

## 3. Current-state writing

Describe what the system does today. A reader of reference documentation needs the present behavior, not the history.

Before: The library now supports .NET 10. Previously it targeted only .NET Standard 1.3.
After: The library targets .NET Standard 2.0, .NET 8, .NET 9, and .NET 10.

Before: A new `ParseFromHexString` method was added to handle hex input.
After: `ParseFromHexString` parses an IP address from a hexadecimal string.

Words that signal a delta: now, new, newly, previously, no longer, used to, was added, was changed, will, soon, upcoming, currently (when it hides a plan). Keep them only in change logs, release notes, and migration guides.

A breaking-change section is allowed because upgraders need it. Write it as current behavior by version: "In .NET Standard 2.0 and .NET 4.8, the input `abcd::%` fails to parse. In .NET 8 and later, the parser ignores the trailing `%`."

## 4. Direct statements instead of tentative ones

Established behavior gets a direct statement.
Before: It is suggested that IP address based user input should be sanitized to meet your development needs.
After: Sanitize IP address input before you parse it.

Before: The comparison may not be done in mathematical order.
After: `CompareTo` compares by byte length first, then by unsigned value.

Honest uncertainty stays, in plain words. His own READMEs do this well: "the type is a best guess based on hints and should not be interpreted as definitive." Keep a hedge when the behavior really is a guess, and say what it is a guess about.

## 5. Procedures

- Put the warning before the step it protects.
- One action per step. Start the step with a verb.
- State the expected result when a reader could be unsure it worked.
- Number the steps when the order matters. Use bullets when it does not.
- Keep conditions at the front of the step: "If the build fails, run `dotnet restore`."

Example:

1. Run `dotnet format style; dotnet format analyzers; dotnet csharpier format .` from the `src` directory.
2. Run the tests on every target.

*Note that* the three format commands can run on their own, but order can matter.

## 6. Progressive disclosure examples

A README opening that follows the order in SKILL.md:

> # Gulliver
> Gulliver is a C# library for working with byte arrays of any size. It handles endianness and arrays of uneven length.
>
> Gulliver is named after the traveler in Swift's book about big-endian and little-endian eggs, which its author has admittedly not yet read.
>
> ## Getting started
> Install the package from NuGet. Then call `ByteArrayUtils` for the common operations.

(This example restates facts already in his public README. When you write for a real project, take the facts from the project, not from this file.)

The level order for a document of any size: definition, background, smallest working example, core concepts from simple to advanced, procedures, reference, gotchas, developer notes, credits and license. Do not put an exhaustive option table before the first working example. Do not bury a breaking change at the bottom when it affects upgrades.

## 7. AI tells with examples

Based on the patterns in blader/humanizer (itself drawn from Wikipedia's "Signs of AI writing"). The examples below are new and written for this skill.

**Not X but Y**
Before: This is not just a parser. It is a complete address toolkit.
After: The library parses, formats, converts, and compares IPv4 and IPv6 addresses and subnets.

**One-line closer**
Before: Tests run on every target.
That is what keeps us safe.
After: Tests run on every target.

**Staged opener**
Before: Let's dive into how the pre-commit hook works. Here's what you need to know.
After: The pre-commit hook formats and lints the code before each commit.

**Arguing with no one**
Before: To be clear, this is not about speed. A tempting approach would be to cache everything.
After: The cache stores only the parsed results.

**Forced triad**
Before: The library is fast, flexible, and reliable.
After: The library parses a subnet in one call. (Cite a measured number only if the source gives one.)

**Inflated significance and sales language**
Before: This robust library stands as a testament to careful engineering.
After: The library has no runtime dependencies on .NET 8 and later.

**Avoiding "is"**
Before: The `Duid` type serves as the main entry point and boasts several parsing options.
After: The `Duid` type is the main entry point. It has two parsing methods, `Parse` and `TryParse`.

**Bold decoration**
Before: - **Speed:** Speed is improved. - **Safety:** Safety is improved.
After: Parsing is faster, and invalid input returns false instead of throwing. (State only what the source supports.)

**Chat leftover**
Before: Here is the updated README! Let me know if you want changes.
After: (Return only the README.)

**Describing the document**
Before: The table below lists the options, organized by name.
After: (Show the table. The reader can see its order.)

**Re-explaining to a reader who knows**
Before: (A five-paragraph reply that restates the problem, the diagnosis, and the evidence before the answer.)
After: The decision first, one fact the reader lacks, and a link if they need one.

His habits that look like tells but stay: the italic *MUST*, real bold labels in a reference list, an "admittedly" aside, and a team refrain used on purpose.

## 8. Final self-check

- Every claim traces to the source or the user. Nothing invented.
- Established terms and proper nouns are untouched.
- Each sentence has one meaning and an explicit actor.
- One term per concept, repeated.
- No delta words outside change logs.
- No stray hedges ("may", "could", "should") on established behavior.
- No em dashes. No chat leftovers. No staged openers or closers.
- Warnings come before their steps.
- The structure moves from overview to specifics.
- At most one dry aside, and none in procedures or warnings.
- Nothing crass, no innuendo, no blame.
- No curly quotes `“` `”` use `"`
- No `…` use `...`
- No em dashes `—`
