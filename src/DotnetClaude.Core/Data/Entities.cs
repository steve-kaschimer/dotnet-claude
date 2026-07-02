namespace DotnetClaude.Core.Data;

public sealed class Conversation
{
    public int Id { get; set; }
    public string Title { get; set; } = "New conversation";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ChatMessageEntity> Messages { get; set; } = [];
    public List<QueryMetric> Metrics { get; set; } = [];
}

public enum ChatRole
{
    User,
    Assistant,
}

public sealed class ChatMessageEntity
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    public string? ModelWireId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Performance data captured for a single request/response round trip to the
/// Claude API, including every tool-use iteration inside an agentic loop.
/// </summary>
public sealed class QueryMetric
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    /// <summary>The assistant message this query produced.</summary>
    public int? AssistantMessageId { get; set; }
    public ChatMessageEntity? AssistantMessage { get; set; }

    public string ModelWireId { get; set; } = "";
    public string ModelDisplayName { get; set; } = "";

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheCreationInputTokens { get; set; }
    public int CacheReadInputTokens { get; set; }
    public int ContextWindowTokens { get; set; }

    public long TotalLatencyMs { get; set; }
    public int ApiCallCount { get; set; }
    public int ToolCallCount { get; set; }

    public string StopReason { get; set; } = "";
    public decimal EstimatedCostUsd { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ToolInvocation> ToolInvocations { get; set; } = [];

    public int TotalTokens => InputTokens + OutputTokens + CacheCreationInputTokens + CacheReadInputTokens;

    public double ContextUtilizationPercent =>
        ContextWindowTokens == 0 ? 0 : Math.Round(100.0 * TotalTokens / ContextWindowTokens, 2);
}

/// <summary>One tool ("skill") call made by the model during an agentic loop.</summary>
public sealed class ToolInvocation
{
    public int Id { get; set; }
    public int QueryMetricId { get; set; }
    public QueryMetric? QueryMetric { get; set; }

    public string ToolName { get; set; } = "";
    public string InputJson { get; set; } = "";
    public string OutputText { get; set; } = "";
    public long LatencyMs { get; set; }
    public bool IsError { get; set; }
}
