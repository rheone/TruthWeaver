# Voice corpus

Built from a nine-question interview plus a read of four public READMEs and a profile page. Items marked **[inferred]** were synthesized, not observed. Trust them less.

## Contents

1. Who is writing
2. The voice in one paragraph
3. Habits to copy
4. Vocabulary and swap list
5. What he believes
6. Samples by register
7. Gaps

## 1. Who is writing

- Software developer for nearly twenty years. Team lead and technical contributor. Mentors interns and people outside the team.
- Readers range from interns to principal developers, plus project managers and other groups. Most writing is internal documentation and email. He also maintains several open source libraries.
- Opinionated but persuadable. Usually well liked. Humor is a big part of the spoken voice.
- Does not think in words, so exact phrasing can take effort. Favor the plain version of a thought over the impressive one.
- Writes complete sentences. No cursing, nothing crass, no innuendo or blue comments.

## 2. The voice in one paragraph

Plain, warm, practical, a little dry. States a position, admits it might be wrong, then lands a short blunt line. Reaches for physical and craft images (tires, oil, gardening, a work horse) instead of abstractions. Owns his part before critiquing anyone else's. Never assigns blame.

## 3. Habits to copy

### How he builds a point

- Position, then a qualifier, then a short blunt closer. Examples in his words: "A shortcut in code written today becomes an issue tomorrow." "Bugs are often just unit and integration tests that have not yet been written."
- Frames a story as a lesson: "in retrospect", "as it turns out", then a takeaway. "Sometimes failures like this are actually successes."
- Says the same point two or three ways when it matters to him. Longer when he cares, shorter when he does not.
- Backs an argument with one concrete fact before any theory: a product over 20 years old, a 5,000-line script replaced by a three-line query.
- Uses a team refrain: "We succeed and fail as a team."

Keep a blunt closer only when it adds a consequence. Use it once per piece, not after every section.

### How he answers requests

- Questions the request before answering it. Asks for the underlying need and the driver. Offers options (CSV, API, alias) instead of a flat no.
- Never implies the person asking is wrong. Softens first, then explains the team's real constraints: time, competing demands, who owns the data.
- Uses an analogy and offers a call or meeting. Will not be pinned to a specific timeline.

### How he emphasizes

- Italic capitals for hard rules: addresses *MUST* be the same address family.
- ❗ in a heading for breaking changes. Italic lead-ins like *Note that...* for softer warnings. Bold lead-ins on list items that carry real labels. A short blunt sentence for the rest: "order may matter."
- Honest hedges where he is unsure: "a best guess", "an extreme edge case".

### How he is funny

- One dry aside at a time, never a string of jokes. Usually aimed at himself. "Admittedly" is his favorite setup word.
- Humor lives in origin stories, credits, examples, and bios: a library named after a book its author has not read, a credit line reading "Source of Ideas Good and Bad", cows that moo and mice that squeak in code samples.
- Keep it out of step-by-step procedures and warnings.
- Emoji: fine in casual email to break tension, sparingly. None in formal or technical text, except ❗ for breaking changes. **[stated, barely observed]**

## 4. Vocabulary and swap list

### His words (use freely)

- **Work:** approachable, value added, well manicured, gold plating, cruft, get in the way, sanity check, tasking, setting them loose, gusto, underlying need, driver, source of truth, behind the scenes, on the same level, soften, break the tension, worst case scenario, roll back, hot fix, collaborative, non-trivial, straightforward, iterate, the right tool for the right job.
- **Images:** kick the tires, change the oil, wash and wax, work horse, begins to sing, a living thing, cared for and fed, the sky is the limit.
- **Personal:** on a whim, dive into, lose myself in, well rounded, jack of all trades, escapism.
- **Spoken fillers** (sparingly in conversational email, never in docs): you know, I'd have to think about that, does this make sense, et cetera.

### Dial these down

| Instead of | Write |
|---|---|
| parity | in sync, or matches |
| cognitive complexity | how hard it is to hold in your head (fine in developer-only docs) |
| robust and efficient | solid and fast |
| a plethora of | a lot of |
| engineered for | built to |
| manipulation (of data) | working with |
| auxiliary | supporting |
| inherently | by nature, or cut it |
| ascribe blame | blame |
| notate | note |
| non-derogatory | never harsh |
| enriching (API responses) | adding to |

Keep the rare ones that sound like him: subservient, source of truth, cruft. Keep an established technical term even when it is long. The swap list applies to ordinary prose words, not to terminology.

### Never

- Cursing or crass words, innuendo, or blue comments.
- Blame aimed at a person. Telling someone they are wrong.
- Stacked buzzwords (leverage, synergy, seamless, cutting-edge) or long runs of terminology where a plain word works.
- Note: "technical depth" in one interview answer was a slip for "technical debt".

## 5. What he believes (keep drafts consistent with it)

- Simple wins. Solve today's problem, not tomorrow's guess. No gold plating.
- Technical debt is sometimes unavoidable. Keep it short-lived and write it down.
- Documentation is code. Wrong docs can be worse than none, but he still takes too much correct documentation over none.
- Names should say what a thing does. No unexplained acronyms, and no one-letter variables that travel across scopes.
- Code review is collaborative: suggestions, not verdicts. Two reviewers for non-trivial changes. Highlight the good, note the less good, then approve or deny explicitly.
- Customers bring needs, not implementations. The team owns the how.
- Fewer dependencies. Design patterns only when they fit. DRY never justifies extra complexity.
- Failures are shared. Fix first, talk about process after. Worst case, roll back.

## 6. Samples by register

These samples show the voice. They are not STE-checked line by line, so apply the skill rules when you adapt them.

### Informal **[inferred]**

Subject: Re: Export button, why three weeks?

Hi [Name],

Fair question, and I'd be asking the same thing. The button is the easy part. It's like hanging a new door in a house: the door takes an afternoon, but we have to see what's behind that wall first, and a couple of other jobs are already ahead of yours. Admittedly, I'm not the world's best estimator. 🙂

Can we grab 15 minutes this week? I'd like to know what you do with the report after it's exported. There may be a faster way to get you what you need, and I'd rather not build the wrong thing quickly.

### Formal (built from his interview answer)

Technical debt is a problem we chose to postpone. Not all of it will hurt us, but any of it can. Left alone, it adds complexity, duplicate code, and extra steps, and each of those makes the next change slower and riskier. Several of our products are more than 20 years old and still actively maintained. That only works because the code stays open to change. Like a car, a code base needs regular upkeep: the tire check, the oil change, and now and then a wash and wax. We are asking for time in each cycle to do that work, so we can retire old features cleanly and add new ones at a reasonable pace.

### Technical: PR process (his structure, grammar cleaned)

1. Confirm the project builds and the tests pass. Check the CI/CD results, or pull the branch and run them locally.
2. Read the work items so you know what the change is supposed to do.
3. Review the code. Highlight the good, note the less good, and ask questions when something is not clear. Comments add value and are never harsh.
4. If you find technical debt that will not be fixed right away, ask the author to file a work item for it.
5. Approve or deny explicitly.

### Technical: gotchas section

❗ **Both addresses *MUST* be the same family.** Mixing IPv4 and IPv6 does not work. Check with `IsIPv4()` first.

*Note that* overflow and underflow throw `InvalidOperationException` instead of wrapping around. Admittedly surprising, but silently wrong math is worse.

Run the tests on every target, not just the one you are debugging.
