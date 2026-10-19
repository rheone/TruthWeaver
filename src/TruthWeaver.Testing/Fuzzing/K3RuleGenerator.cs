namespace TruthWeaver.Testing.Fuzzing;

/// <summary>
/// Generates random rule text over a list of terms and the three constants. Each rule carries an expected evaluation that
/// <see cref="K3Oracle"/> builds, so the expectation shares no code with the engine. The generator covers every rule
/// operator.
/// </summary>
/// <remarks>
/// The output depends only on the state of the <see cref="Random"/> and the arguments, so the same seed gives the same
/// rules. Some rules do not compile, for example a threshold that the compiler rejects as out of range. Skip such a rule.
/// </remarks>
public static class K3RuleGenerator
{
    private static readonly (string Name, Func<int, int, bool> Satisfies)[] Thresholds =
    [
        ("AtLeast", (count, k) => count >= k),
        ("AtMost", (count, k) => count <= k),
        ("GreaterThan", (count, k) => count > k),
        ("LessThan", (count, k) => count < k),
        ("Exactly", (count, k) => count == k),
    ];

    private static readonly (string Name, Func<TruthValue, TruthValue> Inspect)[] Inspections =
    [
        ("IsTrue", K3Oracle.IsTrue),
        ("IsFalse", K3Oracle.IsFalse),
        ("IsUnknown", K3Oracle.IsUnknown),
        ("IsKnown", K3Oracle.IsKnown),
    ];

    /// <summary>Enumerates <paramref name="rule"/> and every descendant, parents before children.</summary>
    /// <param name="rule">The root rule.</param>
    /// <returns>All sub-rules.</returns>
    public static IEnumerable<GeneratedRule> Subtrees(GeneratedRule rule)
    {
        yield return rule;
        foreach (GeneratedRule child in rule.Children)
        {
            foreach (GeneratedRule descendant in Subtrees(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Generates a random rule over <paramref name="terms"/> and the three constants, together with an evaluation built
    /// only from <see cref="K3Oracle"/>. Compound nodes are always in parentheses, so the no-implicit-mixing rule cannot
    /// reject them.
    /// </summary>
    /// <param name="random">The seeded random source.</param>
    /// <param name="depth">The maximum nesting depth. Zero gives a single term or constant.</param>
    /// <param name="terms">
    /// The rule text of each term, for example <c>a</c> or <c>hasRole(role: "admin")</c>. The term at index <c>i</c> reads
    /// entry <c>i</c> of the assignment that <see cref="GeneratedRule.Eval"/> receives.
    /// </param>
    /// <returns>The generated rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="terms"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="terms"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="depth"/> is negative.</exception>
    public static GeneratedRule GenerateRule(Random random, int depth, IReadOnlyList<string> terms)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        if (terms.Count == 0)
        {
            throw new ArgumentException("At least one term is necessary.", nameof(terms));
        }

        return Generate(random, depth, terms, RuleOperatorFamilies.All);
    }

    /// <summary>
    /// Generates a random rule like <see cref="GenerateRule(Random, int, IReadOnlyList{string})"/>, but with operators only
    /// from the chosen families. A rule that has no family gets only terms and constants.
    /// </summary>
    /// <param name="random">The seeded random source.</param>
    /// <param name="depth">The maximum nesting depth. Zero gives a single term or constant.</param>
    /// <param name="terms">The rule text of each term; see the other overload.</param>
    /// <param name="families">The operator families the rule may use.</param>
    /// <returns>The generated rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="terms"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="terms"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="depth"/> is negative.</exception>
    public static GeneratedRule GenerateRule(
        Random random,
        int depth,
        IReadOnlyList<string> terms,
        RuleOperatorFamilies families
    )
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        if (terms.Count == 0)
        {
            throw new ArgumentException("At least one term is necessary.", nameof(terms));
        }

        return Generate(random, depth, terms, families);
    }

    // The case numbers of the switch in Generate, by family. Cases 7 and 8 fall to the default branch (the thresholds).
    private static int[] CasesOf(RuleOperatorFamilies families)
    {
        List<int> cases = [];
        if (families.HasFlag(RuleOperatorFamilies.Connectives))
        {
            cases.AddRange([0, 1, 2, 3, 4, 5, 9, 10, 11]);
        }

        if (families.HasFlag(RuleOperatorFamilies.Cardinality))
        {
            cases.AddRange([6, 7, 8, 12, 13, 14, 15]);
        }

        if (families.HasFlag(RuleOperatorFamilies.Conditional))
        {
            cases.Add(17);
        }

        if (families.HasFlag(RuleOperatorFamilies.Inspection))
        {
            cases.AddRange([16, 18, 19]);
        }

        return [.. cases];
    }

    private static GeneratedRule Generate(Random random, int depth, IReadOnlyList<string> terms, RuleOperatorFamilies families)
    {
        int[] allowed = families == RuleOperatorFamilies.All ? [] : CasesOf(families);
        if (depth == 0 || random.Next(5) == 0 || (allowed.Length == 0 && families != RuleOperatorFamilies.All))
        {
            return GenerateLeaf(random, terms);
        }

        GeneratedRule Child()
        {
            return Generate(random, depth - 1, terms, families);
        }

        // The full set keeps the original draw, so the rules of a seed do not change.
        switch (families == RuleOperatorFamilies.All ? random.Next(20) : allowed[random.Next(allowed.Length)])
        {
            case 0:
                GeneratedRule operand = Child();
                return new GeneratedRule($"NOT ({operand.Text})", v => K3Oracle.Not(operand.Eval(v)), [operand]);
            case 1:
                GeneratedRule[] ands = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"({string.Join(" AND ", ands.Select(o => o.Text))})",
                    v => K3Oracle.And(ands.Select(o => o.Eval(v))),
                    ands
                );
            case 2:
                GeneratedRule[] ors = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"({string.Join(" OR ", ors.Select(o => o.Text))})",
                    v => K3Oracle.Or(ors.Select(o => o.Eval(v))),
                    ors
                );
            case 3:
                GeneratedRule xl = Child();
                GeneratedRule xr = Child();
                return new GeneratedRule($"({xl.Text} XOR {xr.Text})", v => K3Oracle.Xor(xl.Eval(v), xr.Eval(v)), [xl, xr]);
            case 4:
                GeneratedRule el = Child();
                GeneratedRule er = Child();
                return new GeneratedRule(
                    $"({el.Text} EQUIVALENT {er.Text})",
                    v => K3Oracle.Equivalent(el.Eval(v), er.Eval(v)),
                    [el, er]
                );
            case 5:
                GeneratedRule il = Child();
                GeneratedRule ir = Child();
                return new GeneratedRule(
                    $"({il.Text} IMPLIES {ir.Text})",
                    v => K3Oracle.Implies(il.Eval(v), ir.Eval(v)),
                    [il, ir]
                );
            case 9:
                GeneratedRule nl = Child();
                GeneratedRule nr = Child();
                return new GeneratedRule($"({nl.Text} NAND {nr.Text})", v => K3Oracle.Nand(nl.Eval(v), nr.Eval(v)), [nl, nr]);
            case 10:
                GeneratedRule rl = Child();
                GeneratedRule rr = Child();
                return new GeneratedRule($"({rl.Text} NOR {rr.Text})", v => K3Oracle.Nor(rl.Eval(v), rr.Eval(v)), [rl, rr]);
            case 11:
                GeneratedRule[] parity = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"PARITY({string.Join(", ", parity.Select(o => o.Text))})",
                    v => K3Oracle.Parity([.. parity.Select(o => o.Eval(v))]),
                    parity
                );
            case 12:
                GeneratedRule[] anys = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ANY({string.Join(", ", anys.Select(o => o.Text))})",
                    v => K3Oracle.Any([.. anys.Select(o => o.Eval(v))]),
                    anys
                );
            case 13:
                GeneratedRule[] alls = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ALL({string.Join(", ", alls.Select(o => o.Text))})",
                    v => K3Oracle.All([.. alls.Select(o => o.Eval(v))]),
                    alls
                );
            case 14:
                GeneratedRule[] nones = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"NONE({string.Join(", ", nones.Select(o => o.Text))})",
                    v => K3Oracle.None([.. nones.Select(o => o.Eval(v))]),
                    nones
                );
            case 15:
                GeneratedRule[] betweenOperands = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                int min = random.Next(0, betweenOperands.Length + 1);
                int max = random.Next(min, betweenOperands.Length + 1);
                if (min == 0 && max == betweenOperands.Length)
                {
                    min = 1; // The full range is rejected as an always-true structural constant.
                }

                return new GeneratedRule(
                    $"BETWEEN({min}, {max}, {string.Join(", ", betweenOperands.Select(o => o.Text))})",
                    v => K3Oracle.Between(min, max, [.. betweenOperands.Select(o => o.Eval(v))]),
                    betweenOperands
                );
            case 16:
                GeneratedRule[] coalesced = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"COALESCE({string.Join(", ", coalesced.Select(o => o.Text))})",
                    v => K3Oracle.Coalesce([.. coalesced.Select(o => o.Eval(v))]),
                    coalesced
                );
            case 19:
                GeneratedRule projected = Child();
                bool unknownAs = random.Next(2) == 0;
                TruthValue replacement = unknownAs ? TruthValue.True : TruthValue.False;
                return new GeneratedRule(
                    $"COALESCE({projected.Text}, {replacement})",
                    v => K3Oracle.Project(projected.Eval(v), replacement),
                    [projected]
                );
            case 18:
                GeneratedRule inspected = Child();
                (string inspection, Func<TruthValue, TruthValue> inspect) = Inspections[random.Next(Inspections.Length)];
                return new GeneratedRule($"{inspection}({inspected.Text})", v => inspect(inspected.Eval(v)), [inspected]);
            case 17:
                GeneratedRule condition = Child();
                GeneratedRule whenTrue = Child();
                GeneratedRule whenFalse = Child();
                return new GeneratedRule(
                    $"If({condition.Text}, {whenTrue.Text}, {whenFalse.Text})",
                    v => K3Oracle.If(condition.Eval(v), whenTrue.Eval(v), whenFalse.Eval(v)),
                    [condition, whenTrue, whenFalse]
                );
            case 6:
                GeneratedRule[] exactlyOne = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ExactlyOne({string.Join(", ", exactlyOne.Select(o => o.Text))})",
                    v => K3Oracle.ExactlyOne([.. exactlyOne.Select(o => o.Eval(v))]),
                    exactlyOne
                );
            default:
                (string name, Func<int, int, bool> satisfies) = Thresholds[random.Next(Thresholds.Length)];
                GeneratedRule[] operands = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                int k = random.Next(0, operands.Length + 1);
                return new GeneratedRule(
                    $"{name}({k}, {string.Join(", ", operands.Select(o => o.Text))})",
                    v => K3Oracle.Cardinality(count => satisfies(count, k), [.. operands.Select(o => o.Eval(v))]),
                    operands
                );
        }
    }

    private static GeneratedRule GenerateLeaf(Random random, IReadOnlyList<string> terms)
    {
        switch (random.Next(6))
        {
            case 0:
                return new GeneratedRule("TRUE", _ => TruthValue.True, []);
            case 1:
                return new GeneratedRule("FALSE", _ => TruthValue.False, []);
            case 2:
                return new GeneratedRule("UNKNOWN", _ => TruthValue.Unknown, []);
            default:
                int index = random.Next(terms.Count);
                return new GeneratedRule(terms[index], v => v[index], []);
        }
    }
}
