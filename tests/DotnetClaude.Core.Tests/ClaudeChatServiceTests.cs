using Anthropic;
using DotnetClaude.Core.Data;
using DotnetClaude.Core.Services;
using DotnetClaude.Core.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetClaude.Core.Tests;

/// <summary>
/// Exercises the real request/response wiring against the live Claude API using a
/// deliberately invalid key, so the test suite doesn't depend on a real credential.
/// This still validates request construction, error handling, and DB persistence.
/// </summary>
public class ClaudeChatServiceTests : IAsyncLifetime
{
    private IDbContextFactory<AppDbContext> _dbFactory = null!;
    private ServiceProvider _provider = null!;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        _provider = services.BuildServiceProvider();
        _dbFactory = _provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }

    private ClaudeChatService CreateService(int maxToolIterations = 2) => new(
        client: new AnthropicClient { ApiKey = "sk-ant-test-invalid-key-00000000000000000000000" },
        dbFactory: _dbFactory,
        toolRegistry: new ToolRegistry([new GetCurrentTimeTool(), new CalculatorTool()]),
        options: Options.Create(new ClaudeOptions { MaxToolIterations = maxToolIterations, MaxOutputTokens = 256 }),
        logger: NullLogger<ClaudeChatService>.Instance);

    private async Task<Data.Conversation> CreateConversationAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conversation = new Data.Conversation { Title = "Test conversation" };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        return conversation;
    }

    [Fact]
    public async Task SendMessageAsync_InvalidApiKey_YieldsFailedStep()
    {
        var conversation = await CreateConversationAsync();
        var service = CreateService();

        var steps = new List<AgentStep>();
        await foreach (var step in service.SendMessageAsync(conversation.Id, "Hello, Claude", "claude-haiku-4-5"))
        {
            steps.Add(step);
        }

        Assert.Contains(steps, s => s is AgentStep.ApiCallStarted);
        Assert.Contains(steps, s => s is AgentStep.Failed);
        Assert.DoesNotContain(steps, s => s is AgentStep.Completed);
    }

    [Fact]
    public async Task SendMessageAsync_PersistsUserMessageEvenWhenApiCallFails()
    {
        var conversation = await CreateConversationAsync();
        var service = CreateService();

        await foreach (var _ in service.SendMessageAsync(conversation.Id, "What time is it?", "claude-haiku-4-5"))
        {
            // drain
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var messages = await db.Messages.Where(m => m.ConversationId == conversation.Id).ToListAsync();

        var userMessage = Assert.Single(messages);
        Assert.Equal(ChatRole.User, userMessage.Role);
        Assert.Equal("What time is it?", userMessage.Content);
    }

    [Fact]
    public async Task SendMessageAsync_UnknownConversationId_StillCallsApiAndFails()
    {
        var service = CreateService();

        var steps = new List<AgentStep>();
        await foreach (var step in service.SendMessageAsync(conversationId: 999_999, "Hi", "claude-opus-4-8"))
        {
            steps.Add(step);
        }

        Assert.Contains(steps, s => s is AgentStep.Failed);
    }
}
