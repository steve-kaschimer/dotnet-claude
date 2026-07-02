namespace DotnetClaude.Core.Services;

public interface IClaudeChatService
{
    /// <summary>
    /// Sends a user message to Claude, driving a bounded agentic tool-use loop, and
    /// streams back each step (tool calls, tool results, assistant text, final metrics)
    /// as it happens. Persists the user/assistant messages and the query performance
    /// metrics to the database.
    /// </summary>
    IAsyncEnumerable<AgentStep> SendMessageAsync(
        int conversationId,
        string userMessage,
        string modelWireId,
        CancellationToken cancellationToken = default);
}
