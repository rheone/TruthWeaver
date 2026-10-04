namespace TruthWeaver.Tests.ReferenceDocs;

using System.Globalization;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;

/// <summary>
/// Evaluates a canonical form: a function-call expression over the inventory's oracle operations, for example
/// <c>OR(NOT(a), b)</c> or <c>ATLEAST(k + 1, ...)</c>. Identifiers are operand variables, the operation's own parameter
/// names, or the literals <c>True</c>, <c>False</c> and <c>Unknown</c>; <c>...</c> splices every operand; integers support
/// <c>+</c> and <c>-</c>. Parameters precede operands in a call, as in the DSL.
/// </summary>
internal sealed partial class K3Expression
{
    private readonly List<string> tokens;
    private readonly Dictionary<string, Value> environment;
    private readonly IReadOnlyList<Value> operands;
    private int position;

    private K3Expression(string text, Dictionary<string, Value> environment, IReadOnlyList<Value> operands)
    {
        this.tokens = [.. TokenRegex().Matches(text).Select(match => match.Value)];
        this.environment = environment;
        this.operands = operands;
    }

    private string? Peek => this.position < this.tokens.Count ? this.tokens[this.position] : null;

    /// <summary>Evaluates <paramref name="text"/> to a truth value.</summary>
    /// <param name="text">The expression.</param>
    /// <param name="environment">Variable and parameter bindings.</param>
    /// <param name="operands">The values <c>...</c> splices.</param>
    /// <returns>The oracle's result.</returns>
    /// <exception cref="FormatException">The expression is malformed, uses an unknown name, or is not a truth value.</exception>
    internal static TruthValue Evaluate(string text, Dictionary<string, Value> environment, IReadOnlyList<Value> operands)
    {
        K3Expression expression = new(text, environment, operands);
        Value result = expression.ParseSum();
        if (expression.position != expression.tokens.Count)
        {
            throw new FormatException($"unexpected '{expression.tokens[expression.position]}'");
        }

        return result.Truth ?? throw new FormatException("the form evaluates to an integer, not a truth value");
    }

    [GeneratedRegex(@"\.\.\.|[A-Za-z_][A-Za-z0-9_]*|\d+|[(),+\-]")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();

    private Value ParseSum()
    {
        Value left = this.ParseTerm();
        while (this.Peek is "+" or "-")
        {
            bool add = this.tokens[this.position++] == "+";
            Value right = this.ParseTerm();
            int l = left.Integer ?? throw new FormatException("'+' and '-' apply to integers");
            int r = right.Integer ?? throw new FormatException("'+' and '-' apply to integers");
            left = new Value(add ? l + r : l - r, null);
        }

        return left;
    }

    private Value ParseTerm()
    {
        string token = this.Peek ?? throw new FormatException("the form ends unexpectedly");
        this.position++;
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int integer))
        {
            return new Value(integer, null);
        }

        if (!IdentifierRegex().IsMatch(token))
        {
            throw new FormatException($"unexpected '{token}'");
        }

        if (this.Peek == "(")
        {
            this.position++;
            return this.Call(token);
        }

        if (this.environment.TryGetValue(token, out Value bound))
        {
            return bound;
        }

        // Single letters stay free for variable names; only the full words are literals.
        if (token.Length > 1 && K3Operation.TryParseTruth(token, out TruthValue literal))
        {
            return new Value(null, literal);
        }

        throw new FormatException($"unknown name '{token}'");
    }

    private Value Call(string name)
    {
        if (!K3Operation.Inventory.TryGetValue(name.ToUpperInvariant(), out K3Operation? operation))
        {
            throw new FormatException($"unknown operation '{name}'");
        }

        List<Value> arguments = [];
        while (this.Peek != ")")
        {
            if (this.Peek == "...")
            {
                this.position++;
                arguments.AddRange(this.operands);
            }
            else
            {
                arguments.Add(this.ParseSum());
            }

            if (this.Peek == ",")
            {
                this.position++;
            }
            else if (this.Peek != ")")
            {
                throw new FormatException($"expected ',' or ')' in the call to {name}");
            }
        }

        this.position++;
        int parameterCount = operation.Parameters.Length;
        if (arguments.Count < parameterCount)
        {
            throw new FormatException($"{name} takes {parameterCount} parameter(s) before its operands");
        }

        string[] parameters = [.. arguments.Take(parameterCount).Select(value => value.ToParameter())];
        TruthValue[] truths =
        [
            .. arguments
                .Skip(parameterCount)
                .Select(value => value.Truth ?? throw new FormatException($"{name} operands must be truth values")),
        ];
        if (!operation.AcceptsOperandCount(truths.Length))
        {
            throw new FormatException($"{name} does not accept {truths.Length} operand(s)");
        }

        string result = operation.Evaluate(parameters, truths);
        if (!Enum.TryParse(result, out TruthValue truth))
        {
            throw new FormatException($"{name} does not produce a truth value");
        }

        return new Value(null, truth);
    }

    /// <summary>An integer or a truth value.</summary>
    /// <param name="Integer">The integer, when the value is one.</param>
    /// <param name="Truth">The truth value, when the value is one.</param>
    internal readonly record struct Value(int? Integer, TruthValue? Truth)
    {
        /// <summary>Parses a parameter assignment: an integer, or a truth value name.</summary>
        /// <param name="text">The parameter text.</param>
        /// <returns>The parsed value.</returns>
        internal static Value Parse(string text)
        {
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int integer))
            {
                return new Value(integer, null);
            }

            if (K3Operation.TryParseTruth(text, out TruthValue truth))
            {
                return new Value(null, truth);
            }

            throw new FormatException($"'{text}' is neither an integer nor a truth value");
        }

        /// <summary>The value as a parameter string for <see cref="K3Operation.Evaluate"/>.</summary>
        /// <returns>The integer's digits or the truth value's name.</returns>
        internal string ToParameter()
        {
            return this.Integer?.ToString(CultureInfo.InvariantCulture) ?? this.Truth!.Value.ToString();
        }
    }
}
