using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic.Models.Messages;

namespace DotnetClaude.Core.Tools;

/// <summary>Demo tool: evaluates a basic arithmetic expression. Useful for showing multi-step tool calls.</summary>
public sealed partial class CalculatorTool : IDemoTool
{
    public string Name => "calculate";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Evaluate a basic arithmetic expression (+, -, *, /, parentheses, decimals). " +
                      "Call this instead of doing arithmetic yourself when the user asks for a calculation.",
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["expression"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "An arithmetic expression, e.g. '(12.5 + 7) * 3'.",
                }),
            },
            Required = ["expression"],
        },
    };

    [GeneratedRegex(@"^[0-9+\-*/().\s]+$")]
    private static partial Regex AllowedExpression();

    public Task<string> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken cancellationToken)
    {
        if (!input.TryGetValue("expression", out var exprElement) || exprElement.ValueKind != JsonValueKind.String)
        {
            return Task.FromResult("Error: missing 'expression' argument.");
        }

        var expression = exprElement.GetString() ?? "";

        if (string.IsNullOrWhiteSpace(expression) || !AllowedExpression().IsMatch(expression))
        {
            return Task.FromResult("Error: expression may only contain numbers, spaces, and + - * / ( ).");
        }

        try
        {
            using var table = new DataTable();
            var result = table.Compute(expression, filter: null);

            if (result is double d && (double.IsInfinity(d) || double.IsNaN(d)))
            {
                return Task.FromResult("Error: division by zero.");
            }

            return Task.FromResult(Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture) ?? "Error: could not evaluate expression.");
        }
        catch (Exception ex) when (ex is EvaluateException or SyntaxErrorException or DivideByZeroException)
        {
            return Task.FromResult($"Error: {ex.Message}");
        }
    }
}
