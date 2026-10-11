using System.Runtime.CompilerServices;

// TruthWeaver.Testing.Tests replaces the Simplify step of RuleFuzzer through an internal overload. The engine has no
// known defect for a public test to find, so a broken rewrite is the only way to prove that the fuzzer reports a failure.
[assembly: InternalsVisibleTo("TruthWeaver.Testing.Tests")]
