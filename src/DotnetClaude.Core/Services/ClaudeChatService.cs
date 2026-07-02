using System.Diagnostics;
using Anthropic;
using Anthropic.Models.Messages;
using DotnetClaude.Core.Data;
using DotnetClaude.Core.Models;
using DotnetClaude.Core.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetClaude.Core.Services;

public sealed class ClaudeChatService(
    AnthropicClient client,
    IDbContextFactory<AppDbContext> dbFactory,
    ToolRegistry toolRegistry,
    IOptions<ClaudeOptions> options,
    ILogger<ClaudeChatService> logger) : IClaudeChatService
{
    private readonly ClaudeOptions _options = options.Value;

    public async IAsyncEnumerable<AgentStep> SendMessageAsync(
        int conversationId,
        string userMessage,
        string modelWireId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelInfo = ModelCatalog.GetByWireId(modelWireId);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var history = await db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        db.Messages.Add(new ChatMessageEntity
        {
            ConversationId = conversationId,
            Role = ChatRole.User,
            Content = userMessage,
        });
        await db.SaveChangesAsync(cancellationToken);

        var messages = history
            .Select(m => new MessageParam
            {
                Role = m.Role == ChatRole.User ? Role.User : Role.Assistant,
                Content = m.Content,
            })
            .ToList();
        messages.Add(new MessageParam { Role = Role.User, Content = userMessage });

        List<ToolUnion>? tools = _options.ToolsEnabled && toolRegistry.All.Count > 0
            ? toolRegistry.All.Select(t => (ToolUnion)t.Definition).ToList()
            : null;

        var metric = new QueryMetric
        {
            ConversationId = conversationId,
            ModelWireId = modelInfo.WireId,
            ModelDisplayName = modelInfo.DisplayName,
            ContextWindowTokens = modelInfo.ContextWindowTokens,
        };

        var assistantTextParts = new List<string>();
        string lastStopReason = "";

        AgentStep? failure = null;

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            yield return new AgentStep.ApiCallStarted(iteration);

            var parameters = new MessageCreateParams
            {
                Model = modelInfo.ApiModel,
                MaxTokens = _options.MaxOutputTokens,
                System = _options.SystemPrompt,
                Messages = messages,
                Tools = tools,
            };

            Message response;
            var callStopwatch = Stopwatch.StartNew();
            try
            {
                response = await client.Messages.Create(parameters, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Claude API call failed for conversation {ConversationId}", conversationId);
                failure = new AgentStep.Failed($"Claude API call failed: {ex.Message}");
                break;
            }
            callStopwatch.Stop();

            metric.ApiCallCount++;
            metric.TotalLatencyMs += callStopwatch.ElapsedMilliseconds;
            metric.InputTokens += (int)response.Usage.InputTokens;
            metric.OutputTokens += (int)response.Usage.OutputTokens;
            metric.CacheCreationInputTokens += (int)(response.Usage.CacheCreationInputTokens ?? 0);
            metric.CacheReadInputTokens += (int)(response.Usage.CacheReadInputTokens ?? 0);
            lastStopReason = (string)response.StopReason!;

            var assistantContent = new List<ContentBlockParam>();
            var toolResults = new List<ContentBlockParam>();

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    assistantContent.Add(new TextBlockParam { Text = text.Text });
                    assistantTextParts.Add(text.Text);
                    yield return new AgentStep.AssistantText(text.Text);
                }
                else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    assistantContent.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input,
                    });

                    var inputJson = System.Text.Json.JsonSerializer.Serialize(toolUse.Input);
                    yield return new AgentStep.ToolCallStarted(toolUse.Name, inputJson);

                    var toolStopwatch = Stopwatch.StartNew();
                    string output;
                    bool isError;
                    if (toolRegistry.TryGet(toolUse.Name, out var tool))
                    {
                        try
                        {
                            output = await tool.ExecuteAsync(toolUse.Input, cancellationToken);
                            isError = false;
                        }
                        catch (Exception ex)
                        {
                            output = $"Tool error: {ex.Message}";
                            isError = true;
                        }
                    }
                    else
                    {
                        output = $"Unknown tool '{toolUse.Name}'.";
                        isError = true;
                    }
                    toolStopwatch.Stop();

                    metric.ToolCallCount++;
                    metric.ToolInvocations.Add(new ToolInvocation
                    {
                        ToolName = toolUse.Name,
                        InputJson = inputJson,
                        OutputText = output,
                        LatencyMs = toolStopwatch.ElapsedMilliseconds,
                        IsError = isError,
                    });

                    yield return new AgentStep.ToolCallCompleted(toolUse.Name, output, toolStopwatch.ElapsedMilliseconds, isError);

                    toolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = toolUse.ID,
                        Content = output,
                        IsError = isError,
                    });
                }
            }

            if (response.StopReason != "tool_use" || toolResults.Count == 0)
            {
                break;
            }

            messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });
            messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
        }

        if (failure is not null)
        {
            yield return failure;
            yield break;
        }

        metric.StopReason = lastStopReason;
        metric.EstimatedCostUsd = CostCalculator.EstimateCostUsd(
            modelInfo,
            metric.InputTokens,
            metric.OutputTokens,
            metric.CacheCreationInputTokens,
            metric.CacheReadInputTokens);

        var assistantText = string.Join("\n\n", assistantTextParts).Trim();

        var assistantMessage = new ChatMessageEntity
        {
            ConversationId = conversationId,
            Role = ChatRole.Assistant,
            Content = string.IsNullOrEmpty(assistantText) ? "(no text response)" : assistantText,
            ModelWireId = modelInfo.WireId,
        };
        db.Messages.Add(assistantMessage);
        metric.AssistantMessage = assistantMessage;
        db.QueryMetrics.Add(metric);
        await db.SaveChangesAsync(cancellationToken);

        yield return new AgentStep.Completed(metric);
    }
}
