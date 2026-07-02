using System.Text.Json;
using DotnetClaude.Core.Tools;
using Xunit;

namespace DotnetClaude.Core.Tests;

public class GetCurrentTimeToolTests
{
    private readonly GetCurrentTimeTool _tool = new();

    [Fact]
    public async Task ExecuteAsync_NoTimezone_ReturnsUtcTime()
    {
        var result = await _tool.ExecuteAsync(new Dictionary<string, JsonElement>(), CancellationToken.None);
        Assert.Contains("UTC", result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidTimezone_ConvertsCorrectly()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["timezone"] = JsonSerializer.SerializeToElement("America/New_York"),
        };

        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        Assert.Contains("America/New_York", result);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTimezone_ReturnsFriendlyError()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["timezone"] = JsonSerializer.SerializeToElement("Not/A_Zone"),
        };

        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        Assert.Contains("Unknown time zone", result);
    }

    [Fact]
    public void Definition_HasExpectedName()
    {
        Assert.Equal("get_current_time", _tool.Definition.Name);
        Assert.Equal("get_current_time", _tool.Name);
    }
}

public class CalculatorToolTests
{
    private readonly CalculatorTool _tool = new();

    [Theory]
    [InlineData("2 + 2", "4")]
    [InlineData("(12.5 + 7) * 3", "58.5")]
    [InlineData("10 / 4", "2.5")]
    public async Task ExecuteAsync_ValidExpression_ReturnsResult(string expression, string expected)
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["expression"] = JsonSerializer.SerializeToElement(expression),
        };

        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExecuteAsync_MissingExpression_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(new Dictionary<string, JsonElement>(), CancellationToken.None);
        Assert.StartsWith("Error", result);
    }

    [Theory]
    [InlineData("2 + 2; DROP TABLE users")]
    [InlineData("System.IO.File.Delete(\"x\")")]
    [InlineData("2 + abc")]
    public async Task ExecuteAsync_DisallowedCharacters_ReturnsError(string expression)
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["expression"] = JsonSerializer.SerializeToElement(expression),
        };

        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        Assert.StartsWith("Error", result);
    }

    [Fact]
    public async Task ExecuteAsync_DivideByZero_ReturnsError()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["expression"] = JsonSerializer.SerializeToElement("1 / 0"),
        };

        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        Assert.StartsWith("Error", result);
    }
}

public class ToolRegistryTests
{
    [Fact]
    public void TryGet_KnownTool_ReturnsTrue()
    {
        var registry = new ToolRegistry([new GetCurrentTimeTool(), new CalculatorTool()]);

        Assert.True(registry.TryGet("calculate", out var tool));
        Assert.IsType<CalculatorTool>(tool);
        Assert.Equal(2, registry.All.Count);
    }

    [Fact]
    public void TryGet_UnknownTool_ReturnsFalse()
    {
        var registry = new ToolRegistry([new GetCurrentTimeTool()]);
        Assert.False(registry.TryGet("does_not_exist", out _));
    }
}
