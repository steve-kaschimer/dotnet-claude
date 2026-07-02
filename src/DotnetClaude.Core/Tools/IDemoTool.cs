using System.Text.Json;
using Anthropic.Models.Messages;

namespace DotnetClaude.Core.Tools;

/// <summary>
/// A tool ("skill") the assistant can invoke during an agentic loop. Implementations
/// are executed locally by <see cref="Services.ClaudeChatService"/> in response to a
/// <c>tool_use</c> block, so the API only ever needs the schema in <see cref="Definition"/>.
/// </summary>
public interface IDemoTool
{
    string Name { get; }

    Tool Definition { get; }

    /// <summary>Runs the tool against the model-supplied input and returns the text to send back as the tool_result.</summary>
    Task<string> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken cancellationToken);
}
