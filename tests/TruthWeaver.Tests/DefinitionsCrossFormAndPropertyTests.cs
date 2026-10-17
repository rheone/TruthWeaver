namespace TruthWeaver.Tests;

using TruthWeaver.Analysis;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Ticket 11-5 (ADR-0008): the closing cross-form and property tests for <c>let</c> definitions, plus the parent
/// ticket's remaining acceptance item that the BDD analysis, equivalence check, every rewrite and every printer work
/// unchanged on a rule that used definitions.
/// </summary>
public sealed class DefinitionsCrossFormAndPropertyTests
{
    private const int PropertySeeds = 200;
    private static readonly string[] Predicates = ["a", "b", "c", "d"];

    /// <summary>
    /// Rule text, JSON, YAML and <see cref="RuleBuilder"/> all compile a representative set of definitions - a
    /// parameterless definition, a parameterized one, and a definition that uses another one - to the identical
    /// compiled tree (ADR-0008 decisions 2-4).
    /// </summary>
    [Fact]
    public void Compile_RepresentativeDefinitionsAcrossAllFourForms_ProduceTheIdenticalCompiledTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        const string text = """
            let adult = a;
            let bothTrue(x, y) = x AND y;
            let adultAndBoth = adult AND bothTrue(b, c);
            adultAndBoth OR d
            """;
        const string json = """
            {
              "definitions": {
                "adult": { "body": { "predicate": "a" } },
                "bothTrue": {
                  "parameters": ["x", "y"],
                  "body": { "op": "and", "operands": [{ "predicate": "x" }, { "predicate": "y" }] }
                },
                "adultAndBoth": {
                  "body": {
                    "op": "and",
                    "operands": [
                      { "op": "use", "name": "adult" },
                      { "op": "use", "name": "bothTrue", "operands": [{ "predicate": "b" }, { "predicate": "c" }] }
                    ]
                  }
                }
              },
              "op": "or",
              "operands": [{ "op": "use", "name": "adultAndBoth" }, { "predicate": "d" }]
            }
            """;
        const string yaml = """
            definitions:
              adult:
                body:
                  predicate: a
              bothTrue:
                parameters: [x, y]
                body:
                  op: and
                  operands:
                    - predicate: x
                    - predicate: y
              adultAndBoth:
                body:
                  op: and
                  operands:
                    - op: use
                      name: adult
                    - op: use
                      name: bothTrue
                      operands:
                        - predicate: b
                        - predicate: c
            op: or
            operands:
              - op: use
                name: adultAndBoth
              - predicate: d
            """;

        RuleDefinition adult = RuleBuilder.Define("adult", RuleBuilder.Predicate("a"));
        RuleDefinition bothTrue = RuleBuilder.Define(
            "bothTrue",
            ["x", "y"],
            RuleBuilder.And(RuleBuilder.Predicate("x"), RuleBuilder.Predicate("y"))
        );
        RuleDefinition adultAndBoth = RuleBuilder.Define(
            "adultAndBoth",
            RuleBuilder.And(
                RuleBuilder.Use("adult"),
                RuleBuilder.Use("bothTrue", RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"))
            )
        );
        RuleBuilder builder = RuleBuilder
            .Or(RuleBuilder.Use("adultAndBoth"), RuleBuilder.Predicate("d"))
            .WithDefinitions(adult, bothTrue, adultAndBoth);

        CompiledRule<RuleTestContext> fromText = compiler.Compile(text).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(json).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromYaml = compiler.CompileYaml(yaml).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromBuilder = builder.Compile(compiler).GetRuleOrThrow();
        CompiledRule<RuleTestContext> handWritten = compiler.Compile("(a AND (b AND c)) OR d").GetRuleOrThrow();

        Assert.Equal("(a AND (b AND c)) OR d", fromText.CanonicalText);
        Assert.Equal(fromText.CanonicalText, fromJson.CanonicalText);
        Assert.Equal(fromText.CanonicalText, fromYaml.CanonicalText);
        Assert.Equal(fromText.CanonicalText, fromBuilder.CanonicalText);
        Assert.Equal(fromText.CanonicalText, handWritten.CanonicalText);
        Assert.False(RuleDiff.Compare(handWritten, fromText).HasChanges);
        Assert.False(RuleDiff.Compare(handWritten, fromJson).HasChanges);
        Assert.False(RuleDiff.Compare(handWritten, fromYaml).HasChanges);
        Assert.False(RuleDiff.Compare(handWritten, fromBuilder).HasChanges);
    }

    /// <summary>
    /// Property test: for many randomly generated documents of chained, parameterized definitions and uses (no
    /// cycles, so every one compiles), substituting by hand with an expansion model independent of
    /// <c>DefinitionExpander</c> gives the same tree the compiler's own expansion gives, for every seed.
    /// </summary>
    [Fact]
    public void Compile_RandomlyGeneratedDefinitions_MatchHandExpandedModel_ForEverySeed_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        for (int seed = 0; seed < PropertySeeds; seed++)
        {
            Random random = new(seed);
            Document document = NextDocument(random);
            string source = document.ToRuleText();

            CompilationResult<RuleTestContext> result = compiler.Compile(source);
            Assert.True(
                result.Succeeded,
                $"Seed {seed} did not compile: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}\n{source}"
            );

            TestExpr expanded = HandExpand(document.Root, document.DefinitionsByName, bindings: null);
            CompiledRule<RuleTestContext> expected = compiler.Compile(expanded.ToRuleText()).GetRuleOrThrow();

            Assert.Equal(expected.CanonicalText, result.Rule.CanonicalText);
        }
    }

    /// <summary>
    /// The BDD-based equivalence check (<see cref="RuleEquivalence"/>), <see cref="RuleDiff"/>, every rewrite
    /// (<c>ExpandToPrimitives</c>, <c>ExpandToNand</c>, <c>ExpandToNor</c>, <c>CompressToDerived</c>,
    /// <c>Canonicalize</c>, <c>Simplify</c>) and every printer give the identical result for a rule that used
    /// definitions and its hand-written equivalent, confirming ADR-0008 decision 1: none of them needed a
    /// definitions-aware case because the compiled tree never contains one.
    /// </summary>
    [Fact]
    public void Analysis_RewritesAndPrinters_OnARuleThatUsedDefinitions_MatchTheHandWrittenEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string withDefinitions = """
            let adult = a;
            let eitherTrue(x, y) = x OR y;
            NOT (adult AND eitherTrue(b, c)) OR d
            """;
        const string handWrittenText = "NOT (a AND (b OR c)) OR d";

        CompiledRule<RuleTestContext> withDefs = compiler.Compile(withDefinitions).GetRuleOrThrow();
        CompiledRule<RuleTestContext> handWritten = compiler.Compile(handWrittenText).GetRuleOrThrow();

        // BDD-based equivalence check.
        RuleEquivalenceResult equivalence = RuleEquivalence.Compare(withDefs, handWritten);
        Assert.Equal(RuleEquivalenceOutcome.Equivalent, equivalence.Outcome);

        // Diffing (also BDD-backed for the preserves-meaning verdict).
        Assert.False(RuleDiff.Compare(handWritten, withDefs).HasChanges);

        // Every rewrite.
        Assert.Equal(
            handWritten.ExpandToPrimitives().GetRuleOrThrow().CanonicalText,
            withDefs.ExpandToPrimitives().GetRuleOrThrow().CanonicalText
        );
        Assert.Equal(
            handWritten.ExpandToNand().GetRuleOrThrow().CanonicalText,
            withDefs.ExpandToNand().GetRuleOrThrow().CanonicalText
        );
        Assert.Equal(
            handWritten.ExpandToNor().GetRuleOrThrow().CanonicalText,
            withDefs.ExpandToNor().GetRuleOrThrow().CanonicalText
        );
        Assert.Equal(
            handWritten.CompressToDerived().GetRuleOrThrow().CanonicalText,
            withDefs.CompressToDerived().GetRuleOrThrow().CanonicalText
        );
        Assert.Equal(
            handWritten.Canonicalize().GetRuleOrThrow().CanonicalText,
            withDefs.Canonicalize().GetRuleOrThrow().CanonicalText
        );
        Assert.Equal(handWritten.Simplify().GetRuleOrThrow().CanonicalText, withDefs.Simplify().GetRuleOrThrow().CanonicalText);

        // Every printer.
        Assert.Equal(handWritten.PrintRuleText(), withDefs.PrintRuleText());
        Assert.Equal(handWritten.PrintJson(), withDefs.PrintJson());
        Assert.Equal(handWritten.PrintYaml(), withDefs.PrintYaml());
        Assert.Equal(handWritten.CanonicalText, withDefs.CanonicalText);
        Assert.Equal(handWritten.PrintMermaid(), withDefs.PrintMermaid());
        Assert.Equal(handWritten.PrintDot(), withDefs.PrintDot());
        Assert.Equal(handWritten.PrintHtml(), withDefs.PrintHtml());
        Assert.Equal(handWritten.PrintPlainText(), withDefs.PrintPlainText());
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string predicate in Predicates)
        {
            builder.AddConstant(predicate, true);
        }

        return new(builder.Build(), new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096));
    }

    // ---- Random document generation and an expansion model independent of DefinitionExpander ----

    /// <summary>
    /// Expands <paramref name="node"/> by hand: every <see cref="Use"/> is replaced by its definition's body with
    /// its parameters substituted, exactly what ADR-0008 decision 1 asks the compiler to do - implemented here
    /// independently of <c>DefinitionExpander</c>, as the property test's reference model.
    /// </summary>
    private static TestExpr HandExpand(
        TestExpr node,
        IReadOnlyDictionary<string, (IReadOnlyList<string> Parameters, TestExpr Body)> definitions,
        IReadOnlyDictionary<string, TestExpr>? bindings
    )
    {
        return node switch
        {
            Leaf leaf => bindings is not null && bindings.TryGetValue(leaf.Name, out TestExpr? bound) ? bound : leaf,
            Not n => new Not(HandExpand(n.Operand, definitions, bindings)),
            And a => new And(HandExpand(a.Left, definitions, bindings), HandExpand(a.Right, definitions, bindings)),
            Or o => new Or(HandExpand(o.Left, definitions, bindings), HandExpand(o.Right, definitions, bindings)),
            Use use => ExpandUse(use, definitions, bindings),
            _ => throw new InvalidOperationException($"Unhandled TestExpr '{node.GetType()}'."),
        };
    }

    private static TestExpr ExpandUse(
        Use use,
        IReadOnlyDictionary<string, (IReadOnlyList<string> Parameters, TestExpr Body)> definitions,
        IReadOnlyDictionary<string, TestExpr>? bindings
    )
    {
        TestExpr[] arguments = [.. use.Arguments.Select(a => HandExpand(a, definitions, bindings))];
        (IReadOnlyList<string> parameters, TestExpr body) = definitions[use.Name];
        Dictionary<string, TestExpr> newBindings = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            newBindings[parameters[i]] = arguments[i];
        }

        return HandExpand(body, definitions, newBindings);
    }

    /// <summary>
    /// Generates a document of 1-3 definitions, each only able to use an earlier one (so the chain is acyclic by
    /// construction), plus a root expression that uses at least one of them.
    /// </summary>
    private static Document NextDocument(Random random)
    {
        int definitionCount = random.Next(1, 4);
        List<(string Name, IReadOnlyList<string> Parameters, TestExpr Body)> definitions = [];

        for (int i = 0; i < definitionCount; i++)
        {
            string name = $"d{i}";
            List<string> parameters = [.. Enumerable.Range(0, random.Next(0, 3)).Select(p => $"p{i}_{p}")];
            TestExpr body = NextExpr(random, parameters, definitions, depth: 2);
            definitions.Add((name, parameters, body));
        }

        TestExpr root = NextExpr(random, parameters: [], definitions, depth: 2);
        return new Document(definitions, root);
    }

    /// <summary>A random predicate, parameter reference, negation, conjunction, disjunction or use of an earlier definition.</summary>
    private static TestExpr NextExpr(
        Random random,
        IReadOnlyList<string> parameters,
        IReadOnlyList<(string Name, IReadOnlyList<string> Parameters, TestExpr Body)> availableDefinitions,
        int depth
    )
    {
        bool canUse = availableDefinitions.Count > 0;
        int choiceCount = canUse ? 6 : 5;
        int choice = depth <= 0 ? random.Next(2) : random.Next(choiceCount);
        switch (choice)
        {
            case 0:
                return new Leaf(Predicates[random.Next(Predicates.Length)]);
            case 1 when parameters.Count > 0:
                return new Leaf(parameters[random.Next(parameters.Count)]);
            case 2:
                return new Not(NextExpr(random, parameters, availableDefinitions, depth - 1));
            case 3:
                return new And(
                    NextExpr(random, parameters, availableDefinitions, depth - 1),
                    NextExpr(random, parameters, availableDefinitions, depth - 1)
                );
            case 4:
                return new Or(
                    NextExpr(random, parameters, availableDefinitions, depth - 1),
                    NextExpr(random, parameters, availableDefinitions, depth - 1)
                );
            case 5 when canUse:
                (string name, IReadOnlyList<string> targetParameters, _) = availableDefinitions[
                    random.Next(availableDefinitions.Count)
                ];
                TestExpr[] arguments =
                [
                    .. targetParameters.Select(_ => NextExpr(random, parameters, availableDefinitions, depth - 1)),
                ];
                return new Use(name, arguments);
            default:
                return new Leaf(Predicates[random.Next(Predicates.Length)]);
        }
    }

    private sealed record Document(
        IReadOnlyList<(string Name, IReadOnlyList<string> Parameters, TestExpr Body)> Definitions,
        TestExpr Root
    )
    {
        public IReadOnlyDictionary<string, (IReadOnlyList<string> Parameters, TestExpr Body)> DefinitionsByName =>
            this.Definitions.ToDictionary(d => d.Name, d => (d.Parameters, d.Body), StringComparer.Ordinal);

        public string ToRuleText()
        {
            IEnumerable<string> lets = this.Definitions.Select(d =>
                d.Parameters.Count == 0
                    ? $"let {d.Name} = {d.Body.ToRuleText()};"
                    : $"let {d.Name}({string.Join(", ", d.Parameters)}) = {d.Body.ToRuleText()};"
            );
            return string.Join(" ", lets) + " " + this.Root.ToRuleText();
        }
    }

    private abstract record TestExpr
    {
        public abstract string ToRuleText();
    }

    // A predicate reference and a parameter reference render identically in rule text (a bare identifier); which one
    // it is depends only on whether an enclosing definition declared that name as a parameter.
    private sealed record Leaf(string Name) : TestExpr
    {
        public override string ToRuleText()
        {
            return this.Name;
        }
    }

    private sealed record Not(TestExpr Operand) : TestExpr
    {
        public override string ToRuleText()
        {
            return $"NOT ({this.Operand.ToRuleText()})";
        }
    }

    private sealed record And(TestExpr Left, TestExpr Right) : TestExpr
    {
        public override string ToRuleText()
        {
            return $"({this.Left.ToRuleText()} AND {this.Right.ToRuleText()})";
        }
    }

    private sealed record Or(TestExpr Left, TestExpr Right) : TestExpr
    {
        public override string ToRuleText()
        {
            return $"({this.Left.ToRuleText()} OR {this.Right.ToRuleText()})";
        }
    }

    private sealed record Use(string Name, IReadOnlyList<TestExpr> Arguments) : TestExpr
    {
        public override string ToRuleText()
        {
            if (this.Arguments.Count == 0)
            {
                return this.Name;
            }

            return $"{this.Name}({string.Join(", ", this.Arguments.Select(a => a.ToRuleText()))})";
        }
    }
}
