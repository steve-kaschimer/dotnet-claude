namespace DotnetClaude.Core.Services;

/// <summary>Configuration for how the chat service talks to the Claude API.</summary>
public sealed class ClaudeOptions
{
    public const string SectionName = "Anthropic";

    /// <summary>API key. If unset, falls back to the ANTHROPIC_API_KEY environment variable.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Max output tokens per API call.</summary>
    public int MaxOutputTokens { get; set; } = 4096;

    /// <summary>Upper bound on tool-use round trips within a single turn, to prevent runaway loops.</summary>
    public int MaxToolIterations { get; set; } = 6;

    /// <summary>Whether the demo tools (get_current_time, calculate) are offered to the model.</summary>
    public bool ToolsEnabled { get; set; } = true;

    public string SystemPrompt { get; set; } =
        "You are a helpful assistant embedded in a .NET demo application that showcases " +
        "the Claude API: model switching, token usage, latency, and tool ('skill') calls. " +
        "Use the available tools when they would give a more accurate answer than reasoning " +
        "alone. Keep responses concise.";
}
