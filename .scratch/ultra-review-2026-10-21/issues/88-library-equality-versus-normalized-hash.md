# 88 - Two libraries whose texts differ only in line endings have one hash but are not equal

Status: needs-triage
Category: enhancement
Severity: low
Lens: Follow-up to tickets 84 and 85
Blocked by: none

## Finding

Ticket 84 made the content hash read CRLF and lone CR as LF. Ticket 85 gave `RuleLibrary` value equality over names (without case) and the exact rule texts. A library with CRLF text and one with the same text in LF have the same rule hashes but compare unequal, so equal-meaning `CompilerOptions` compare unequal. The agent chose the narrowest behavior.

## Open question for the owner

Should equality follow the normalized text (so it agrees with the hash), or stay exact?

## Next step

Decide; following the normalized text is a one-line change in `RuleLibrary`.
