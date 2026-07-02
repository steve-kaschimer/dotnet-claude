using DotnetClaude.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetClaude.Core.Services;

public sealed class ConversationService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<Conversation> CreateConversationAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var conversation = new Conversation { Title = string.IsNullOrWhiteSpace(title) ? "New conversation" : title };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<List<Conversation>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Conversations
            .OrderByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatMessageEntity>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task RenameConversationAsync(int conversationId, string title, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var conversation = await db.Conversations.FindAsync([conversationId], cancellationToken);
        if (conversation is null)
        {
            return;
        }
        conversation.Title = title;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteConversationAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var conversation = await db.Conversations.FindAsync([conversationId], cancellationToken);
        if (conversation is null)
        {
            return;
        }
        db.Conversations.Remove(conversation);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>All query metrics across every conversation, newest first, for the performance dashboard.</summary>
    public async Task<List<QueryMetric>> GetAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.QueryMetrics
            .Include(m => m.ToolInvocations)
            .Include(m => m.Conversation)
            .OrderByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<QueryMetric>> GetMetricsForConversationAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.QueryMetrics
            .Include(m => m.ToolInvocations)
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }
}
