using DotnetClaude.Core.Data;

namespace DotnetClaude.Core.Services;

/// <summary>
/// One step in an agentic turn, emitted live so the UI can render the model's
/// progress (thinking, calling a tool, getting a result, final answer) as it happens.
/// </summary>
public abstract record AgentStep
{
    public sealed record ApiCallStarted(int Iteration) : AgentStep;

    public sealed record ToolCallStarted(string ToolName, string InputJson) : AgentStep;

    public sealed record ToolCallCompleted(string ToolName, string OutputText, long LatencyMs, bool IsError) : AgentStep;

    public sealed record AssistantText(string Text) : AgentStep;

    public sealed record Completed(QueryMetric Metric) : AgentStep;

    public sealed record Failed(string Message) : AgentStep;
}
